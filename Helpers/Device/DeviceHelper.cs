using AutoOS.Helpers.ReadWrite;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using WinRT;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.IpHelper;
using Windows.Win32.NetworkManagement.Ndis;
using static Windows.Win32.Devices.DeviceAndDriverInstallation.SETUP_DI_GET_CLASS_DEVS_FLAGS;
using static Windows.Win32.Devices.DeviceAndDriverInstallation.SETUP_DI_REGISTRY_PROPERTY;

namespace AutoOS.Helpers.Device;

public enum DeviceType
{
    GPU,
    XHCI,
    NIC,
    HID,
    Other
}

public enum DeviceState
{
    Enabled,
    Disabled,
    Error
}

public enum NicDriverType
{
    NDIS,
    NetAdapterCx
}

public enum NicDeviceType
{
    LAN,
    WiFi,
    Virtual
}

public enum XhciDeviceType
{
    Controller,
    Hub
}

[GeneratedBindableCustomProperty]
public partial class DeviceInfo : INotifyPropertyChanged
{
    public string FriendlyName { get; set; } = string.Empty;
    public string DeviceDescription { get; set; } = string.Empty;
    public string PnpDeviceId { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string RegistryPath { get; set; } = string.Empty;
    public DeviceState State { get; set; } = DeviceState.Error;
    public string DevObjName { get; set; } = string.Empty;
    public uint MsiSupported { get; set; }
    public uint MsiLimit { get; set; }
    public uint MaxMsiLimit { get; set; }
    public uint DevicePolicy { get; set; }
    public uint DevicePriority { get; set; }
    public ulong AssignmentSetOverride { get; set; }
    public NicDriverType? DriverType { get; set; }
    public NicDeviceType? NicType { get; set; }
    private bool isActive;
    public bool IsActive
    {
        get => isActive;
        set { if (isActive != value) { isActive = value; OnPropertyChanged(); } }
    }
    public XhciDeviceType? XhciType { get; set; }
    public ulong? BaseAddress { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal static class DeviceHelper
{
    private static readonly Guid GUID_DEVCLASS_DISPLAY = new("4d36e968-e325-11ce-bfc1-08002be10318");
    private static readonly Guid GUID_DEVCLASS_USB = new("36fc9e60-c465-11cf-8056-444553540000");
    private static readonly Guid GUID_DEVCLASS_NET = new("4d36e972-e325-11ce-bfc1-08002be10318");
    private static readonly Guid GUID_DEVCLASS_HID = new("745a17a0-74d3-11d0-b6fe-00a0c90f57da");

    public class ApplyResult
    {
        public bool Success { get; set; }
        public bool NeedsRestart { get; set; }
        public List<DeviceInfo> ChangedDevices { get; set; } = [];
        public Dictionary<string, DeviceInfo> AppliedSettings { get; set; } = [];
    }

    public class RestartResult
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> FailedDevices { get; set; } = [];
    }

    public unsafe static List<DeviceInfo> GetDevices(DeviceType type)
    {
        var devices = new List<DeviceInfo>();
        Guid classGuid = type switch
        {
            DeviceType.GPU => GUID_DEVCLASS_DISPLAY,
            DeviceType.XHCI => GUID_DEVCLASS_USB,
            DeviceType.NIC => GUID_DEVCLASS_NET,
            DeviceType.HID => GUID_DEVCLASS_HID,
            DeviceType.Other => default,
            _ => throw new ArgumentException("Unknown device type")
        };

        var flags = DIGCF_PRESENT;
        if (type == DeviceType.Other) flags |= DIGCF_ALLCLASSES;

        HDEVINFO deviceInfoSetHandle = PInvoke.SetupDiGetClassDevs(type == DeviceType.Other ? null : &classGuid, null, HWND.Null, flags);
        if (deviceInfoSetHandle.Value == -1) return devices;

        const int MAX_DEVICE_ID_LEN = 256;
        char* pIdBuffer = stackalloc char[MAX_DEVICE_ID_LEN];

        uint index = 0;
        while (true)
        {
            SP_DEVINFO_DATA deviceInfoData = default;
            deviceInfoData.cbSize = (uint)sizeof(SP_DEVINFO_DATA);

            if (!PInvoke.SetupDiEnumDeviceInfo(deviceInfoSetHandle, index++, &deviceInfoData)) break;

            string enumerator = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_ENUMERATOR_NAME);
            string service = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_SERVICE);

            if (type == DeviceType.GPU)
            {
                if (!string.Equals(enumerator, "PCI", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(service, "BasicDisplay", StringComparison.OrdinalIgnoreCase)) continue;
            }
            else if (type == DeviceType.XHCI)
            {
                if (!string.Equals(service, "USBXHCI", StringComparison.OrdinalIgnoreCase)) continue;
            }
            else if (type == DeviceType.NIC || type == DeviceType.Other)
            {
                if (!string.Equals(enumerator, "PCI", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(enumerator, "USB", StringComparison.OrdinalIgnoreCase)) continue;

                if (type == DeviceType.Other)
                {
                    string classGuidStr = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_CLASSGUID);
                    if (Guid.TryParse(classGuidStr, out Guid devGuid))
                    {
                        if (devGuid == GUID_DEVCLASS_DISPLAY || devGuid == GUID_DEVCLASS_NET || string.Equals(service, "USBXHCI", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                }
            }

            string pnpDeviceId = string.Empty;
            uint requiredSize = 0;
            PInvoke.SetupDiGetDeviceInstanceId(deviceInfoSetHandle, &deviceInfoData, null, 0, &requiredSize);
            if (requiredSize > 0)
            {
                if (requiredSize <= MAX_DEVICE_ID_LEN)
                {
                    if (PInvoke.SetupDiGetDeviceInstanceId(deviceInfoSetHandle, &deviceInfoData, pIdBuffer, MAX_DEVICE_ID_LEN, null))
                        pnpDeviceId = new string(pIdBuffer).TrimEnd('\0');
                }
                else
                {
                    char[] heapBuffer = new char[requiredSize];
                    fixed (char* pHeap = heapBuffer)
                        if (PInvoke.SetupDiGetDeviceInstanceId(deviceInfoSetHandle, &deviceInfoData, pHeap, requiredSize, null))
                            pnpDeviceId = new string(pHeap).TrimEnd('\0');
                }
            }

            string vendorId = string.Empty;
            string deviceId = string.Empty;

            if (pnpDeviceId.Contains("VEN_"))
                vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
            else if (pnpDeviceId.Contains("VID_"))
                vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VID_") + 4, 4).ToLowerInvariant();

            if (pnpDeviceId.Contains("DEV_"))
                deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("DEV_") + 4, 4).ToLowerInvariant();
            else if (pnpDeviceId.Contains("PID_"))
                deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("PID_") + 4, 4).ToLowerInvariant();
            string registryPath = $@"SYSTEM\CurrentControlSet\Control\Class\{GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_DRIVER)}";

            string driverVersion = Registry.LocalMachine.OpenSubKey(registryPath).GetValue("DriverVersion")?.ToString();

            uint msiSupported = 2, msiLimit = 0, devicePolicy = 0, devicePriority = 0;
            ulong assignmentSetOverride = 0;

            using (var deviceRegKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters", false))
            {
                if (deviceRegKey != null)
                {
                    using (var msiKeySub = deviceRegKey.OpenSubKey(@"Interrupt Management\MessageSignaledInterruptProperties"))
                        if (msiKeySub != null)
                        {
                            msiSupported = Convert.ToUInt32(msiKeySub.GetValue("MSISupported") ?? 2);
                            msiLimit = Convert.ToUInt32(msiKeySub.GetValue("MessageNumberLimit") ?? 0);
                        }

                    using var affinityKey = deviceRegKey.OpenSubKey(@"Interrupt Management\Affinity Policy");
                    if (affinityKey != null)
                    {
                        devicePolicy = Convert.ToUInt32(affinityKey.GetValue("DevicePolicy") ?? 0);
                        devicePriority = Convert.ToUInt32(affinityKey.GetValue("DevicePriority") ?? 0);
                        if (affinityKey.GetValue("AssignmentSetOverride") is byte[] bytes && bytes.Length > 0)
                        {
                            byte[] full = new byte[8];
                            Array.Copy(bytes, full, Math.Min(bytes.Length, 8));
                            assignmentSetOverride = BitConverter.ToUInt64(full, 0);
                        }
                    }

                }
            }

            uint maxMsiLimit = 0;
            var msiPropKey = PInvoke.DEVPKEY_PciDevice_InterruptMessageMaximum;
            Windows.Win32.Devices.Properties.DEVPROPTYPE propertyType;
            byte[] msiBuffer = new byte[4];
            fixed (byte* pMsiBuf = msiBuffer)
            {
                if (PInvoke.SetupDiGetDeviceProperty(deviceInfoSetHandle, &deviceInfoData, &msiPropKey, &propertyType, pMsiBuf, (uint)msiBuffer.Length, &requiredSize, 0))
                    if (requiredSize >= 4) maxMsiLimit = BitConverter.ToUInt32(msiBuffer, 0);
            }

            NicDriverType? nicDriverType = null;
            NicDeviceType? nicDeviceType = null;
            bool isActive = false;

            XhciDeviceType? xhciType = null;
            ulong? baseAddress = null;

            if (type == DeviceType.NIC)
            {
                using var classKey = Registry.LocalMachine.OpenSubKey(registryPath);
                nicDeviceType = classKey?.GetValue("*PhysicalMediaType")?.ToString() switch
                {
                    "9" => NicDeviceType.WiFi,
                    "14" => NicDeviceType.LAN,
                    _ => NicDeviceType.Virtual
                };
                nicDriverType = GetNicDriverType(classKey) ? NicDriverType.NetAdapterCx : NicDriverType.NDIS;

                isActive = GetActive(pnpDeviceId);
            }
            else if (type == DeviceType.XHCI)
            {
                xhciType = XhciDeviceType.Controller;
                baseAddress = GetDeviceBaseAddress(pnpDeviceId);
            }

            var device = new DeviceInfo
            {
                FriendlyName = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_FRIENDLYNAME),
                DeviceDescription = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_DEVICEDESC),
                PnpDeviceId = pnpDeviceId,
                VendorId = vendorId,
                DeviceId = deviceId,
                Location = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_LOCATION_INFORMATION),
                RegistryPath = registryPath,
                State = GetDeviceState(pnpDeviceId),
                DevObjName = GetDeviceRegistryPropertyString(deviceInfoSetHandle, &deviceInfoData, SPDRP_PHYSICAL_DEVICE_OBJECT_NAME),
                MsiSupported = msiSupported,
                MsiLimit = msiLimit,
                MaxMsiLimit = maxMsiLimit,
                DevicePolicy = devicePolicy,
                DevicePriority = devicePriority,
                AssignmentSetOverride = assignmentSetOverride,
                DriverType = nicDriverType,
                NicType = nicDeviceType,
                IsActive = isActive,
                XhciType = xhciType,
                BaseAddress = baseAddress,
                CurrentVersion = driverVersion
            };

            devices.Add(device);
        }

        PInvoke.SetupDiDestroyDeviceInfoList(deviceInfoSetHandle);
        return devices;
    }

