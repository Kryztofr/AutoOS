using AutoOS.Helpers.Device;
using AutoOS.Helpers.CPU;

namespace AutoOS.Helpers.Scheduling;

internal static class SchedulingHelper
{
    public static async Task OptimizeAffinities(SchedulingPage page = null)
    {
        var cpuSetsInfo = CpuHelper.GetCpuSets();
        var (pCores, eCores) = CpuHelper.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);

        if (pCores.Count <= 2)
            return;

        bool hasHyperThreading = cpuSetsInfo.HyperThreading;
        int threadsPerCore = hasHyperThreading ? 2 : 1;

        var allChangedDevices = new List<(DeviceInfo device, DeviceType deviceType)>();

        if (pCores.Count >= 4)
        {
            var gpuDevices = DeviceHelper.GetDevices(DeviceType.GPU);
            if (gpuDevices.Count > 0)
            {
                var gpuMask = BuildAffinityMask(pCores, pCores.Count - 4, 2, threadsPerCore);
                var gpuResult = ApplyAffinityOnly(gpuDevices, gpuMask, DeviceType.GPU);
                allChangedDevices.AddRange(gpuResult.ChangedDevices.Select(d => (d, DeviceType.GPU)));
            }
            var xhciDevices = DeviceHelper.GetDevices(DeviceType.XHCI);
            if (xhciDevices.Count > 0)
            {
                var xhciMask = BuildAffinityMask(pCores, pCores.Count - 2, 1, threadsPerCore);
                var xhciResult = ApplyAffinityOnly(xhciDevices, xhciMask, DeviceType.XHCI);
                allChangedDevices.AddRange(xhciResult.ChangedDevices.Select(d => (d, DeviceType.XHCI)));
            }
            var nicDevices = DeviceHelper.GetDevices(DeviceType.NIC);
            if (nicDevices.Count > 0)
            {
                var nicMask = BuildAffinityMask(pCores, pCores.Count - 1, 1, threadsPerCore);
                var nicResult = ApplyAffinityOnly(nicDevices, nicMask, DeviceType.NIC);
                allChangedDevices.AddRange(nicResult.ChangedDevices.Select(d => (d, DeviceType.NIC)));
            }
        }

        if (allChangedDevices.Count > 0)
        {
            if (page != null)
            {
                foreach (var (device, deviceType) in allChangedDevices)
                {
                    page.UpdateDevice(deviceType, device.PnpDeviceId, device);
                }
            }

            foreach (DeviceInfo device in allChangedDevices.Select(d => d.device))
            {
                await Task.Run(() => DeviceHelper.RestartDevice(device));
            }
        }
    }

    private static ulong BuildAffinityMask(List<CpuCore> pCores, int startCoreIndex, int coreCount, int threadsPerCore)
    {
        ulong mask = 0;

        for (int i = 0; i < coreCount && (startCoreIndex + i) < pCores.Count; i++)
        {
            int coreIndex = startCoreIndex + i;
            var core = pCores[coreIndex];

            int threads = threadsPerCore;
            if (threads > core.Threads.Count)
                threads = core.Threads.Count;

            for (int j = 0; j < threads; j++)
            {
                mask |= core.Threads[j].BitMask;
            }
        }

        return mask;
    }

    private static DeviceHelper.ApplyResult ApplyAffinityOnly(List<DeviceInfo> devices, ulong assignmentSetOverride, DeviceType deviceType)
    {
        var result = new DeviceHelper.ApplyResult();
        var changedDevices = new List<DeviceInfo>();

        foreach (var device in devices)
        {
            bool affinityChanged = device.DevicePolicy != 4 || device.AssignmentSetOverride != assignmentSetOverride;

            if (affinityChanged)
            {
                DeviceHelper.SetAffinityPolicy(device.PnpDeviceId, 4, device.DevicePriority, assignmentSetOverride);
                
                device.DevicePolicy = 4;
                device.AssignmentSetOverride = assignmentSetOverride;

                if (!changedDevices.Contains(device))
                    changedDevices.Add(device);
            }

            if (deviceType == DeviceType.NIC && assignmentSetOverride != 0)
                DeviceHelper.SetRSS(device, assignmentSetOverride);
        }

        result.ChangedDevices = changedDevices;
        result.Success = changedDevices.Count > 0;
        result.NeedsRestart = changedDevices.Count > 0;

        return result;
    }
}