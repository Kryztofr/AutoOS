using AutoOS.Views.Settings.Scheduling.Models;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace AutoOS.Views.Settings.Scheduling.Services;

public class DeviceSettingsService
{
    public class ApplyResult
    {
        public bool Success { get; set; }
        public bool NeedsRestart { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DeviceInfo> ChangedDevices { get; set; } = [];
        public Dictionary<string, DeviceSettings> AppliedSettings { get; set; } = [];
    }

    public static ApplyResult ApplySettingsToDevices(
        List<DeviceInfo> devices,
        bool msiSupported,
        uint messageNumberLimit,
        uint devicePolicy,
        uint devicePriority,
        ulong assignmentSetOverride,
        DeviceType deviceType = DeviceType.GPU)
    {
        var result = new ApplyResult();
        var changedDevices = new List<DeviceInfo>();

        foreach (var device in devices)
        {
            if (device.RegistryKey == null)
                continue;

            var currentSettings = RegistryService.ReadDeviceSettings(device.RegistryKey, device.MaxMSILimit);

            uint expectedMsiSupported = msiSupported ? 1u : 0u;
            bool msiChanged = currentSettings.MsiSupported != expectedMsiSupported ||
                              currentSettings.MessageNumberLimit != messageNumberLimit;

            bool affinityChanged = currentSettings.DevicePolicy != devicePolicy ||
                                   currentSettings.DevicePriority != devicePriority ||
                                   currentSettings.AssignmentSetOverride != assignmentSetOverride;

            var appliedSettings = new DeviceSettings
            {
                MsiSupported = expectedMsiSupported,
                MessageNumberLimit = messageNumberLimit,
                DevicePolicy = devicePolicy,
                DevicePriority = devicePriority,
                AssignmentSetOverride = assignmentSetOverride,
                MaxMSILimit = device.MaxMSILimit
            };
            result.AppliedSettings[device.DevObjName ?? string.Empty] = appliedSettings;

            if (msiChanged)
            {
                RegistryService.SetMSIMode(device.RegistryKey, msiSupported, messageNumberLimit);
                if (!changedDevices.Contains(device))
                    changedDevices.Add(device);
            }

            if (affinityChanged)
            {
                RegistryService.SetAffinityPolicy(device.RegistryKey, devicePolicy, devicePriority, assignmentSetOverride);
                if (!changedDevices.Contains(device))
                    changedDevices.Add(device);
            }

            if (deviceType == DeviceType.NIC && devicePolicy == 4 && assignmentSetOverride != 0)
            {
                SetRssProcessorNumbers(device, assignmentSetOverride);
            }
        }

        result.ChangedDevices = changedDevices;
        result.Success = changedDevices.Count > 0;
        result.NeedsRestart = changedDevices.Count > 0;

        return result;
    }

    public static void SetRssProcessorNumbers(DeviceInfo device, ulong assignmentSetOverride)
    {
        using var enumKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{device.PnpDeviceId}", writable: true);
        string driver = enumKey.GetValue("Driver")?.ToString();

        foreach (var subKeyName in enumKey.GetSubKeyNames())
        {
            using var subKey = enumKey.OpenSubKey(subKeyName, writable: false);
            driver = subKey?.GetValue("Driver")?.ToString();
            if (!string.IsNullOrEmpty(driver) && driver.Contains('\\'))
                break;
        }

        if (string.IsNullOrEmpty(driver) || !driver.Contains('\\'))
            return;

        using var classKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Class\{driver}", writable: true);

        if (classKey.GetValue("*PhysicalMediaType")?.ToString() != "14")
            return;

        var selectedThreads = new List<int>();
        for (int i = 0; i < 64 && assignmentSetOverride != 0; i++)
        {
            if ((assignmentSetOverride & (1UL << i)) != 0)
                selectedThreads.Add(i);
        }

        if (selectedThreads.Count == 0)
            return;

        var (minThread, maxThread) = (selectedThreads.Min(), selectedThreads.Max());

        classKey.SetValue("*RSS", "0", RegistryValueKind.String);
        classKey.SetValue("*RssBaseProcGroup", "0", RegistryValueKind.String);
        classKey.SetValue("*RssBaseProcNumber", minThread.ToString(), RegistryValueKind.String);
        classKey.SetValue("*RssMaxProcGroup", "0", RegistryValueKind.String);
        classKey.SetValue("*RssMaxProcNumber", maxThread.ToString(), RegistryValueKind.String);
    }

