using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Dmrsv.RandomSelector.Input
{
    /// <summary>
    /// Sends keyboard input on Windows via the Win32 <c>SendInput</c> API using
    /// DirectInput scan codes (KEYEVENTF_SCANCODE), which is what DJMAX RESPECT V
    /// reads. This is the original behaviour extracted from the old Locator.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class WindowsInputSender : IInputSender
    {
        private readonly Dictionary<Key, ushort> _scanCodes;

        private static readonly HashSet<Key> ArrowKeys = new()
        {
            Key.Left, Key.Up, Key.Right, Key.Down,
        };

        public WindowsInputSender()
        {
            _scanCodes = new Dictionary<Key, ushort>
            {
                [Key.D1] = 0x02, [Key.D2] = 0x03, [Key.D3] = 0x04, [Key.D4] = 0x05, [Key.D5] = 0x06,
                [Key.D6] = 0x07, [Key.D7] = 0x08, [Key.D8] = 0x09, [Key.D9] = 0x0A, [Key.D0] = 0x0B,
                [Key.Q] = 0x10, [Key.W] = 0x11, [Key.E] = 0x12, [Key.R] = 0x13, [Key.T] = 0x14,
                [Key.Y] = 0x15, [Key.U] = 0x16, [Key.I] = 0x17, [Key.O] = 0x18, [Key.P] = 0x19,
                [Key.A] = 0x1E, [Key.S] = 0x1F, [Key.D] = 0x20, [Key.F] = 0x21, [Key.G] = 0x22,
                [Key.H] = 0x23, [Key.J] = 0x24, [Key.K] = 0x25, [Key.L] = 0x26, [Key.Semicolon] = 0x27,
                [Key.Z] = 0x2C, [Key.X] = 0x2D, [Key.C] = 0x2E, [Key.V] = 0x2F, [Key.B] = 0x30,
                [Key.N] = 0x31, [Key.M] = 0x32,
                [Key.F5] = 0x3F,
                [Key.ShiftLeft] = 0x2A, [Key.ShiftRight] = 0x36,
                [Key.ControlLeft] = 0x1D,
                [Key.PageUp] = 0xC9 + 1024, [Key.PageDown] = 0xD1 + 1024,
                [Key.Numpad4] = 0x4B, [Key.Numpad5] = 0x4C, [Key.Numpad6] = 0x4D, [Key.Numpad8] = 0x48,
                // Arrow keys resolve to scan codes at runtime via MapVirtualKey.
                [Key.Left] = (ushort)MapVirtualKey(0x25, 0),
                [Key.Up] = (ushort)MapVirtualKey(0x26, 0),
                [Key.Right] = (ushort)MapVirtualKey(0x27, 0),
                [Key.Down] = (ushort)MapVirtualKey(0x28, 0),
            };
        }

        public void KeyDown(Key key) => SendKeyDown(_scanCodes[key], ArrowKeys.Contains(key));

        public void KeyUp(Key key) => SendKeyUp(_scanCodes[key], ArrowKeys.Contains(key));

        private bool SendKeyDown(ushort scanCode, bool isArrowKey)
        {
            // Original code from https://github.com/learncodebygaming/pydirectinput
            // Copyright(c) 2020 Ben Johnson
            uint insertedEvents = 0;
            uint expectedEvents = 1;
            uint Flags = 0x0008;    // KEYEVENTF_SCANCODE
            INPUT input = new INPUT { Type = 1 };
            input.Data.Keyboard = new KEYBDINPUT
            {
                Vk = 0,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            if (isArrowKey)
            {
                Flags |= 0x0001;    // KEYEVENTF_EXTENDEDKEY
                if (GetKeyState(0x90) != 0)
                {
                    INPUT input2 = new INPUT { Type = 1 };
                    input2.Data.Keyboard = new KEYBDINPUT
                    {
                        Vk = 0,
                        Scan = 0xE0,
                        Flags = 0x0008,
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    };
                    INPUT[] inputs2 = new INPUT[] { input2 };
                    expectedEvents = 2;
                    insertedEvents += SendInput(1, inputs2, Marshal.SizeOf(typeof(INPUT)));
                }
            }
            input.Data.Keyboard.Scan = scanCode;
            input.Data.Keyboard.Flags = Flags;

            INPUT[] inputs = new INPUT[] { input };
            insertedEvents += SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            return insertedEvents == expectedEvents;
        }

        private bool SendKeyUp(ushort scanCode, bool isArrowKey)
        {
            // Original code from https://github.com/learncodebygaming/pydirectinput
            // Copyright(c) 2020 Ben Johnson
            uint insertedEvents = 0;
            uint expectedEvents = 1;
            uint Flags = 0x0008 | 0x0002;       // KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP
            INPUT input = new INPUT { Type = 1 };
            input.Data.Keyboard = new KEYBDINPUT
            {
                Vk = 0,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            if (isArrowKey)
            {
                Flags |= 0x0001;    // KEYEVENTF_EXTENDEDKEY
            }

            input.Data.Keyboard.Scan = scanCode;
            input.Data.Keyboard.Flags = Flags;

            INPUT[] inputs = new INPUT[] { input };
            insertedEvents += SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

            if (isArrowKey && GetKeyState(0x90) != 0)
            {
                INPUT input2 = new INPUT { Type = 1 };
                input2.Data.Keyboard = new KEYBDINPUT
                {
                    Vk = 0,
                    Scan = 0xE0,
                    Flags = 0x0008 | 0x0002,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                };
                INPUT[] inputs2 = new INPUT[] { input2 };
                expectedEvents = 2;
                insertedEvents += SendInput(1, inputs2, Marshal.SizeOf(typeof(INPUT)));
            }
            return insertedEvents == expectedEvents;
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(int wCode, int wMapType);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }
    }
}
