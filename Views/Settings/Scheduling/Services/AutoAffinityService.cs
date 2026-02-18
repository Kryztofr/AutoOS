using AutoOS.Views.Settings.Scheduling.Models;
using AutoOS.Views.Settings.Scheduling.ViewModels;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace AutoOS.Views.Settings.Scheduling.Services;

public static class AutoAffinityService
{
    public static async Task ApplyAutoAffinities(SchedulingPageViewModel viewModel = null)
    {
        var cpuSetsInfo = CpuDetectionService.GetCpuSets();
        var (pCores, eCores) = CpuDetectionService.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);

        if (pCores.Count <= 2)
            return;

        bool hasHyperThreading = cpuSetsInfo.HyperThreading;
        int threadsPerCore = hasHyperThreading ? 2 : 1;

        var allChangedDevices = new List<(DeviceInfo device, DeviceType deviceType)>();
        var allDevices = new List<DeviceInfo>();

        if (pCores.Count >= 4)
        {
            var gpuDevices = DeviceDetectionService.FindDevicesByType(DeviceType.GPU);
            if (gpuDevices.Count > 0)
            {
                allDevices.AddRange(gpuDevices);
                var gpuMask = BuildAffinityMask(pCores, pCores.Count - 4, 2, threadsPerCore);
                var gpuResult = ApplyAffinityOnly(gpuDevices, gpuMask, DeviceType.GPU);
                allChangedDevices.AddRange(gpuResult.ChangedDevices.Select(d => (d, DeviceType.GPU)));
            }
            var xhciDevices = DeviceDetectionService.FindDevicesByType(DeviceType.XHCI);
            if (xhciDevices.Count > 0)
            {
                allDevices.AddRange(xhciDevices);
                var xhciMask = BuildAffinityMask(pCores, pCores.Count - 2, 1, threadsPerCore);
                var xhciResult = ApplyAffinityOnly(xhciDevices, xhciMask, DeviceType.XHCI);
                allChangedDevices.AddRange(xhciResult.ChangedDevices.Select(d => (d, DeviceType.XHCI)));
            }
            var nicDevices = DeviceDetectionService.FindDevicesByType(DeviceType.NIC);
            if (nicDevices.Count > 0)
            {
                allDevices.AddRange(nicDevices);
                var nicMask = BuildAffinityMask(pCores, pCores.Count - 1, 1, threadsPerCore);
                var nicResult = ApplyAffinityOnly(nicDevices, nicMask, DeviceType.NIC);
                allChangedDevices.AddRange(nicResult.ChangedDevices.Select(d => (d, DeviceType.NIC)));
            }
        }

        if (allChangedDevices.Count > 0)
        {
            if (viewModel != null)
            {
                foreach (var (device, deviceType) in allChangedDevices)
                {
                    var devObjName = device.DevObjName ?? string.Empty;
                    viewModel.UpdateDevice(deviceType, devObjName);
                }
            }
            
            await RestartDevicesSilentlyAsync(allChangedDevices.Select(d => d.device).ToList());
        }

        CleanupDevices(allDevices);
    }

    private static ulong BuildAffinityMask(List<CpuCore> pCores, int startCoreIndex, int coreCount, int threadsPerCore)
    {
        ulong mask = 0;

        for (int i = 0; i < coreCount && (startCoreIndex + i) < pCores.Count; i++)
        {
            int coreIndex = startCoreIndex + i;
            var core = pCores[coreIndex];

            int threadsToUse = threadsPerCore;
            if (threadsToUse > core.Threads.Count)
                threadsToUse = core.Threads.Count;

            for (int j = 0; j < threadsToUse; j++)
            {
                mask |= core.Threads[j].BitMask;
            }
        }

        return mask;
    }

    private static void CleanupDevices(List<DeviceInfo> devices)
    {
        HDEVINFO handle = default;
        foreach (var device in devices)
        {
            if (handle.Value == 0)
                handle = device.DeviceInfoSet;
            if (device.RegistryKey != null)
                device.RegistryKey.Close();
        }

        if (handle.Value != 0 && handle.Value != (nint)(-1))
            PInvoke.SetupDiDestroyDeviceInfoList(handle);
    }

    private static DeviceSettingsService.ApplyResult ApplyAffinityOnly(List<DeviceInfo> devices, ulong assignmentSetOverride, DeviceType deviceType)
    {
        var result = new DeviceSettingsService.ApplyResult();
        var changedDevices = new List<DeviceInfo>();

        foreach (var device in devices)
        {
            if (device.RegistryKey == null)
                continue;

            var currentSettings = RegistryService.ReadDeviceSettings(device.RegistryKey, device.MaxMSILimit);

            bool affinityChanged = currentSettings.DevicePolicy != 4 ||
                                  currentSettings.AssignmentSetOverride != assignmentSetOverride;

            if (affinityChanged)
            {
                RegistryService.SetAffinityPolicy(device.RegistryKey, 4, currentSettings.DevicePriority, assignmentSetOverride);
                if (!changedDevices.Contains(device))
                    changedDevices.Add(device);
            }

            if (deviceType == DeviceType.NIC && assignmentSetOverride != 0)
            {
                DeviceSettingsService.SetRssProcessorNumbers(device, assignmentSetOverride);
            }
        }

        result.ChangedDevices = changedDevices;
        result.Success = changedDevices.Count > 0;
        result.NeedsRestart = changedDevices.Count > 0;

        return result;
    }

    private static async Task RestartDevicesSilentlyAsync(List<DeviceInfo> devices)
    {
        await Task.Run(() =>
        {
            foreach (var device in devices)
            {
                if (device.DeviceInfoSet.Value != 0)
                {
                    DeviceSettingsService.RestartDevice(device);
                }
            }
        });
    }
}