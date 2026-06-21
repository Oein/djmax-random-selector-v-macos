using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DjmaxRandomSelectorV.Desktop.Platform
{
    /// <summary>
    /// macOS Accessibility (AXIsProcessTrusted) helpers. Synthesized keyboard
    /// events (CGEvent) are silently dropped unless the host app is trusted for
    /// Accessibility, so the app must detect this and guide the user.
    /// </summary>
    [SupportedOSPlatform("macos")]
    internal static class MacAccessibility
    {
        private const string ApplicationServices =
            "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
        private const string CoreFoundation =
            "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        private const string LibSystem = "/usr/lib/libSystem.dylib";
        private const int RTLD_NOW = 2;

        /// <summary>True if the app is already trusted for Accessibility.</summary>
        public static bool IsTrusted() => AXIsProcessTrusted();

        /// <summary>
        /// Returns the current trust state and, if not trusted, asks macOS to show
        /// the "allow Accessibility" prompt (equivalent to passing
        /// kAXTrustedCheckOptionPrompt = true).
        /// </summary>
        public static bool PromptIfNeeded()
        {
            // dlopen the framework paths to guarantee the constants are loaded.
            IntPtr appServices = dlopen(ApplicationServices, RTLD_NOW);
            IntPtr coreFoundation = dlopen(CoreFoundation, RTLD_NOW);
            if (appServices != IntPtr.Zero && coreFoundation != IntPtr.Zero)
            {
                IntPtr promptKeySym = dlsym(appServices, "kAXTrustedCheckOptionPrompt");
                IntPtr trueSym = dlsym(coreFoundation, "kCFBooleanTrue");
                if (promptKeySym != IntPtr.Zero && trueSym != IntPtr.Zero)
                {
                    // The symbols are pointers to the CF constants; dereference once.
                    IntPtr key = Marshal.ReadIntPtr(promptKeySym);
                    IntPtr value = Marshal.ReadIntPtr(trueSym);
                    IntPtr options = CFDictionaryCreate(IntPtr.Zero,
                        new[] { key }, new[] { value }, 1, IntPtr.Zero, IntPtr.Zero);
                    try
                    {
                        return AXIsProcessTrustedWithOptions(options);
                    }
                    finally
                    {
                        if (options != IntPtr.Zero)
                        {
                            CFRelease(options);
                        }
                    }
                }
            }
            return AXIsProcessTrusted();
        }

        /// <summary>Opens System Settings at the Accessibility privacy pane.</summary>
        public static void OpenSettings()
        {
            try
            {
                var psi = new ProcessStartInfo("open") { UseShellExecute = false };
                psi.ArgumentList.Add("x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility");
                Process.Start(psi);
            }
            catch
            {
                // Best effort — ignore if the URL scheme can't be opened.
            }
        }

        [DllImport(ApplicationServices)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool AXIsProcessTrusted();

        [DllImport(ApplicationServices)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFDictionaryCreate(IntPtr allocator, IntPtr[] keys, IntPtr[] values,
            long numValues, IntPtr keyCallBacks, IntPtr valueCallBacks);

        [DllImport(CoreFoundation)]
        private static extern void CFRelease(IntPtr cf);

        [DllImport(LibSystem)]
        private static extern IntPtr dlopen(string? path, int mode);

        [DllImport(LibSystem)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);
    }
}
