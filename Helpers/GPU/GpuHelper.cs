using AutoOS.Views.Settings.Scheduling.Services;
using Microsoft.UI.Xaml.Data;
using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using static AutoOS.Views.Settings.Scheduling.Services.SetupApi;

namespace AutoOS.Helpers.GPU
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public partial class GpuInfo : INotifyPropertyChanged
    {
        private string deviceName;
        public string DeviceName
        {
            get => deviceName;
            set { if (deviceName != value) { deviceName = value; OnPropertyChanged(); } }
        }

        private string currentVersion;
        public string CurrentVersion
        {
            get => currentVersion;
            set { if (currentVersion != value) { currentVersion = value; OnPropertyChanged(); } }
        }

        private bool isInstalled;
        public bool IsInstalled
        {
            get => isInstalled;
            set { if (isInstalled != value) { isInstalled = value; OnPropertyChanged(); } }
        }

        private bool hdcp = false;
        public bool HDCP
        {
            get => hdcp;
            set { if (hdcp != value) { hdcp = value; OnPropertyChanged(); } }
        }

        private bool hdmidpaudio = true;
        public bool HDMIDPAudio
        {
            get => hdmidpaudio;
            set { if (hdmidpaudio != value) { hdmidpaudio = value; OnPropertyChanged(); } }
        }

        public string PnPDeviceId { get; set; }
        public string VendorId { get; set; }
        public string DeviceId { get; set; }
        public string Codename { get; set; }
        public string RegistryPath { get; set; }
        public bool NVIDIA => VendorId == "10de";
        public bool Install { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class VendorIdToBitmapIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string vendorId = value as string;
            string icon = vendorId switch
            {
                "10de" => "Nvidia.png",
                "1002" => "Amd.png",
                "8086" => "Intel.png",
                _ => null
            };

            return new BitmapIcon
            {
                UriSource = new Uri($"ms-appx:///Assets/Fluent/{icon}"),
                ShowAsMonochrome = false
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public static class GpuHelper
    {
        private static readonly HttpClient httpClient = new();

        public static async Task<List<GpuInfo>> DetectGPUs()
        {
            var gpus = new List<GpuInfo>();
            string deviceName = string.Empty;
            string codename = string.Empty;
            Dictionary<string, (string Vendor, Dictionary<string, string> Devices)> pciDb = null;

            Guid guid = new("4d36e968-e325-11ce-bfc1-08002be10318");
            IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_PRESENT);

            try
            {
                uint index = 0;
                SP_DEVINFO_DATA devInfo = new() { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

                while (SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfo))
                {
                    index++;

                    var sb = new StringBuilder(512);
                    SetupDiGetDeviceInstanceId(hDevInfo, ref devInfo, sb, sb.Capacity, out _);
                    
                    string registryPath = GetRegistryPath(hDevInfo, devInfo);
                    bool hdcp = false;
                    bool hdmidpaudio = true;
                    string pnpDeviceId = sb.ToString();

                    string vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
                    string deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("DEV_") + 4, 4).ToLowerInvariant();

                    string currentVersion = GetDriverVersion(hDevInfo, devInfo);
                    bool isInstalled = !currentVersion.StartsWith("10.0.") || !currentVersion.EndsWith(".1");

                    if (isInstalled && (vendorId == "10de" || vendorId == "1002" || vendorId == "8086"))
                    {
                        deviceName = GetDeviceName(hDevInfo, devInfo);
                        if (vendorId == "10de")
                        {
                            currentVersion = string.Concat(currentVersion.Split('.')[2].AsSpan()[1..], currentVersion.Split('.')[3].AsSpan()[..2], ".", currentVersion.Split('.')[3].AsSpan(2, 2));
                            hdcp = Registry.GetValue(registryPath, "RMHdcpKeyglobZero", null) is int intValue && intValue == 0;
                        }
                        else if (vendorId == "1002")
                        {
                            currentVersion = (Registry.GetValue(registryPath, "RadeonSoftwareVersion", null) ?? Registry.GetValue(registryPath, "FireproSoftwareVersion", null))?.ToString();
                        }
                        else if (vendorId == "8086")
                        {
                            currentVersion = currentVersion?.Split('.')[2] + "." + currentVersion?.Split('.')[3];
                        }
                        hdmidpaudio = new ManagementObjectSearcher("SELECT DeviceID, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Name LIKE '%High Definition Audio Controller%'").Get().Cast<ManagementObject>().Any(obj => obj["DeviceID"]?.ToString().Contains(pnpDeviceId[(pnpDeviceId.LastIndexOf('\\') + 1)..pnpDeviceId.LastIndexOf('&')], StringComparison.OrdinalIgnoreCase) == true && Convert.ToInt32(obj["ConfigManagerErrorCode"]) == 0);
                    }
                    else if (!isInstalled && (vendorId == "10de" || vendorId == "1002" || vendorId == "8086"))
                    {
                        if (pciDb == null)
                        {
                            string pciPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "pci.ids");

                            if (!File.Exists(pciPath))
                                await File.WriteAllBytesAsync(pciPath, await httpClient.GetByteArrayAsync("https://raw.githubusercontent.com/pciutils/pciids/master/pci.ids"));

                            pciDb = new Dictionary<string, (string Vendor, Dictionary<string, string> Devices)>(StringComparer.OrdinalIgnoreCase);

                            string currentVendor = null;
                            foreach (var line in File.ReadLines(pciPath))
                            {
                                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                                {
                                    if (!char.IsWhiteSpace(line[0]))
                                    {
                                        var parts = line.Split(' ', 2);
                                        if (parts.Length < 2) continue;
                                        currentVendor = parts[0].ToLowerInvariant();
                                        pciDb[currentVendor] = (parts[1].Trim(), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                                    }
                                    else if (line.StartsWith("\t") && currentVendor != null)
                                    {
                                        var parts = line.Trim().Split(' ', 2);
                                        if (parts.Length < 2) continue;
                                        pciDb[currentVendor].Devices[parts[0].ToLowerInvariant()] = parts[1].Trim();
                                    }
                                }
                            }
                        }

                        if (pciDb != null && pciDb.TryGetValue(vendorId, out var vendor) && vendor.Devices.TryGetValue(deviceId, out var name))
                        {
                            deviceName = name.Split('[', ']') is { Length: > 1 } parts ? parts[1] : name;
                            if (vendorId == "10de")
                            {
                                deviceName = $"NVIDIA {deviceName}";
                            }
                            else if (vendorId == "1002")
                            {
                                deviceName = $"AMD {deviceName}";
                            }
                            else if (vendorId == "8086")
                            {
                                deviceName = $"INTEL {deviceName}";
                            }
                            
                            codename = name.Split('[')[0].Trim();
                            currentVersion = "N/A";
                        }
                    }
                    else
                    {
                        continue;
                    }

                    gpus.Add(new GpuInfo
                    {
                        PnPDeviceId = pnpDeviceId,
                        DeviceName = deviceName,
                        VendorId = vendorId,
                        DeviceId = deviceId,
                        Codename = codename,
                        CurrentVersion = $"Current Version: {currentVersion}",
                        IsInstalled = isInstalled,
                        RegistryPath = registryPath,
                        HDCP = hdcp,
                        HDMIDPAudio = hdmidpaudio
                    });
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }

            return gpus;
        }

        public static async Task RefreshGpu(GpuInfo gpu)
        {
            var all = await DetectGPUs();

            var updated = all.FirstOrDefault(x =>
                x.PnPDeviceId == gpu.PnPDeviceId);

            if (updated == null)
                return;

            gpu.DeviceName = updated.DeviceName;
            gpu.CurrentVersion = updated.CurrentVersion;
            gpu.IsInstalled = updated.IsInstalled;
        }

        private static string GetDeviceName(IntPtr hDevInfo, SP_DEVINFO_DATA devInfo)
        {
            uint regType;
            uint requiredSize;

            IntPtr buffer = Marshal.AllocHGlobal(1024);

            SetupDiGetDeviceRegistryProperty(
                hDevInfo,
                ref devInfo,
                SPDRP.SPDRP_DEVICEDESC,
                out regType,
                buffer,
                1024,
                out requiredSize
            );

            string name = Marshal.PtrToStringUni(buffer);

            Marshal.FreeHGlobal(buffer);

            return name;
        }

        private static string GetDriverVersion(IntPtr hDevInfo, SP_DEVINFO_DATA devInfo)
        {
            SetupDiGetDeviceProperty(
                hDevInfo,
                ref devInfo,
                ref DEVPKEY_Device_DriverVersion,
                out _,
                null,
                0,
                out uint requiredSize,
                0
            );

            if (requiredSize == 0)
                return null;

            byte[] buffer = new byte[requiredSize];

            if (!SetupDiGetDeviceProperty(
                    hDevInfo,
                    ref devInfo,
                    ref DEVPKEY_Device_DriverVersion,
                    out _,
                    buffer,
                    (uint)buffer.Length,
                    out _,
                    0))
                return null;

            return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
        }

        private static string GetRegistryPath(IntPtr hDevInfo, SP_DEVINFO_DATA devInfo)
        {
            uint regType;
            uint requiredSize;

            IntPtr buffer = Marshal.AllocHGlobal(1024);

            SetupDiGetDeviceRegistryProperty(
                hDevInfo,
                ref devInfo,
                SPDRP.SPDRP_DRIVER,
                out regType,
                buffer,
                1024,
                out requiredSize
            );

            string driverKey = Marshal.PtrToStringUni(buffer);
            Marshal.FreeHGlobal(buffer);

            if (string.IsNullOrEmpty(driverKey))
                return null;

            return $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{driverKey}";
        }
    }
}