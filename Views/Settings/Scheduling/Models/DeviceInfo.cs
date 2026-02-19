using Microsoft.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace AutoOS.Views.Settings.Scheduling.Models;

public class DeviceInfo
{
    internal HDEVINFO DeviceInfoSet { get; set; }
    internal SP_DEVINFO_DATA DeviceInfoData { get; set; }
    public RegistryKey RegistryKey { get; set; }
    public string DeviceDesc { get; set; } = string.Empty;
    public string DevObjName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string LocationInformation { get; set; } = string.Empty;
    public string PnpDeviceId { get; set; } = string.Empty;
    public uint MaxMSILimit { get; set; }
}
