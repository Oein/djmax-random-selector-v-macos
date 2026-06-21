using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DjmaxRandomSelectorV.Desktop.Platform
{
    /// <summary>
    /// System-wide hotkey via the Carbon Event Manager (<c>RegisterEventHotKey</c>).
    /// The hotkey is delivered to the application's event target, which Avalonia's
    /// macOS backend pumps on the main run loop — so <c>onPressed</c> runs on the
    /// UI thread. This is the macOS counterpart to the Windows app's
    /// <c>RegisterHotKey</c> + HWND hook.
    /// </summary>
    [SupportedOSPlatform("macos")]
    internal sealed class MacGlobalHotkey : IGlobalHotkey
    {
        private const string Carbon = "/System/Library/Frameworks/Carbon.framework/Carbon";

        // FourCharCodes / constants from Carbon (CarbonEvents.h).
        private const uint kEventClassKeyboard = 0x6B657962; // 'keyb'
        private const uint kEventHotKeyPressed = 6;
        private const uint HotKeySignature = 0x646D7273;     // 'dmrs'

        private delegate int EventHandlerProc(IntPtr callRef, IntPtr theEvent, IntPtr userData);

        // Kept in a field so the GC doesn't collect the delegate behind the native call.
        private EventHandlerProc? _handler;
        private Action? _onPressed;
        private IntPtr _hotKeyRef;
        private IntPtr _handlerRef;

        public bool Register(uint keyCode, Action onPressed)
        {
            _onPressed = onPressed;
            _handler = OnHotKey;

            IntPtr target = GetApplicationEventTarget();

            var eventType = new EventTypeSpec
            {
                EventClass = kEventClassKeyboard,
                EventKind = kEventHotKeyPressed,
            };

            int status = InstallEventHandler(target, _handler, 1, new[] { eventType }, IntPtr.Zero, out _handlerRef);
            if (status != 0)
            {
                return false;
            }

            var hotKeyId = new EventHotKeyID { Signature = HotKeySignature, Id = 1 };
            status = RegisterEventHotKey(keyCode, 0, hotKeyId, target, 0, out _hotKeyRef);
            return status == 0;
        }

        private int OnHotKey(IntPtr callRef, IntPtr theEvent, IntPtr userData)
        {
            _onPressed?.Invoke();
            return 0; // noErr
        }

        public void Dispose()
        {
            if (_hotKeyRef != IntPtr.Zero)
            {
                UnregisterEventHotKey(_hotKeyRef);
                _hotKeyRef = IntPtr.Zero;
            }
            if (_handlerRef != IntPtr.Zero)
            {
                RemoveEventHandler(_handlerRef);
                _handlerRef = IntPtr.Zero;
            }
            _handler = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventTypeSpec
        {
            public uint EventClass;
            public uint EventKind;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EventHotKeyID
        {
            public uint Signature;
            public uint Id;
        }

        [DllImport(Carbon)]
        private static extern IntPtr GetApplicationEventTarget();

        [DllImport(Carbon)]
        private static extern int InstallEventHandler(IntPtr target, EventHandlerProc handler,
            uint numTypes, EventTypeSpec[] typeList, IntPtr userData, out IntPtr handlerRef);

        [DllImport(Carbon)]
        private static extern int RemoveEventHandler(IntPtr handlerRef);

        [DllImport(Carbon)]
        private static extern int RegisterEventHotKey(uint hotKeyCode, uint hotKeyModifiers,
            EventHotKeyID hotKeyId, IntPtr target, uint options, out IntPtr hotKeyRef);

        [DllImport(Carbon)]
        private static extern int UnregisterEventHotKey(IntPtr hotKeyRef);
    }
}
