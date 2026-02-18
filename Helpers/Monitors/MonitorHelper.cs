using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace AutoOS.Helpers.Monitor
{
    public class MonitorInfo
    {
        public string DeviceName { get; set; } = "";
        public (uint Width, uint Height) Resolution { get; set; }
        public uint RefreshRate { get; set; }
    }

    public static partial class MonitorHelper
    {
        private const int ENUM_CURRENT_SETTINGS = -1;
        private const uint CDS_UPDATEREGISTRY = 0x00000001;
        private const uint CDS_GLOBAL = 0x00000008;
        private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct DEVMODE
        {
            public fixed byte dmDeviceName[32];
            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            public fixed byte dmFormName[32];
            public ushort dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency;
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct DISPLAY_DEVICE
        {
            public int cb;
            public fixed byte DeviceName[32];
            public fixed byte DeviceString[128];
            public int StateFlags;
            public fixed byte DeviceID[128];
            public fixed byte DeviceKey[128];
        }

        [LibraryImport("user32.dll", EntryPoint = "EnumDisplaySettingsA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EnumDisplaySettings([MarshalAs(UnmanagedType.LPStr)] string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [LibraryImport("user32.dll", EntryPoint = "EnumDisplayDevicesA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EnumDisplayDevices([MarshalAs(UnmanagedType.LPStr)] string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [LibraryImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExA")]
        private static partial int ChangeDisplaySettingsEx([MarshalAs(UnmanagedType.LPStr)] string? lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        public static unsafe List<MonitorInfo> GetMonitors()
        {
            List<MonitorInfo> monitors = new();
            var hardwareNames = GetModelNamesFromRegistry();
            DISPLAY_DEVICE adapter = new() { cb = sizeof(DISPLAY_DEVICE) };
            uint i = 0;

            while (EnumDisplayDevices(null, i++, ref adapter, 0))
            {
                string adapterPath = PtrToString(adapter.DeviceName, 32);
                DISPLAY_DEVICE monitorDevice = new() { cb = sizeof(DISPLAY_DEVICE) };
                uint j = 0;

                while (EnumDisplayDevices(adapterPath, j++, ref monitorDevice, EDD_GET_DEVICE_INTERFACE_NAME))
                {
                    if ((monitorDevice.StateFlags & 0x1) != 0)
                    {
                        DEVMODE dm = new() { dmSize = (ushort)sizeof(DEVMODE) };
                        if (EnumDisplaySettings(adapterPath, ENUM_CURRENT_SETTINGS, ref dm))
                        {
                            string interfacePath = PtrToString(monitorDevice.DeviceID, 128);
                            string hwId = ExtractHardwareId(interfacePath);

                            monitors.Add(new MonitorInfo
                            {
                                DeviceName = hardwareNames.TryGetValue(hwId, out var name) ? name : PtrToString(monitorDevice.DeviceString, 128),
                                Resolution = (dm.dmPelsWidth, dm.dmPelsHeight),
                                RefreshRate = dm.dmDisplayFrequency
                            });
                        }
                    }
                }
            }
            return monitors;
        }

        public static unsafe void SetHighestRefreshRates()
        {
            DISPLAY_DEVICE adapter = new() { cb = sizeof(DISPLAY_DEVICE) };
            uint i = 0;

            while (EnumDisplayDevices(null, i++, ref adapter, 0))
            {
                string adapterPath = PtrToString(adapter.DeviceName, 32);
                DEVMODE current = new() { dmSize = (ushort)sizeof(DEVMODE) };
                if (!EnumDisplaySettings(adapterPath, ENUM_CURRENT_SETTINGS, ref current)) continue;

                DEVMODE best = current;
                for (int j = 0; ; j++)
                {
                    DEVMODE test = new() { dmSize = (ushort)sizeof(DEVMODE) };
                    if (!EnumDisplaySettings(adapterPath, j, ref test)) break;

                    if (test.dmPelsWidth == current.dmPelsWidth &&
                        test.dmPelsHeight == current.dmPelsHeight &&
                        test.dmDisplayFrequency > best.dmDisplayFrequency)
                    {
                        best = test;
                    }
                }

                if (best.dmDisplayFrequency > current.dmDisplayFrequency)
                {
                    ChangeDisplaySettingsEx(adapterPath, ref best, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_GLOBAL, IntPtr.Zero);
                }
            }
        }

        private static Dictionary<string, string> GetModelNamesFromRegistry()
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var monitorKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY");
                if (monitorKey == null) return results;

                foreach (var hwId in monitorKey.GetSubKeyNames())
                {
                    using var instanceKey = monitorKey.OpenSubKey(hwId);
                    if (instanceKey == null) continue;

                    foreach (var instance in instanceKey.GetSubKeyNames())
                    {
                        using var details = instanceKey.OpenSubKey($@"{instance}\Device Parameters");
                        if (details == null) continue;

                        if (details.GetValue("EDID") is byte[] edid)
                        {
                            string model = ParseEdidForModel(edid);
                            if (!string.IsNullOrEmpty(model)) results[hwId] = model;
                        }
                    }
                }
            }
            catch { }
            return results;
        }

        private static string ExtractHardwareId(string path)
        {
            var parts = path.Split('#');
            return parts.Length > 1 ? parts[1] : "";
        }

        private static string ParseEdidForModel(byte[] edid)
        {
            for (int i = 54; i < 108; i += 18)
            {
                if (edid.Length >= i + 18 && edid[i] == 0 && edid[i + 1] == 0 && edid[i + 2] == 0 && edid[i + 3] == 0xFC)
                {
                    return Encoding.ASCII.GetString(edid, i + 5, 13).Replace("\0", "").Trim();
                }
            }
            return "";
        }

        private static unsafe string PtrToString(byte* ptr, int size)
        {
            int len = 0;
            while (len < size && ptr[len] != 0) len++;
            return Encoding.ASCII.GetString(ptr, len);
        }
    }
}