    public unsafe static bool RestartDevice(DeviceInfo device)
    {
        if (device.DeviceInfoSet.Value == 0)
            return false;

        var hDevInfo = device.DeviceInfoSet;

        var propChangeParams = new Windows.Win32.Devices.DeviceAndDriverInstallation.SP_PROPCHANGE_PARAMS
        {
            ClassInstallHeader = new Windows.Win32.Devices.DeviceAndDriverInstallation.SP_CLASSINSTALL_HEADER
            {
                cbSize = (uint)Marshal.SizeOf(typeof(Windows.Win32.Devices.DeviceAndDriverInstallation.SP_CLASSINSTALL_HEADER)),
                InstallFunction = Windows.Win32.Devices.DeviceAndDriverInstallation.DI_FUNCTION.DIF_PROPERTYCHANGE
            },
            StateChange = (Windows.Win32.Devices.DeviceAndDriverInstallation.SETUP_DI_STATE_CHANGE)DICS_STATE.DICS_PROPCHANGE,
            Scope = (Windows.Win32.Devices.DeviceAndDriverInstallation.SETUP_DI_PROPERTY_CHANGE_SCOPE)DICS_FLAG.DICS_FLAG_GLOBAL,
            HwProfile = 0
        };

        var deviceInfoData = device.DeviceInfoData;

        if (!PInvoke.SetupDiSetClassInstallParams(
            hDevInfo,
            &deviceInfoData,
            (Windows.Win32.Devices.DeviceAndDriverInstallation.SP_CLASSINSTALL_HEADER*)&propChangeParams,
            (uint)sizeof(Windows.Win32.Devices.DeviceAndDriverInstallation.SP_PROPCHANGE_PARAMS)))
        {
            return false;
        }

        if (!PInvoke.SetupDiCallClassInstaller(
            Windows.Win32.Devices.DeviceAndDriverInstallation.DI_FUNCTION.DIF_PROPERTYCHANGE,
            hDevInfo,
            &deviceInfoData))
        {
            return false;
        }

        if (!PInvoke.SetupDiSetClassInstallParams(
            hDevInfo,
            &deviceInfoData,
            (Windows.Win32.Devices.DeviceAndDriverInstallation.SP_CLASSINSTALL_HEADER*)&propChangeParams,
            (uint)sizeof(Windows.Win32.Devices.DeviceAndDriverInstallation.SP_PROPCHANGE_PARAMS)))
        {
            return false;
        }

        if (!PInvoke.SetupDiCallClassInstaller(
            Windows.Win32.Devices.DeviceAndDriverInstallation.DI_FUNCTION.DIF_PROPERTYCHANGE,
            hDevInfo,
            &deviceInfoData))
        {
            return false;
        }

        SP_DEVINSTALL_PARAMS_W installParams = default;
        installParams.cbSize = (uint)sizeof(SP_DEVINSTALL_PARAMS_W);

        if (PInvoke.SetupDiGetDeviceInstallParams(
            hDevInfo,
            &deviceInfoData,
            &installParams))
        {
            if ((installParams.Flags & SETUP_DI_DEVICE_INSTALL_FLAGS.DI_NEEDREBOOT) != 0)
            {
                return false;
            }
        }

        return true;
    }

    public static async Task<RestartResult> RestartDevicesAsync(List<DeviceInfo> devices)
    {
        var result = new RestartResult();
        int successCount = 0;
        int failedCount = 0;
        var failedDevices = new System.Collections.Concurrent.ConcurrentBag<string>();

        var tasks = devices.Select(async device =>
        {
            bool success = await Task.Run(() => RestartDevice(device));
            if (success)
                Interlocked.Increment(ref successCount);
            else
            {
                Interlocked.Increment(ref failedCount);
                failedDevices.Add(device.DeviceDesc);
            }
        });

        await Task.WhenAll(tasks);

        result.SuccessCount = successCount;
        result.FailedCount = failedCount;
        result.FailedDevices = failedDevices.ToList();

        return result;
    }

    public class RestartResult
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> FailedDevices { get; set; } = [];
    }
}
