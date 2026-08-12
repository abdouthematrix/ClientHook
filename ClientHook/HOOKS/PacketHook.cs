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
        // Send uses ThisCall. Passes 'this' in the ECX register.
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int SendMsgDelegate(IntPtr thisPtr);

        // Receive uses Cdecl. Arguments: Opcode, Buffer Pointer, Explicit Length Descriptor
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte ReceiveMsgDelegate(uint opcode, IntPtr packetBuffer, int packetLength);

        private static LocalHook _sendHook;
        private static LocalHook _recvHook;
        private static PacketLogger _logger;

        private static SendMsgDelegate _originalSend;
        private static ReceiveMsgDelegate _originalReceive;

        public static bool SendHookInstalled { get; private set; }
        public static bool ReceiveHookInstalled { get; private set; }

        // Outgoing packet buffer offset within the SendMsg "this" instance, and the max payload
        // length used for the (Length - 4) < MaxPayloadLength sanity check on both directions --
        // both taken verbatim from the reference source's reversed constants.
        public static int SendPacketOffset { get; set; } = 4;
        public static int MaxPayloadLength { get; set; } = 0x3FD;

        public static bool Install(ClientConfig cfg)
        {
            if (!cfg.PacketLoggingEnabled)
            {
                Log.Info("  [SKIP] PacketHook disabled by config");
                return false;
            }

            SendPacketOffset = cfg.SendPacketOffset;
            MaxPayloadLength = cfg.MaxPayloadLength;
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
        
        private static int SendMsgDetour(IntPtr thisPtr)
        {
            try
            {
                if (thisPtr != IntPtr.Zero)
                {
                    IntPtr packetPtr = IntPtr.Add(thisPtr, SendPacketOffset);
                    ushort length = (ushort)Marshal.ReadInt16(packetPtr);
                    if ((uint)(length - 4) < (uint)MaxPayloadLength)
                    {
                        var data = new byte[length];
                        Marshal.Copy(packetPtr, data, 0, length);
                        _logger.LogPacket(PacketDirection.Send, data);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[PacketHook] SendDetour error: {ex.Message}");
            }

            // Always call the real original -- this hook never fabricates or alters a send.
            return _originalSend(thisPtr);
        }        

        private static byte ReceiveMsgDetour(uint opcode, IntPtr packetBuffer, int packetLength)
        {
            // Execute the original game logic first so the buffer stays populated and unmodified
            // by the time we read it for logging.
            byte result = _originalReceive(opcode, packetBuffer, packetLength);

            try
            {
                if (packetBuffer != IntPtr.Zero)
                {
                    short length = Marshal.ReadInt16(packetBuffer);

                    if ((uint)(length - 4) < (uint)MaxPayloadLength)
                    {
                        var data = new byte[length];
                        Marshal.Copy(packetBuffer, data, 0, length);
                        _logger.LogPacket(PacketDirection.Receive, data, opcode);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[PacketHook] RecvDetour error: {ex.Message}");
            }

            return result;
        }
    }
}
