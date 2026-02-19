using AutoOS.Views.Settings.Scheduling.Models;
using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace AutoOS.Views.Settings.Scheduling.Services;

public enum DeviceType
{
    GPU,
    XHCI,
    NIC
}

public class DeviceDetectionService
{
    public unsafe static List<DeviceInfo> FindDevicesByType(DeviceType deviceType)
    {
        var devices = new List<DeviceInfo>();

        var pnpDeviceIds = GetPnpDeviceIdsFromWmi(deviceType);

        using var deviceInfoSetHandle = PInvoke.SetupDiGetClassDevs(
            null,
            null,
            HWND.Null,
            SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_ALLCLASSES | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT
        );

        uint index = 0;
        while (true)
        {
            var deviceInfoData = new SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>()
            };

            if (!PInvoke.SetupDiEnumDeviceInfo(deviceInfoSetHandle, index, ref deviceInfoData))
            {
                if ((uint)Marshal.GetLastPInvokeError() == 259)
                    break;
            }

            index++;

            var device = GetDeviceInfo(deviceInfoSetHandle, deviceInfoData);
            if (device == null)
                continue;

            if (pnpDeviceIds.Count == 0)
                continue;

            bool matches = false;

            if (!string.IsNullOrEmpty(device.PnpDeviceId))
            {
                matches = pnpDeviceIds.Any(id =>
                    device.PnpDeviceId.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                    device.PnpDeviceId.StartsWith(id, StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith(device.PnpDeviceId, StringComparison.OrdinalIgnoreCase));
            }

            if (!matches && !string.IsNullOrEmpty(device.DevObjName))
            {
                var lastPart = device.DevObjName.Split('\\').LastOrDefault();
                if (lastPart != null)
                {
                    matches = pnpDeviceIds.Any(id =>
                        id.Equals(lastPart, StringComparison.OrdinalIgnoreCase) ||
                        id.EndsWith("\\" + lastPart, StringComparison.OrdinalIgnoreCase) ||
                        lastPart.Equals(id.Split('\\').LastOrDefault(), StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!matches) continue;

            if (deviceType == DeviceType.GPU && (device.DeviceDesc?.Contains("Microsoft Basic Display Adapter", StringComparison.OrdinalIgnoreCase) ?? false))
                continue;

            if (deviceType == DeviceType.NIC)
            {
                if (device.DeviceDesc?.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ?? false)
                    continue;
                
                if (device.DeviceDesc?.Contains("HID", StringComparison.OrdinalIgnoreCase) ?? false)
                    continue;
            }

            device.DeviceInfoSet = new HDEVINFO(deviceInfoSetHandle.DangerousGetHandle());
            device.DeviceInfoData = deviceInfoData;
            devices.Add(device);
        }

        return devices;
    }

    private static List<string> GetPnpDeviceIdsFromWmi(DeviceType deviceType)
    {
        var pnpDeviceIds = new List<string>();
        string wmiQuery = deviceType switch
        {
            DeviceType.GPU => "SELECT PNPDeviceID FROM Win32_VideoController",
            DeviceType.XHCI => "SELECT PNPDeviceID FROM Win32_USBController",
            DeviceType.NIC => "SELECT PNPDeviceID FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE",
            _ => throw new ArgumentException("Unknown device type")
        };

        using var searcher = new ManagementObjectSearcher(wmiQuery);
        foreach (ManagementObject obj in searcher.Get())
        {
            var pnpId = obj["PNPDeviceID"]?.ToString();
            if (!string.IsNullOrEmpty(pnpId))
                pnpDeviceIds.Add(pnpId);
        }

        return pnpDeviceIds;
    }

    private unsafe static DeviceInfo GetDeviceInfo(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData)
    {
        var deviceInfoSet = new HDEVINFO(deviceInfoSetHandle.DangerousGetHandle());
        var device = new DeviceInfo
        {
            DeviceInfoData = deviceInfoData,
            DevObjName = GetDeviceRegistryPropertyString(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_PHYSICAL_DEVICE_OBJECT_NAME),
            DeviceDesc = GetDeviceRegistryPropertyString(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC)
        };

        if (string.IsNullOrEmpty(device.DeviceDesc))
            return null;

        device.FriendlyName = GetDeviceRegistryPropertyString(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_FRIENDLYNAME);
        device.LocationInformation = GetDeviceRegistryPropertyString(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_LOCATION_INFORMATION);

        var hardwareIds = GetDeviceRegistryPropertyMultiString(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_HARDWAREID);
        if (hardwareIds?.Length > 0)
        {
            device.PnpDeviceId = hardwareIds[0];
            if (string.IsNullOrEmpty(device.PnpDeviceId))
            {
                var compatibleIds = GetDeviceRegistryPropertyMultiString(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_COMPATIBLEIDS);
                device.PnpDeviceId = compatibleIds?.Length > 0 ? compatibleIds[0] : string.Empty;
            }
        }

        var regKeyHandle = PInvoke.SetupDiOpenDevRegKey(
            deviceInfoSetHandle,
            deviceInfoData,
            (uint)SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_GLOBAL,
            0,
            PInvoke.DIREG_DEV,
            0x00020019
        );

        if (!regKeyHandle.IsInvalid)
        {
            device.RegistryKey = RegistryKey.FromHandle(regKeyHandle);
        }

        Windows.Win32.Devices.Properties.DEVPROPTYPE propertyType;
        uint requiredSize = 0;
        byte[] buffer = new byte[16];
        var msiKey = PInvoke.DEVPKEY_PciDevice_InterruptMessageMaximum;

        fixed (byte* pBuffer = buffer)
        {
            if (PInvoke.SetupDiGetDeviceProperty(
                deviceInfoSet,
                &deviceInfoData,
                &msiKey,
                &propertyType,
                pBuffer,
                (uint)buffer.Length,
                &requiredSize,
                0))
            {
                if (requiredSize >= 4)
                {
                    device.MaxMSILimit = BitConverter.ToUInt32(buffer, 0);
                }
            }
            else if (Marshal.GetLastPInvokeError() == 122)
            {
                buffer = new byte[requiredSize];
                fixed (byte* pBufferRetry = buffer)
                {
                    if (PInvoke.SetupDiGetDeviceProperty(
                        deviceInfoSet,
                        &deviceInfoData,
                        &msiKey,
                        &propertyType,
                        pBufferRetry,
                        (uint)buffer.Length,
                        &requiredSize,
                        0))
                    {
                        if (requiredSize >= 4)
                        {
                            device.MaxMSILimit = BitConverter.ToUInt32(buffer, 0);
                        }
                    }
                }
            }
        }

        return device;
    }

    private unsafe static string GetDeviceRegistryPropertyString(HDEVINFO deviceInfoSet, SP_DEVINFO_DATA* deviceInfoData, SETUP_DI_REGISTRY_PROPERTY property)
    {
        uint requiredSize;
        uint regType;

        PInvoke.SetupDiGetDeviceRegistryProperty(
            deviceInfoSet,
            deviceInfoData,
            property,
            &regType,
            null,
            0,
            &requiredSize
        );

        if (requiredSize == 0)
            return string.Empty;

        byte* buffer = stackalloc byte[(int)requiredSize];
        if (PInvoke.SetupDiGetDeviceRegistryProperty(
                deviceInfoSet,
                deviceInfoData,
                property,
                &regType,
                buffer,
                requiredSize,
                null))
        {
            return new string((char*)buffer);
        }

        return string.Empty;
    }

    private unsafe static string[] GetDeviceRegistryPropertyMultiString(HDEVINFO deviceInfoSet, SP_DEVINFO_DATA* deviceInfoData, SETUP_DI_REGISTRY_PROPERTY property)
    {
        uint requiredSize;
        uint regType;

        PInvoke.SetupDiGetDeviceRegistryProperty(
            deviceInfoSet,
            deviceInfoData,
            property,
            &regType,
            null,
            0,
            &requiredSize
        );

        if (requiredSize == 0)
            return null;

        byte* buffer = stackalloc byte[(int)requiredSize];
        if (PInvoke.SetupDiGetDeviceRegistryProperty(
                deviceInfoSet,
                deviceInfoData,
                property,
                &regType,
                buffer,
                requiredSize,
                null))
        {
            var result = new List<string>();
            char* ptr = (char*)buffer;

            while (*ptr != '\0')
            {
                string str = new string(ptr);
                result.Add(str);
                ptr += str.Length + 1;
            }

            return [.. result];
        }

        return null;
    }
}
