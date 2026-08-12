using System;
using System.IO;
using System.Text;

namespace ClientHook
{
    public enum PacketDirection
    {
        Send,
        Receive
    }

    /// <summary>
    /// Logs captured packets to a file and optionally forwards them to a live WPF viewer.
    /// </summary>
    public sealed class PacketLogger
    {
        private readonly object _lock = new object();
        private readonly string _logPath;

        public PacketLogger(string logPath)
        {
            _logPath = logPath;
        }

        /// <summary>
        /// When true, packets are also forwarded to a live WPF viewer window.
        /// </summary>
        public bool ShowViewer { get; set; }

        public void Log(string message)
        {
            WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public void LogPacket(PacketDirection direction, byte[] data, uint? opcode = null)
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");
            sb.Append(direction == PacketDirection.Send ? "SEND" : "RECV");

            if (opcode.HasValue)
            {
                sb.Append(" opcode=0x").Append(opcode.Value.ToString("X4"));
            }

            sb.Append(" len=").Append(data.Length).Append(": ");
            sb.Append(ToHex(data));

            WriteLine(sb.ToString());

            if (ShowViewer)
            {
                try
                {
                    WpfViewerHost.ShowPacket(direction, data);
                }
                catch (Exception ex)
                {
                    Log($"Viewer error: {ex.Message}");
                }
            }
        }

        private static string ToHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                sb.Append(b.ToString("X2")).Append(' ');
            }
            return sb.ToString().TrimEnd();
        }

        private void WriteLine(string line)
        {
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
                catch
                {
                    // Best-effort logging — failures don’t crash the hook.
                }
            }
        }
    }
}
