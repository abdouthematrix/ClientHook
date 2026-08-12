using EasyHook;
using System;
using System.Runtime.InteropServices;

namespace ClientHook
{
    /// <summary>
    /// Hooks packet send/recv functions by scanning for configured patterns.
    /// Captures packets and logs them to file and optionally to a live viewer.
    /// </summary>
    public sealed class PacketHook
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate int SendMsgDelegate(IntPtr socket, IntPtr buffer, int length, int flags);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate int ReceiveMsgDelegate(IntPtr socket, IntPtr buffer, int length, int flags);

        private static LocalHook _sendHook;
        private static LocalHook _recvHook;
        private static PacketLogger _logger;

        private static SendMsgDelegate _originalSend;
        private static ReceiveMsgDelegate _originalReceive;

        public static bool SendHookInstalled { get; private set; }
        public static bool ReceiveHookInstalled { get; private set; }

        public static bool Install(ClientConfig cfg)
        {
            if (!cfg.PacketLoggingEnabled)
            {
                Log.Info("  [SKIP] PacketHook disabled by config");
                return false;
            }

            try
            {
                _logger = new PacketLogger(cfg.PacketLogPath) { ShowViewer = cfg.PacketViewerEnabled };

                // --- Send pattern ---
                (byte[] sendPattern, bool[] sendMask) = PatternScanner.ParseHexPattern(cfg.SendPatternHex);
                if (sendPattern.Length > 0)
                {
                    IntPtr sendAddr = PatternScanner.FindPatternInMainModule(sendPattern, sendMask);
                    if (sendAddr != IntPtr.Zero)
                    {
                        _logger.Log($"SendMsg pattern found at 0x{(long)sendAddr:X}");
                        Log.Info($"SendMsg pattern found at 0x{(long)sendAddr:X}; hook installed.");
                        _sendHook = LocalHook.Create(sendAddr, new SendMsgDelegate(SendMsgDetour), null);
                        _sendHook.ThreadACL.SetExclusiveACL(new int[0]);
                        _originalSend = Marshal.GetDelegateForFunctionPointer<SendMsgDelegate>(_sendHook.HookBypassAddress);
                        SendHookInstalled = true;
                    }
                    else
                    {
                        _logger.Log("SendMsg pattern not found in target module.");
                        Log.Info("SendMsg pattern not found in target module -- send capture disabled.");
                    }
                }

                // --- Receive pattern ---
                (byte[] recvPattern, bool[] recvMask) = PatternScanner.ParseHexPattern(cfg.ReceivePatternHex);
                if (recvPattern.Length > 0)
                {
                    IntPtr recvAddr = PatternScanner.FindPatternInMainModule(recvPattern, recvMask);
                    if (recvAddr != IntPtr.Zero)
                    {
                        _logger.Log($"ReceiveMsg pattern found at 0x{(long)recvAddr:X}");
                        Log.Info($"ReceiveMsg pattern found at 0x{(long)recvAddr:X}; hook installed.");
                        _recvHook = LocalHook.Create(recvAddr, new ReceiveMsgDelegate(ReceiveMsgDetour), null);
                        _recvHook.ThreadACL.SetExclusiveACL(new int[0]);
                        _originalReceive = Marshal.GetDelegateForFunctionPointer<ReceiveMsgDelegate>(_recvHook.HookBypassAddress);
                        ReceiveHookInstalled = true;
                    }
                    else
                    {
                        _logger.Log("ReceiveMsg pattern not found in target module.");
                        Log.Info("ReceiveMsg pattern not found in target module -- receive capture disabled.");
                    }
                }

                if (SendHookInstalled || ReceiveHookInstalled)
                {
                    Log.Info("  [OK] PacketHook installed");
                    return true;
                }
                else
                {
                    Log.Info("  [FAIL] PacketHook: no patterns found, nothing hooked.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Info($"  [FAIL] PacketHook: {ex.Message}");
                return false;
            }
        }

        public static void UnInstall()
        {
            _sendHook?.Dispose();
            _recvHook?.Dispose();
            SendHookInstalled = false;
            ReceiveHookInstalled = false;
        }

        private static int SendMsgDetour(IntPtr socket, IntPtr buffer, int length, int flags)
        {
            try
            {
                byte[] data = new byte[length];
                Marshal.Copy(buffer, data, 0, length);
                _logger.LogPacket(PacketDirection.Send, data);
            }
            catch (Exception ex)
            {
                Log.Info($"[PacketHook] SendDetour error: {ex.Message}");
            }

            return _originalSend(socket, buffer, length, flags);
        }

        private static int ReceiveMsgDetour(IntPtr socket, IntPtr buffer, int length, int flags)
        {
            int result = _originalReceive(socket, buffer, length, flags);

            if (result > 0)
            {
                try
                {
                    byte[] data = new byte[result];
                    Marshal.Copy(buffer, data, 0, result);
                    _logger.LogPacket(PacketDirection.Receive, data);
                }
                catch (Exception ex)
                {
                    Log.Info($"[PacketHook] RecvDetour error: {ex.Message}");
                }
            }

            return result;
        }
    }
}
