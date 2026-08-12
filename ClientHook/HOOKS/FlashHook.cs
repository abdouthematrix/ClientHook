using EasyHook;
using System;
using System.Runtime.InteropServices;

namespace ClientHook
{
    /// <summary>
    /// Hook for LoadLibraryExW. Redirects Flash.ocx loads to a local copy.
    /// </summary>
    public sealed class FlashHook
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
        private delegate IntPtr LoadLibraryExWDelegate(string lpLibFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);

        private static LocalHook _hook;

        public static bool Install()
        {
            try
            {
                _hook = LocalHook.Create(
                    LocalHook.GetProcAddress("kernelbase.dll", "LoadLibraryExW"),
                    new LoadLibraryExWDelegate(Detour),
                    null);

                _hook.ThreadACL.SetExclusiveACL(new int[0]); // hook all threads
                Log.Info("  [OK] FlashHook installed");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"  [FAIL] FlashHook: {ex.Message}");
                return false;
            }
        }

        public static void UnInstall()
        {
            _hook?.Dispose();
        }

        private static IntPtr Detour(string lpLibFileName, IntPtr hFile, uint dwFlags)
        {
            if (lpLibFileName != null &&
                lpLibFileName.IndexOf("Flash.ocx", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Log.Info($"[FlashHook] Redirecting load of \"{lpLibFileName}\" to local ./Flash.ocx");
                lpLibFileName = "./Flash.ocx";
            }

            return LoadLibraryExW(lpLibFileName, hFile, dwFlags);
        }
    }
}
