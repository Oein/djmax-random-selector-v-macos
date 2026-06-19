namespace Dmrsv.RandomSelector.Input
{
    /// <summary>
    /// Platform-neutral identifiers for the keys the <see cref="Locator"/> needs to
    /// send to DJMAX RESPECT V. Each <see cref="IInputSender"/> implementation maps
    /// these to the appropriate native scan codes / virtual key codes.
    /// </summary>
    public enum Key
    {
        // Number row
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

        // Letters (kept in alphabetical order)
        A, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

        Semicolon,
        F5,

        ShiftLeft, ShiftRight,
        ControlLeft,

        Left, Up, Right, Down,
        PageUp, PageDown,

        // Numeric keypad keys used as the in-game "button mode" shortcut (Ctrl + Numpad)
        Numpad4, Numpad5, Numpad6, Numpad8,
    }
}
