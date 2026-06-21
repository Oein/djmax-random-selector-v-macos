using System;

namespace DjmaxRandomSelectorV.Desktop.Platform
{
    /// <summary>
    /// Registers a single system-wide hotkey. macOS uses Carbon
    /// <c>RegisterEventHotKey</c>; other platforms get a no-op for now.
    /// </summary>
    public interface IGlobalHotkey : IDisposable
    {
        /// <param name="keyCode">Platform virtual key code (macOS kVK_*; F7 = 0x62).</param>
        /// <param name="onPressed">Invoked on the main thread when the hotkey fires.</param>
        bool Register(uint keyCode, Action onPressed);
    }

    public static class GlobalHotkey
    {
        public static IGlobalHotkey Create()
        {
            if (OperatingSystem.IsMacOS())
            {
                return new MacGlobalHotkey();
            }
            return new NullGlobalHotkey();
        }
    }

    internal sealed class NullGlobalHotkey : IGlobalHotkey
    {
        public bool Register(uint keyCode, Action onPressed) => false;

        public void Dispose() { }
    }
}
