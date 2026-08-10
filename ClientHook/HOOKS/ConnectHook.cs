using EasyHook;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ClientHook
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SockAddrIn
    {
        public short sin_family;
        public ushort sin_port;      // network byte order
        public uint sin_addr;        // network byte order
        public long sin_zero;
    }

    /// <summary>
    /// Hook for WinSock2 connect(). Redirects account/game server connections
    /// based on ClientConfig settings.
    /// </summary>
    public sealed class ConnectHook
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate int ConnectDelegate(IntPtr s, ref SockAddrIn name, int namelen);

        [DllImport("ws2_32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern int connect(IntPtr s, ref SockAddrIn name, int namelen);

        [DllImport("ws2_32.dll", CharSet = CharSet.Ansi)]
        private static extern int inet_pton(int family, string ipString, out uint buf);

        private static LocalHook _hook;
        private static ClientConfig _cfg;

        public static bool Install(ClientConfig cfg)
        {
            try
            {
                _cfg = cfg;

                _hook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "connect"),
                    new ConnectDelegate(Detour),
                    null);

                _hook.ThreadACL.SetExclusiveACL(new int[0]); // hook all threads
                Log.Info("  [OK] ConnectHook installed");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"  [FAIL] ConnectHook: {ex.Message}");
                return false;
            }
        }

        public static void UnInstall()
        {
            _hook?.Dispose();
        }

        private static int Detour(IntPtr s, ref SockAddrIn name, int namelen)
        {
            int port = IPAddress.NetworkToHostOrder((short)name.sin_port) & 0xFFFF;

            try
            {
                if (_cfg.AccountServerRedirect && port == _cfg.AccountServerPort)
                {
                    inet_pton(AddressFamily.InterNetwork.GetHashCode(), _cfg.AccountServerIP, out uint addr);
                    name.sin_addr = addr;
                    name.sin_port = (ushort)IPAddress.HostToNetworkOrder((short)_cfg.AccountServerPort);

                    Log.Info($"[ConnectHook] AccountServer -> {_cfg.AccountServerIP}:{_cfg.AccountServerPort}");
                }
                else if (_cfg.GameServerRedirect && port == _cfg.GameServerPort)
                {
                    inet_pton(AddressFamily.InterNetwork.GetHashCode(), _cfg.GameServerIP, out uint addr);
                    name.sin_addr = addr;
                    name.sin_port = (ushort)IPAddress.HostToNetworkOrder((short)_cfg.GameServerPort);

                    Log.Info($"[ConnectHook] GameServer -> {_cfg.GameServerIP}:{_cfg.GameServerPort}");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[ConnectHook] Exception: {ex.Message}");
            }

            return connect(s, ref name, namelen);
        }
    }
}