    private unsafe static DeviceState GetDeviceState(string pnpDeviceId)
    {
        fixed (char* pId = pnpDeviceId)
        {
            if (PInvoke.CM_Locate_DevNode(out uint devInst, pId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL) == CONFIGRET.CR_SUCCESS)
            {
                if (PInvoke.CM_Get_DevNode_Status(out var status, out var prob, devInst, 0) == CONFIGRET.CR_SUCCESS)
                {
                    if ((status & CM_DEVNODE_STATUS_FLAGS.DN_STARTED) != 0)
                        return DeviceState.Enabled;

                    if ((status & CM_DEVNODE_STATUS_FLAGS.DN_HAS_PROBLEM) != 0)
                        return DeviceState.Error;

                    return DeviceState.Disabled;
                }
            }
        }
        return DeviceState.Error;
    }

    private unsafe static string GetDeviceRegistryPropertyString(HDEVINFO deviceInfoSet, SP_DEVINFO_DATA* deviceInfoData, SETUP_DI_REGISTRY_PROPERTY property)
    {
        uint requiredSize = 0;
        uint regType;

        PInvoke.SetupDiGetDeviceRegistryProperty(deviceInfoSet, deviceInfoData, property, &regType, null, 0, &requiredSize);

        if (requiredSize == 0) return string.Empty;

        byte[] buffer = new byte[requiredSize];
        fixed (byte* pBuffer = buffer)
        {
            if (PInvoke.SetupDiGetDeviceRegistryProperty(deviceInfoSet, deviceInfoData, property, &regType, pBuffer, requiredSize, null))
            {
                return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
            }
        }
        return string.Empty;
    }

