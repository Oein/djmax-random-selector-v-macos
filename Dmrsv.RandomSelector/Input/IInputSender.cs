namespace Dmrsv.RandomSelector.Input
{
    /// <summary>
    /// Abstraction over the OS-specific mechanism for synthesizing keyboard input.
    /// The Windows implementation uses Win32 <c>SendInput</c> with DirectInput scan
    /// codes; the macOS implementation uses Quartz <c>CGEvent</c> with virtual key
    /// codes (so it can drive DJMAX RESPECT V running under CrossOver / Whisky).
    /// </summary>
    public interface IInputSender
    {
        void KeyDown(Key key);
        void KeyUp(Key key);
    }
}
