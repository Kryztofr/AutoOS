using Microsoft.UI.Xaml.Data;
using Microsoft.Win32;
using System.ComponentModel;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace AutoOS.Helpers.GPU;

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
    public static List<GpuInfo> GetGPUs()
    {
        var gpus = new List<GpuInfo>();
        string deviceName = string.Empty;
        string codename = string.Empty;
        Dictionary<string, (string Vendor, Dictionary<string, string> Devices)> pciDb = null;

        Guid guid = new("4d36e968-e325-11ce-bfc1-08002be10318");

        HDEVINFO hDevInfo;
        unsafe
        {
            hDevInfo = PInvoke.SetupDiGetClassDevs(&guid, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT);
        }

        if (hDevInfo.Value == (nint)(-1))
            return gpus;

        try
        {
            uint index = 0;
            bool moreDevices = true;

            while (moreDevices)
            {
                string pnpDeviceId = string.Empty;
                SP_DEVINFO_DATA devInfo = new() { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

                unsafe
                {
                    devInfo.cbSize = (uint)sizeof(SP_DEVINFO_DATA);
                    if (PInvoke.SetupDiEnumDeviceInfo(hDevInfo, index, &devInfo))
                    {
                        char* idBuffer = stackalloc char[512];
                        uint requiredSize = 0;
                        PInvoke.SetupDiGetDeviceInstanceId(hDevInfo, &devInfo, idBuffer, 512, &requiredSize);
                        pnpDeviceId = new string(idBuffer);
                    }
                    else
                    {
                        moreDevices = false;
                        continue;
                    }
                }

                index++;

                string registryPath = GetRegistryPath(hDevInfo, devInfo);
                bool hdcp = false;
                bool hdmidpaudio = true;

                string vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
                string deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("DEV_") + 4, 4).ToLowerInvariant();

                string currentVersion = GetDriverVersion(hDevInfo, devInfo);
                bool isInstalled = !string.IsNullOrEmpty(currentVersion) && (!currentVersion.StartsWith("10.0.") || !currentVersion.EndsWith(".1"));

                if (isInstalled && (vendorId == "10de" || vendorId == "1002" || vendorId == "8086"))
                {
                    deviceName = GetDeviceName(hDevInfo, devInfo);
                    if (vendorId == "10de")
                    {
                        var versionParts = currentVersion.Split('.');
                        currentVersion = string.Concat(versionParts[2].AsSpan()[1..], versionParts[3].AsSpan()[..2], ".", versionParts[3].AsSpan(2, 2));
                        hdcp = Registry.GetValue(registryPath, "RMHdcpKeyglobZero", null) is int intValue && intValue == 0;
                    }
                    else if (vendorId == "1002")
                    {
                        currentVersion = (Registry.GetValue(registryPath, "RadeonSoftwareVersion", null) ?? Registry.GetValue(registryPath, "FireproSoftwareVersion", null))?.ToString();
                    }
                    else if (vendorId == "8086")
                    {
                        var versionParts = currentVersion?.Split('.');
                        currentVersion = versionParts?.Length >= 4 ? versionParts[2] + "." + versionParts[3] : currentVersion;
                    }
                    hdmidpaudio = new ManagementObjectSearcher("SELECT DeviceID, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Name LIKE '%High Definition Audio Controller%'").Get().Cast<ManagementObject>().Any(obj => obj["DeviceID"]?.ToString().Contains(pnpDeviceId[(pnpDeviceId.LastIndexOf('\\') + 1)..pnpDeviceId.LastIndexOf('&')], StringComparison.OrdinalIgnoreCase) == true && Convert.ToInt32(obj["ConfigManagerErrorCode"]) == 0);
                }
                else if (!isInstalled && (vendorId == "10de" || vendorId == "1002" || vendorId == "8086"))
                {
                    if (pciDb == null)
                    {
                        string pciPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "pci.ids");

                        if (!File.Exists(pciPath))
                            File.WriteAllBytes(pciPath, httpClient.GetByteArrayAsync("https://raw.githubusercontent.com/pciutils/pciids/master/pci.ids").GetAwaiter().GetResult());

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
                        if (vendorId == "10de") deviceName = $"NVIDIA {deviceName}";
                        else if (vendorId == "1002") deviceName = $"AMD {deviceName}";
                        else if (vendorId == "8086") deviceName = $"INTEL {deviceName}";

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
            PInvoke.SetupDiDestroyDeviceInfoList(hDevInfo);
        }

        return gpus;
    }

    public static void RefreshGpu(GpuInfo gpu)
    {
        var all = GetGPUs();

        var updated = all.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId);

        if (updated == null)
            return;

        gpu.DeviceName = updated.DeviceName;
        gpu.CurrentVersion = updated.CurrentVersion;
        gpu.IsInstalled = updated.IsInstalled;
    }

    private unsafe static string GetDeviceName(HDEVINFO hDevInfo, SP_DEVINFO_DATA devInfo)
    {
        uint regType;
        uint requiredSize;

        byte* buffer = stackalloc byte[1024];

        bool success = PInvoke.SetupDiGetDeviceRegistryProperty(
            hDevInfo,
            &devInfo,
            (uint)SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC,
            &regType,
            buffer,
            1024,
            &requiredSize
        );

        if (!success)
        {
            return string.Empty;
        }

        return new string((char*)buffer);
    }

    private unsafe static string GetDriverVersion(HDEVINFO hDevInfo, SP_DEVINFO_DATA devInfo)
    {
        uint requiredSize;
        Windows.Win32.Devices.Properties.DEVPROPTYPE propType;
        var propertyKey = PInvoke.DEVPKEY_Device_DriverVersion;

        PInvoke.SetupDiGetDeviceProperty(
            hDevInfo,
            &devInfo,
            &propertyKey,
            &propType,
            null,
            0,
            &requiredSize,
            0
        );

        if (requiredSize == 0)
            return null;

        byte* buffer = stackalloc byte[(int)requiredSize];

        if (!PInvoke.SetupDiGetDeviceProperty(
                hDevInfo,
                &devInfo,
                &propertyKey,
                &propType,
                buffer,
                requiredSize,
                null,
                0))
            return null;

        return new string((char*)buffer);
    }

    private unsafe static string GetRegistryPath(HDEVINFO hDevInfo, SP_DEVINFO_DATA devInfo)
    {
        uint regType;
        uint requiredSize;

        byte* buffer = stackalloc byte[1024];

        PInvoke.SetupDiGetDeviceRegistryProperty(
            hDevInfo,
            &devInfo,
            SETUP_DI_REGISTRY_PROPERTY.SPDRP_DRIVER,
            &regType,
            buffer,
            1024,
            &requiredSize
        );

        string driverKey = new((char*)buffer);

        if (string.IsNullOrEmpty(driverKey))
            return null;

        return $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{driverKey}";
    }
}