    public static void SetMSIMode(string pnpDeviceId, bool msiSupported, uint msiLimit)
    {
        using var interruptKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters", true).CreateSubKey("Interrupt Management");

        if (msiSupported)
        {
            using var msiKey = interruptKey.CreateSubKey("MessageSignaledInterruptProperties");

            msiKey.SetValue("MSISupported", 1, RegistryValueKind.DWord);

            if (msiLimit == 0)
                msiKey.DeleteValue("MessageNumberLimit", false);
            else
                msiKey.SetValue("MessageNumberLimit", msiLimit, RegistryValueKind.DWord);
        }
        else
        {
            interruptKey?.DeleteSubKeyTree("MessageSignaledInterruptProperties", false);
        }
    }

    public static void SetAffinityPolicy(string pnpDeviceId, uint devicePolicy, uint devicePriority, ulong assignmentSetOverride)
    {
        using var devParamsKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters", true);
        if (devParamsKey == null) return;

        if (devicePolicy == 0 && devicePriority == 0 && assignmentSetOverride == 0)
        {
            devParamsKey.OpenSubKey("Interrupt Management", true)?.DeleteSubKeyTree("Affinity Policy", false);
            return;
        }

        using var affinityKey = devParamsKey.CreateSubKey("Interrupt Management").CreateSubKey("Affinity Policy");
        affinityKey.SetValue("DevicePolicy", devicePolicy, RegistryValueKind.DWord);

        if (devicePolicy == 4)
        {
            byte[] bytes = BitConverter.GetBytes(assignmentSetOverride);
            int length = bytes.Length;
            while (length > 1 && bytes[length - 1] == 0) length--;

            byte[] trimmed = new byte[length];
            Array.Copy(bytes, trimmed, length);
            affinityKey.SetValue("AssignmentSetOverride", trimmed, RegistryValueKind.Binary);
        }
        else
            affinityKey.DeleteValue("AssignmentSetOverride", false);

        if (devicePriority == 0)
            affinityKey.DeleteValue("DevicePriority", false);
        else
            affinityKey.SetValue("DevicePriority", devicePriority, RegistryValueKind.DWord);
    }

