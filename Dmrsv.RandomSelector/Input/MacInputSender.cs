using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Dmrsv.RandomSelector.Input
{
    /// <summary>
    /// Sends keyboard input on macOS via the Quartz Event Services
    /// (<c>CGEvent</c>) API using ANSI virtual key codes. This allows the selector
    /// to drive DJMAX RESPECT V running through a Windows compatibility layer such
    /// as CrossOver / Whisky / Parallels.
    ///
    /// Requires the host application to be granted Accessibility permission
    /// (System Settings &gt; Privacy &amp; Security &gt; Accessibility); without it the
    /// synthesized events are silently dropped by the OS.
    /// </summary>
    [SupportedOSPlatform("macos")]
    internal sealed class MacInputSender : IInputSender
    {
        private const int kCGEventSourceStateHIDSystemState = 1;
        private const uint kCGHIDEventTap = 0;

        [Flags]
        private enum CGEventFlags : ulong
        {
            None = 0,
            Shift = 0x00020000,
            Control = 0x00040000,
            Option = 0x00080000,
            Command = 0x00100000,
        }

        // kVK_ANSI_* / kVK_* virtual key codes from Carbon HIToolbox (Events.h).
        private static readonly Dictionary<Key, ushort> KeyCodes = new()
        {
            [Key.D0] = 0x1D, [Key.D1] = 0x12, [Key.D2] = 0x13, [Key.D3] = 0x14, [Key.D4] = 0x15,
            [Key.D5] = 0x17, [Key.D6] = 0x16, [Key.D7] = 0x1A, [Key.D8] = 0x1C, [Key.D9] = 0x19,
            [Key.A] = 0x00, [Key.B] = 0x0B, [Key.C] = 0x08, [Key.D] = 0x02, [Key.E] = 0x0E,
            [Key.F] = 0x03, [Key.G] = 0x05, [Key.H] = 0x04, [Key.I] = 0x22, [Key.J] = 0x26,
            [Key.K] = 0x28, [Key.L] = 0x25, [Key.M] = 0x2E, [Key.N] = 0x2D, [Key.O] = 0x1F,
            [Key.P] = 0x23, [Key.Q] = 0x0C, [Key.R] = 0x0F, [Key.S] = 0x01, [Key.T] = 0x11,
            [Key.U] = 0x20, [Key.V] = 0x09, [Key.W] = 0x0D, [Key.X] = 0x07, [Key.Y] = 0x10,
            [Key.Z] = 0x06,
            [Key.Semicolon] = 0x29,
            [Key.F5] = 0x60,
            [Key.ShiftLeft] = 0x38, [Key.ShiftRight] = 0x3C,
            [Key.ControlLeft] = 0x3B,
            [Key.Left] = 0x7B, [Key.Right] = 0x7C, [Key.Down] = 0x7D, [Key.Up] = 0x7E,
            [Key.PageUp] = 0x74, [Key.PageDown] = 0x79,
            [Key.Numpad4] = 0x56, [Key.Numpad5] = 0x57, [Key.Numpad6] = 0x58, [Key.Numpad8] = 0x5B,
        };

        private readonly IntPtr _source;
        private CGEventFlags _activeFlags = CGEventFlags.None;

        public MacInputSender()
        {
            _source = CGEventSourceCreate(kCGEventSourceStateHIDSystemState);
        }

        public void KeyDown(Key key) => Send(key, keyDown: true);

        public void KeyUp(Key key) => Send(key, keyDown: false);

        private void Send(Key key, bool keyDown)
        {
            // Track held modifiers so subsequent key events carry the right flags
            // (e.g. Ctrl + Numpad for the in-game button-mode shortcut).
            CGEventFlags flag = ModifierFlag(key);
            if (flag != CGEventFlags.None)
            {
                if (keyDown)
                {
                    _activeFlags |= flag;
                }
                else
                {
                    _activeFlags &= ~flag;
                }
            }

            if (!KeyCodes.TryGetValue(key, out ushort code))
            {
                return;
            }

            IntPtr handle = CGEventCreateKeyboardEvent(_source, code, keyDown);
            if (handle == IntPtr.Zero)
            {
                return;
            }

            CGEventSetFlags(handle, (ulong)_activeFlags);
            CGEventPost(kCGHIDEventTap, handle);
            CFRelease(handle);
        }

        private static CGEventFlags ModifierFlag(Key key) => key switch
        {
            Key.ShiftLeft or Key.ShiftRight => CGEventFlags.Shift,
            Key.ControlLeft => CGEventFlags.Control,
            _ => CGEventFlags.None,
        };

        private const string ApplicationServices =
            "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
        private const string CoreFoundation =
            "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        [DllImport(ApplicationServices)]
        private static extern IntPtr CGEventSourceCreate(int stateID);

        [DllImport(ApplicationServices)]
        private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey,
            [MarshalAs(UnmanagedType.I1)] bool keyDown);

        [DllImport(ApplicationServices)]
        private static extern void CGEventSetFlags(IntPtr handle, ulong flags);

        [DllImport(ApplicationServices)]
        private static extern void CGEventPost(uint tap, IntPtr handle);

        [DllImport(CoreFoundation)]
        private static extern void CFRelease(IntPtr handle);
    }
}
