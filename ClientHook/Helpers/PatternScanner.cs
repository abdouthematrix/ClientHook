using System;
using System.Runtime.InteropServices;

namespace ClientHook
{
    /// <summary>
    /// Signature-based byte pattern scanner. Runs inside the target process
    /// (post-injection), scanning our own address space directly.
    /// </summary>
    internal static class PatternScanner
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualQuery(IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        /// <summary>
        /// Parses a hex AOB string like "56 68 4C 04 ?? ?? 8B F1"
        /// into (pattern bytes, mask) where mask[i] == true means "must match".
        /// </summary>
        public static (byte[] pattern, bool[] mask) ParseHexPattern(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return (Array.Empty<byte>(), Array.Empty<bool>());
            }

            string[] tokens = hex.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var pattern = new byte[tokens.Length];
            var mask = new bool[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "??" || tokens[i] == "?")
                {
                    pattern[i] = 0x00;
                    mask[i] = false;
                }
                else
                {
                    pattern[i] = Convert.ToByte(tokens[i], 16);
                    mask[i] = true;
                }
            }

            return (pattern, mask);
        }

        /// <summary>
        /// Scans all readable/committed regions of the main module for the given pattern.
        /// Returns IntPtr.Zero if not found.
        /// </summary>
        public static IntPtr FindPatternInMainModule(byte[] pattern, bool[] mask)
        {
            if (pattern.Length == 0 || pattern.Length != mask.Length)
            {
                throw new ArgumentException("Pattern/mask length mismatch or empty pattern.");
            }

            IntPtr moduleBase = GetModuleHandle(null);
            IntPtr cursor = moduleBase;

            while (VirtualQuery(cursor, out MEMORY_BASIC_INFORMATION info, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()))
            {
                if (info.AllocationBase != moduleBase)
                {
                    break;
                }

                const uint MEM_COMMIT = 0x1000;
                const uint PAGE_NOACCESS = 0x01;
                const uint PAGE_GUARD = 0x100;

                bool readable = info.State == MEM_COMMIT
                    && info.Protect != PAGE_NOACCESS
                    && (info.Protect & PAGE_GUARD) == 0;

                if (readable)
                {
                    long regionSize = info.RegionSize.ToInt64();
                    IntPtr found = ScanRegion(info.BaseAddress, regionSize, pattern, mask);
                    if (found != IntPtr.Zero)
                    {
                        return found;
                    }
                }

                cursor = IntPtr.Add(info.BaseAddress, (int)info.RegionSize);
            }

            return IntPtr.Zero;
        }

        private static IntPtr ScanRegion(IntPtr baseAddr, long size, byte[] pattern, bool[] mask)
        {
            byte[] buffer;
            try
            {
                buffer = new byte[size];
                Marshal.Copy(baseAddr, buffer, 0, (int)size);
            }
            catch
            {
                return IntPtr.Zero; // unreadable region, skip
            }

            int patternLength = pattern.Length;
            for (long i = 0; i <= buffer.LongLength - patternLength; i++)
            {
                bool match = true;
                for (int j = 0; j < patternLength; j++)
                {
                    if (mask[j] && buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return IntPtr.Add(baseAddr, (int)i);
                }
            }

            return IntPtr.Zero;
        }
    }
}