    public static ApplyResult ApplySettingsToDevices(List<DeviceInfo> devices, bool msiSupported, uint msiLimit, uint devicePolicy, uint devicePriority, ulong assignmentSetOverride, DeviceType deviceType = DeviceType.GPU)
    {
        var result = new ApplyResult();

        foreach (var device in devices)
        {
            bool changed = false;

            if (device.MsiSupported != (msiSupported ? 1u : 0u) || device.MsiLimit != msiLimit)
            {
                SetMSIMode(device.PnpDeviceId, msiSupported, msiLimit);
                device.MsiSupported = msiSupported ? 1u : 0u;
                device.MsiLimit = msiLimit;
                changed = true;
            }

            if (device.DevicePolicy != devicePolicy || device.DevicePriority != devicePriority || device.AssignmentSetOverride != assignmentSetOverride)
            {
                SetAffinityPolicy(device.PnpDeviceId, devicePolicy, devicePriority, assignmentSetOverride);
                device.DevicePolicy = devicePolicy;
                device.DevicePriority = devicePriority;
                device.AssignmentSetOverride = assignmentSetOverride;
                changed = true;
            }

            if (changed)
            {
                if (deviceType == DeviceType.NIC && assignmentSetOverride != 0)
                    SetRSS(device, assignmentSetOverride);

                result.ChangedDevices.Add(device);
                result.AppliedSettings[device.PnpDeviceId] = device;
            }
        }

        result.Success = result.ChangedDevices.Count > 0;
        result.NeedsRestart = result.Success;
        return result;
    }

