using EasyHook;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ClientHook
{
    /// <summary>
    /// Hook for ShellExecuteA. Blocks or redirects websites based on ClientConfig settings.
    /// </summary>
    public sealed class WebsiteHook
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Ansi)]
        private delegate IntPtr ShellExecuteADelegate(
            IntPtr hwnd, string lpOperation, string lpFile,
            string lpParameters, string lpDirectory, int nShowCmd);

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr ShellExecuteA(
            IntPtr hwnd, string lpOperation, string lpFile,
            string lpParameters, string lpDirectory, int nShowCmd);

        // SE_ERR_FNF ("file not found"), returned when a target is blocked.
        private static readonly IntPtr SE_ERR_FNF = new IntPtr(2);

        private static LocalHook _hook;
        private static ClientConfig _cfg;

        // Regex patterns
        private static readonly Regex s_home = new Regex(@"co\..*\.com$");
        private static readonly Regex s_forum = new Regex(@"bbs\.co\..*\.com");
        private static readonly Regex s_credit = new Regex(@"credit\..*\.com\/purchase$");
        private static readonly Regex s_signup = new Regex(@"account\..*\.com\/common\/signup\.aspx");
        private static readonly Regex s_signout = new Regex(@"co\..*\.com\/signout");

        public static bool Install(ClientConfig cfg)
        {
            try
            {
                _cfg = cfg;

                _hook = LocalHook.Create(
                    LocalHook.GetProcAddress("shell32.dll", "ShellExecuteA"),
                    new ShellExecuteADelegate(Detour),
                    null);

                _hook.ThreadACL.SetExclusiveACL(new int[0]); // hook all threads
                Log.Info("  [OK] WebsiteHook installed");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"  [FAIL] WebsiteHook: {ex.Message}");
                return false;
            }
        }

        public static void UnInstall()
        {
            _hook?.Dispose();
        }

        private static IntPtr Detour(
            IntPtr hwnd, string lpOperation, string lpFile,
            string lpParameters, string lpDirectory, int nShowCmd)
        {
            string original = lpFile;

            if (lpFile != null)
            {
                if (s_home.IsMatch(lpFile)) lpFile = _cfg.WebsiteHome ?? string.Empty;
                else if (s_credit.IsMatch(lpFile)) lpFile = _cfg.WebsiteCredit ?? string.Empty;
                else if (s_forum.IsMatch(lpFile)) lpFile = _cfg.WebsiteForum ?? string.Empty;
                else if (s_signup.IsMatch(lpFile)) lpFile = _cfg.WebsiteSignup ?? string.Empty;
                else if (s_signout.IsMatch(lpFile)) lpFile = _cfg.WebsiteSignout ?? string.Empty;
            }

            if (string.IsNullOrEmpty(lpFile))
            {
                Log.Info($"[WebsiteHook] Blocked popup for \"{original}\".");
                return SE_ERR_FNF;
            }

            if (lpFile != original)
            {
                Log.Info($"[WebsiteHook] Redirected popup from \"{original}\" to \"{lpFile}\".");
            }

            return ShellExecuteA(hwnd, lpOperation, lpFile, lpParameters, lpDirectory, nShowCmd);
        }
    }
}
