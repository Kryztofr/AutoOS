using Microsoft.UI.Xaml.Data;
using Microsoft.Win32;
using System.ComponentModel;
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
    public unsafe static List<GpuInfo> GetGPUs()
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
                    Guid systemGuid = new("4d36e97d-e325-11ce-bfc1-08002be10318");
                    HDEVINFO hAudioDevInfo = PInvoke.SetupDiGetClassDevs(&systemGuid, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT);
                    if (hAudioDevInfo.Value != (nint)(-1))
                    {
                        try
                        {
                            uint audioIndex = 0;
                            SP_DEVINFO_DATA audioDevInfo = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
                            while (PInvoke.SetupDiEnumDeviceInfo(hAudioDevInfo, audioIndex++, &audioDevInfo))
                            {
                                char* audioBuffer = stackalloc char[512];
                                uint audioRequired;
                                if (!PInvoke.SetupDiGetDeviceInstanceId(hAudioDevInfo, &audioDevInfo, audioBuffer, 512, &audioRequired))
                                    continue;

                                string audioId = new(audioBuffer);
                                string fragment = pnpDeviceId[(pnpDeviceId.LastIndexOf('\\') + 1)..pnpDeviceId.LastIndexOf('&')];

                                if (!audioId.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                if (PInvoke.CM_Locate_DevNode(out uint devInst, new PWSTR(audioBuffer), CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL) == CONFIGRET.CR_SUCCESS)
                                {
                                    CM_DEVNODE_STATUS_FLAGS status;
                                    CM_PROB problem;

                                    if (PInvoke.CM_Get_DevNode_Status(out status, out problem, devInst, 0) == CONFIGRET.CR_SUCCESS)
                                    {
                                        hdmidpaudio = (status & CM_DEVNODE_STATUS_FLAGS.DN_STARTED) != 0;
                                        break;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            PInvoke.SetupDiDestroyDeviceInfoList(hAudioDevInfo);
                        }
                    }
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

    public static unsafe void ToggleHdmiDpAudio(string gpuPnPDeviceId, bool enable)
    {
        string fragment = gpuPnPDeviceId[(gpuPnPDeviceId.LastIndexOf('\\') + 1)..gpuPnPDeviceId.LastIndexOf('&')];
        Guid hdaudioGuid = new("4d36e97d-e325-11ce-bfc1-08002be10318");

        HDEVINFO hDevInfo = PInvoke.SetupDiGetClassDevs(&hdaudioGuid, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT);
        if (hDevInfo.Value == (nint)(-1)) return;

        try
        {
            uint index = 0;
            SP_DEVINFO_DATA devInfo = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };

            while (PInvoke.SetupDiEnumDeviceInfo(hDevInfo, index++, &devInfo))
            {
                char* buffer = stackalloc char[512];
                uint requiredSize;

                if (!PInvoke.SetupDiGetDeviceInstanceId(hDevInfo, &devInfo, buffer, 512, &requiredSize))
                    continue;

                string instanceId = new(buffer);

                if (!instanceId.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    continue;

                var propChangeParams = new SP_PROPCHANGE_PARAMS
                {
                    ClassInstallHeader = new SP_CLASSINSTALL_HEADER
                    {
                        cbSize = (uint)sizeof(SP_CLASSINSTALL_HEADER),
                        InstallFunction = DI_FUNCTION.DIF_PROPERTYCHANGE
                    },
                    StateChange = enable ? SETUP_DI_STATE_CHANGE.DICS_ENABLE : SETUP_DI_STATE_CHANGE.DICS_DISABLE,
                    Scope = SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_GLOBAL,
                    HwProfile = 0
                };

                if (PInvoke.SetupDiSetClassInstallParams(
                    hDevInfo,
                    &devInfo,
                    (SP_CLASSINSTALL_HEADER*)&propChangeParams,
                    (uint)sizeof(SP_PROPCHANGE_PARAMS)))
                {
                    PInvoke.SetupDiCallClassInstaller(DI_FUNCTION.DIF_PROPERTYCHANGE, hDevInfo, &devInfo);
                }
            }
        }
        finally
        {
            PInvoke.SetupDiDestroyDeviceInfoList(hDevInfo);
        }
    }
}