namespace Dmrsv.RandomSelector.Input
{
    /// <summary>
    /// No-op input sender used on platforms without a native backend (e.g. Linux,
    /// or CI). The selection logic still runs; no keystrokes are synthesized.
    /// </summary>
    internal sealed class NullInputSender : IInputSender
    {
        public void KeyDown(Key key) { }

        public void KeyUp(Key key) { }
    }
}