    public static void SetRSS(DeviceInfo device, ulong assignmentSetOverride)
    {
        using var classKey = Registry.LocalMachine.OpenSubKey(device.RegistryPath, true);
        if (classKey?.GetValue("*PhysicalMediaType")?.ToString() != "14") return;

        if (device.DevicePolicy == 4 && assignmentSetOverride != 0)
        {
            var threads = Enumerable.Range(0, 64).Where(i => (assignmentSetOverride & (1UL << i)) != 0).ToList();
            if (threads.Count == 0) return;

            classKey.SetValue("*RSS", "1", RegistryValueKind.String);
            classKey.SetValue("*RssBaseProcGroup", "0", RegistryValueKind.String);
            classKey.SetValue("*RssBaseProcNumber", threads.Min().ToString(), RegistryValueKind.String);
            classKey.SetValue("*MaxProcessors", threads.Count.ToString(), RegistryValueKind.String);
        }
        else
        {
            foreach (var key in new[] { "*RssBaseProcGroup", "*RssBaseProcNumber", "*MaxProcessors" })
                classKey.DeleteValue(key, false);
        }
    }

    private static bool GetNicDriverType(RegistryKey classKey)
    {
        using var ndiKey = classKey.OpenSubKey("Ndi");
        string serviceName = ndiKey?.GetValue("Service")?.ToString()?.TrimEnd('.');
        if (string.IsNullOrEmpty(serviceName)) return false;

        using var serviceKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
        if (serviceKey?.GetValue("ImagePath") is not string imagePath) return false;

        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
        string resolved = Environment.ExpandEnvironmentVariables(imagePath.StartsWith(@"\??\") ? imagePath[4..] : imagePath);

        resolved = resolved.StartsWith(@"\SystemRoot", StringComparison.OrdinalIgnoreCase)
            ? resolved.Replace(@"\SystemRoot", systemRoot, StringComparison.OrdinalIgnoreCase)
            : resolved.StartsWith("System32", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(systemRoot, resolved)
                : resolved;

        if (!File.Exists(resolved)) return false;

        return System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(resolved)).Contains("NetAdapter", StringComparison.OrdinalIgnoreCase);
    }

    private unsafe static bool GetActive(string pnpDeviceId)
    {
        uint size = 15000;
        byte[] buffer = new byte[size];
        fixed (byte* pBuf = buffer)
        {
            if (PInvoke.GetAdaptersAddresses(0, GET_ADAPTERS_ADDRESSES_FLAGS.GAA_FLAG_SKIP_ANYCAST, null, (IP_ADAPTER_ADDRESSES_LH*)pBuf, &size) != 0) return false;

            for (var p = (IP_ADAPTER_ADDRESSES_LH*)pBuf; p != null; p = p->Next)
            {
                if (p->IfType == 24 || p->OperStatus != IF_OPER_STATUS.IfOperStatusUp) continue;

                string guid = new((sbyte*)p->AdapterName.Value);
                using var netKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Network\{{4D36E972-E325-11CE-BFC1-08002BE10318}}\{guid}\Connection");
                if (string.Equals(netKey?.GetValue("PnpInstanceID")?.ToString(), pnpDeviceId, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }

    public unsafe static bool RestartDevice(DeviceInfo device)
    {
        using var hDevInfo = PInvoke.SetupDiCreateDeviceInfoList(null, default);
        if (hDevInfo.IsInvalid) return false;

        SP_DEVINFO_DATA devData = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };

        if (!PInvoke.SetupDiOpenDeviceInfo(hDevInfo, device.PnpDeviceId, HWND.Null, 0, ref devData))
            return false;

        var params_ = new SP_PROPCHANGE_PARAMS
        {
            ClassInstallHeader = new()
            {
                cbSize = (uint)sizeof(SP_CLASSINSTALL_HEADER),
                InstallFunction = DI_FUNCTION.DIF_PROPERTYCHANGE
            },
            StateChange = SETUP_DI_STATE_CHANGE.DICS_PROPCHANGE,
            Scope = SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_CONFIGSPECIFIC
        };

        HDEVINFO hDev = (HDEVINFO)hDevInfo.DangerousGetHandle();

        if (PInvoke.SetupDiSetClassInstallParams(hDev, &devData, (SP_CLASSINSTALL_HEADER*)&params_, (uint)sizeof(SP_PROPCHANGE_PARAMS)))
            return PInvoke.SetupDiCallClassInstaller(DI_FUNCTION.DIF_PROPERTYCHANGE, hDev, &devData);

        return false;
    }

    public static async Task<RestartResult> RestartDevicesAsync(List<DeviceInfo> devices)
    {
        var result = new RestartResult();
        int successCount = 0;
        int failedCount = 0;
        var failedDevices = new ConcurrentBag<string>();

        var tasks = devices.Select(async device =>
        {
            bool success = await Task.Run(() => RestartDevice(device));
            if (success)
                Interlocked.Increment(ref successCount);
            else
            {
                Interlocked.Increment(ref failedCount);
                failedDevices.Add(device.DeviceDescription);
            }
        });

        await Task.WhenAll(tasks);

        result.SuccessCount = successCount;
        result.FailedCount = failedCount;
        result.FailedDevices = [.. failedDevices];

        return result;
    }

    public unsafe static bool SetDeviceState(DeviceInfo device, bool enable)
    {
        using var hDevInfo = PInvoke.SetupDiCreateDeviceInfoList(null, default);
        if (hDevInfo.IsInvalid) return false;

        SP_DEVINFO_DATA devData = new() { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };

        if (!PInvoke.SetupDiOpenDeviceInfo(hDevInfo, device.PnpDeviceId, HWND.Null, 0, ref devData))
            return false;

        var params_ = new SP_PROPCHANGE_PARAMS
        {
            ClassInstallHeader = new()
            {
                cbSize = (uint)sizeof(SP_CLASSINSTALL_HEADER),
                InstallFunction = DI_FUNCTION.DIF_PROPERTYCHANGE
            },
            StateChange = enable ? SETUP_DI_STATE_CHANGE.DICS_ENABLE : SETUP_DI_STATE_CHANGE.DICS_DISABLE,
            Scope = SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_GLOBAL
        };

        HDEVINFO hDev = (HDEVINFO)hDevInfo.DangerousGetHandle();

        if (PInvoke.SetupDiSetClassInstallParams(hDev, &devData, (SP_CLASSINSTALL_HEADER*)&params_, (uint)sizeof(SP_PROPCHANGE_PARAMS)))
        {
            return PInvoke.SetupDiCallClassInstaller(DI_FUNCTION.DIF_PROPERTYCHANGE, hDev, &devData);
        }

        return false;
    }

    private unsafe static ulong? GetDeviceBaseAddress(string pnpDeviceId)
    {
        fixed (char* pId = pnpDeviceId)
        {
            if (PInvoke.CM_Locate_DevNode(out uint devInst, pId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL) != CONFIGRET.CR_SUCCESS)
                return null;

            if (PInvoke.CM_Get_First_Log_Conf(out nuint logConf, devInst, CM_LOG_CONF.ALLOC_LOG_CONF) != CONFIGRET.CR_SUCCESS && PInvoke.CM_Get_First_Log_Conf(out logConf, devInst, CM_LOG_CONF.BOOT_LOG_CONF) != CONFIGRET.CR_SUCCESS)
                return null;

            try
            {
                nuint resDes = 0;
                nuint currentHandle = logConf;
                bool isFirst = true;
                uint resType = 0;

                while (true)
                {
                    var result = PInvoke.CM_Get_Next_Res_Des(out nuint nextResDes, currentHandle, 0, out CM_RESTYPE outResType, 0);
                    resType = (uint)outResType;

                    if (!isFirst && resDes != 0)
                        PInvoke.CM_Free_Res_Des_Handle(resDes);

                    if (result != CONFIGRET.CR_SUCCESS)
                        break;

                    resDes = nextResDes;
                    isFirst = false;
                    currentHandle = resDes;

                    if (resType == 1 || resType == 7)
                    {
                        uint dataSize;
                        if (PInvoke.CM_Get_Res_Des_Data_Size(&dataSize, resDes, 0) == CONFIGRET.CR_SUCCESS && dataSize >= 24)
                        {
                            byte[] data = new byte[dataSize];
                            fixed (byte* pData = data)
                            {
                                if (PInvoke.CM_Get_Res_Des_Data(resDes, pData, dataSize, 0) == CONFIGRET.CR_SUCCESS)
                                {
                                    ulong addr = BitConverter.ToUInt64(data, 8);
                                    PInvoke.CM_Free_Res_Des_Handle(resDes);
                                    return addr;
                                }
                            }
                        }
                    }
                }

                if (resDes != 0)
                    PInvoke.CM_Free_Res_Des_Handle(resDes);
            }
            finally
            {
                PInvoke.CM_Free_Log_Conf_Handle(logConf);
            }
        }
        return null;
    }

    public static bool GetIMODState(DeviceInfo device, ReadWriteHelper sharedHw = null)
    {
        using var localHw = sharedHw == null ? new ReadWriteHelper() : null;
        var hw = sharedHw ?? localHw;

        if (device.BaseAddress == null) return false;
        
        hw.ReadMemory32(device.BaseAddress.Value + 0x18, out uint rtsoff);
        ulong runtime = device.BaseAddress.Value + (rtsoff & ~0x1Fu);

        hw.ReadMemory32(device.BaseAddress.Value + 0x04, out uint hcs1);
        int max = (int)((hcs1 >> 8) & 0x7FF);

        for (int i = 0; i < max; i++)
        {
            hw.ReadMemory32(runtime + 0x24 + (0x20 * (ulong)i), out uint imod);
            if (imod != 0) return true;
        }

        return false;
    }

    public static void ToggleImod(DeviceInfo device, bool enable)
    {
        using var hw = new ReadWriteHelper();
        if (device.BaseAddress == null) return;

        hw.ReadMemory32(device.BaseAddress.Value + 0x18, out uint rtsoff);
        ulong runtime = device.BaseAddress.Value + (rtsoff & ~0x1Fu);
        hw.ReadMemory32(device.BaseAddress.Value + 0x04, out uint hcs1);
        int max = (int)((hcs1 >> 8) & 0x7FF);

        if (enable)
        {
            var json = ApplicationData.Current.LocalSettings.Values["XHCIs"]?.ToString();
            if (string.IsNullOrEmpty(json)) return;

            var array = JsonNode.Parse(json)?.AsArray();
            var intervals = array?.FirstOrDefault(x => x?["PnpDeviceId"]?.ToString() == device.PnpDeviceId)?["Intervals"]?.AsObject();
            if (intervals != null)
            {
                foreach (var kvp in intervals)
                {
                    if (ulong.TryParse(kvp.Key, out ulong addr) && uint.TryParse(kvp.Value?.ToString(), out uint val))
                        hw.WriteMemory32(addr, val);
                }
            }
        }
        else
        {
            SaveImod(device, hw);
            for (int i = 0; i < max; i++)
                hw.WriteMemory32(runtime + 0x24 + (0x20 * (ulong)i), 0);
        }
    }

    public static void SaveImod(DeviceInfo device, ReadWriteHelper sharedHw = null)
    {
        using var localHw = sharedHw == null ? new ReadWriteHelper() : null;
        var hw = sharedHw ?? localHw;

        if (device.BaseAddress == null || !GetIMODState(device, hw)) return;

        hw.ReadMemory32(device.BaseAddress.Value + 0x18, out uint rtsoff);
        ulong runtime = device.BaseAddress.Value + (rtsoff & ~0x1Fu);
        hw.ReadMemory32(device.BaseAddress.Value + 0x04, out uint hcs1);
        int max = (int)((hcs1 >> 8) & 0x7FF);

        var intervals = new JsonObject();
        for (int i = 0; i < max; i++)
        {
            ulong addr = runtime + 0x24 + (0x20 * (ulong)i);
            hw.ReadMemory32(addr, out uint val);
            intervals[addr.ToString()] = JsonValue.Create(val);
        }

        var settings = ApplicationData.Current.LocalSettings;
        var json = settings.Values["XHCIs"]?.ToString();
        var array = (!string.IsNullOrEmpty(json) ? JsonNode.Parse(json)?.AsArray() : null) ?? [];
        
        for (int i = array.Count - 1; i >= 0; i--)
            if (array[i]?["PnpDeviceId"]?.ToString() == device.PnpDeviceId) array.RemoveAt(i);

        array.Add((JsonNode)new JsonObject { ["PnpDeviceId"] = JsonValue.Create(device.PnpDeviceId), ["Intervals"] = intervals });
        settings.Values["XHCIs"] = array.ToJsonString();
    }
}
