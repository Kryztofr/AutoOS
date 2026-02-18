using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AutoOS.Helpers.RAM
{
    public class RamInfo
    {
        public string DDRVersion { get; set; } = "";
        public double CapacityGB { get; set; } = 0;
        public int MaxSpeedMHz { get; set; } = 0;
    }

    public static partial class RamHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
                dwMemoryLoad = 0;
                ullTotalPhys = 0;
                ullAvailPhys = 0;
                ullTotalPageFile = 0;
                ullAvailPageFile = 0;
                ullTotalVirtual = 0;
                ullAvailVirtual = 0;
                ullAvailExtendedVirtual = 0;
            }
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial uint GetSystemFirmwareTable(uint firmwareTableProviderSignature, uint firmwareTableID, ref byte pFirmwareTableBuffer, uint bufferSize);

        private const uint RSMB = 0x52534D42;

        public static RamInfo GetRamDetails()
        {
            var info = new RamInfo();

            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(ref memStatus))
                info.CapacityGB = Math.Round(memStatus.ullTotalPhys / 1024.0 / 1024.0 / 1024.0, 1);

            uint bufferSize = GetSystemFirmwareTable(RSMB, 0, ref Unsafe.AsRef<byte>(ref MemoryMarshal.GetReference(Memory<byte>.Empty.Span)), 0);
            if (bufferSize == 0) return info;

            byte[] buffer = new byte[bufferSize];
            GetSystemFirmwareTable(RSMB, 0, ref buffer[0], bufferSize);

            int offset = 8;
            while (offset + 4 < buffer.Length)
            {
                byte type = buffer[offset];
                byte length = buffer[offset + 1];

                if (type == 17 && offset + length <= buffer.Length)
                {
                    int speed = BitConverter.ToUInt16(buffer, offset + 0x20);
                    if (length >= 0x58)
                    {
                        int extSpeed = BitConverter.ToInt32(buffer, offset + 0x54);
                        if (extSpeed > 0) speed = extSpeed;
                    }
                    if (speed == 0) speed = BitConverter.ToUInt16(buffer, offset + 0x15);
                    if (speed > info.MaxSpeedMHz) info.MaxSpeedMHz = speed;

                    byte memType = buffer[offset + 0x12];
                    info.DDRVersion = memType switch
                    {
                        0x12 => "DDR",
                        0x13 => "DDR2",
                        0x18 => "DDR3",
                        0x1A => "DDR4",
                        0x22 => "DDR5",
                        _ => "DDRx"
                    };
                }

                offset += length;
                while (offset + 1 < buffer.Length && (buffer[offset] != 0 || buffer[offset + 1] != 0))
                    offset++;
                offset += 2;
            }

            return info;
        }
    }
}
