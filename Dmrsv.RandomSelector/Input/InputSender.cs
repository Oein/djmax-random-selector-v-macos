using System;

namespace Dmrsv.RandomSelector.Input
{
    /// <summary>
    /// Factory that selects the appropriate <see cref="IInputSender"/> for the
    /// current operating system.
    /// </summary>
    public static class InputSender
    {
        public static IInputSender Create()
        {
            if (OperatingSystem.IsWindows())
            {
                return new WindowsInputSender();
            }
            if (OperatingSystem.IsMacOS())
            {
                return new MacInputSender();
            }
            return new NullInputSender();
        }
    }
}
