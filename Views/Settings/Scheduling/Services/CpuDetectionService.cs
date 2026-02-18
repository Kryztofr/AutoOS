using AutoOS.Views.Settings.Scheduling.Models;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoOS.Views.Settings.Scheduling.Services;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SYSTEM_CPU_SET_INFORMATION
{
    public uint Size;
    public CPU_SET_INFORMATION_TYPE Type;
    public SYSTEM_CPU_SET_INFORMATION_ANONYMOUS Anonymous;
}

[StructLayout(LayoutKind.Explicit)]
public struct SYSTEM_CPU_SET_INFORMATION_ANONYMOUS
{
    [FieldOffset(0)]
    public SYSTEM_CPU_SET_INFORMATION_CPU_SET CpuSet;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SYSTEM_CPU_SET_INFORMATION_CPU_SET
{
    public uint Id;
    public ushort Group;
    public byte LogicalProcessorIndex;
    public byte CoreIndex;
    public byte LastLevelCacheIndex;
    public byte NumaNodeIndex;
    public byte EfficiencyClass;
    public byte AllFlags;
    public byte SchedulingClass;
    public byte Reserved;
    public ulong AllocationTag;
}

public enum CPU_SET_INFORMATION_TYPE
{
    CpuSetInformation = 0
}

public class CpuDetectionService
{
    public class CpuSetsInfo
    {
        public bool HyperThreading { get; set; }
        public int CoreCount { get; set; }
        public int MaxThreadsPerCore { get; set; }
        public bool NumaNode { get; set; }
        public bool LastLevelCache { get; set; }
        public bool EfficiencyClass { get; set; }
        public List<CpuSet> CpuSets { get; set; } = [];
    }

    public static bool IsIntel()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        var vendor = key?.GetValue("VendorIdentifier")?.ToString() ?? "";
        return vendor.Contains("GenuineIntel", StringComparison.OrdinalIgnoreCase);
    }

    public unsafe static CpuSetsInfo GetCpuSets()
    {
        var info = new CpuSetsInfo();
        List<CpuSet> cpuSets;

        var fakeCpuSets = CpuSetInformationFake.FakeCpuSets;
        if (fakeCpuSets != null)
        {
            cpuSets = fakeCpuSets;
            info.CoreCount = cpuSets.Count;
            info.CpuSets = cpuSets;
            ProcessCpuSets(cpuSets, info);
            return info;
        }

        cpuSets = [];

        int bufferSize = 0x20 * 64;
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            uint returnedLength = 0;
            BOOL success = PInvoke.GetSystemCpuSetInformation(
                (Windows.Win32.System.SystemInformation.SYSTEM_CPU_SET_INFORMATION*)buffer,
                (uint)bufferSize,
                &returnedLength,
                HANDLE.Null,
                0
            );

            if (success)
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == 122)
                {
                    Marshal.FreeHGlobal(buffer);
                    bufferSize = (int)returnedLength;
                    buffer = Marshal.AllocHGlobal(bufferSize);

                    success = PInvoke.GetSystemCpuSetInformation(
                        (Windows.Win32.System.SystemInformation.SYSTEM_CPU_SET_INFORMATION*)buffer,
                        (uint)bufferSize,
                        &returnedLength,
                        HANDLE.Null,
                        0
                    );
                }
            }

            int offset = 0;

            while (offset < returnedLength)
            {
                var cpuSetInfo = Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(
                    IntPtr.Add(buffer, offset)
                );

                if (cpuSetInfo.Size == 0)
                    break;

                var cpuSet = cpuSetInfo.Anonymous.CpuSet;

                cpuSets.Add(new CpuSet
                {
                    Id = cpuSet.Id,
                    CoreIndex = cpuSet.CoreIndex,
                    LogicalProcessorIndex = cpuSet.LogicalProcessorIndex,
                    EfficiencyClass = cpuSet.EfficiencyClass,
                    LastLevelCacheIndex = cpuSet.LastLevelCacheIndex,
                    NumaNodeIndex = cpuSet.NumaNodeIndex
                });

                offset += (int)cpuSetInfo.Size;
            }

            info.CoreCount = cpuSets.Count;
            info.CpuSets = cpuSets;

            ProcessCpuSets(cpuSets, info);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return info;
    }

    private static void ProcessCpuSets(List<CpuSet> cpuSets, CpuSetsInfo info)
    {
        byte lastEfficiencyClass = 0;
        byte lastLevelCache = 0;
        byte lastNumaNodeIndex = 0;
        int logicalCores = 0;
        var classGroup = new Dictionary<byte, int>();

        for (int i = 0; i < cpuSets.Count; i++)
        {
            var cpuSet = cpuSets[i];

            if (i == 0)
            {
                lastEfficiencyClass = cpuSet.EfficiencyClass;
            }

            if (cpuSet.CoreIndex != cpuSet.LogicalProcessorIndex)
            {
                info.HyperThreading = true;
                int threadsDiff = cpuSet.LogicalProcessorIndex - cpuSet.CoreIndex;
                if (info.MaxThreadsPerCore < threadsDiff)
                    info.MaxThreadsPerCore = threadsDiff;
            }
            else
            {
                logicalCores++;

                if (!classGroup.ContainsKey(cpuSet.EfficiencyClass))
                {
                    classGroup[cpuSet.EfficiencyClass] = 0;
                }
                classGroup[cpuSet.EfficiencyClass]++;
            }

            if (!info.EfficiencyClass && lastEfficiencyClass != cpuSet.EfficiencyClass)
                info.EfficiencyClass = true;

            if (!info.LastLevelCache && lastLevelCache != cpuSet.LastLevelCacheIndex)
                info.LastLevelCache = true;

            if (!info.NumaNode && lastNumaNodeIndex != cpuSet.NumaNodeIndex)
                info.NumaNode = true;

            lastEfficiencyClass = cpuSet.EfficiencyClass;
            lastLevelCache = cpuSet.LastLevelCacheIndex;
            lastNumaNodeIndex = cpuSet.NumaNodeIndex;
        }
    }

    public static (List<CpuCore> PCores, List<CpuCore> ECores) GroupCpuSetsByEfficiencyClass(CpuSetsInfo cpuSetsInfo)
    {
        var pCores = new List<CpuCore>();
        var eCores = new List<CpuCore>();

        if (!cpuSetsInfo.EfficiencyClass)
        {
            var allCores = GroupCpuSetsByCore(cpuSetsInfo.CpuSets);
            pCores.AddRange(allCores);
            return (pCores, eCores);
        }

        var groupedByEfficiency = cpuSetsInfo.CpuSets
            .GroupBy(c => c.EfficiencyClass)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in groupedByEfficiency)
        {
            var cores = GroupCpuSetsByCore(group.ToList());

            if (IsIntel() && cpuSetsInfo.EfficiencyClass)
            {
                if (group.Key == 0)
                {
                    eCores.AddRange(cores);
                }
                else
                {
                    pCores.AddRange(cores);
                }
            }
            else
            {
                pCores.AddRange(cores);
            }
        }

        return (pCores, eCores);
    }

    private static List<CpuCore> GroupCpuSetsByCore(List<CpuSet> cpuSets)
    {
        var cores = new Dictionary<byte, (CpuCore Core, int SequentialNumber)>();
        int coreCount = 0;

        foreach (var cpuSet in cpuSets.OrderBy(c => c.LogicalProcessorIndex))
        {
            if (!cores.ContainsKey(cpuSet.CoreIndex))
            {
                var core = new CpuCore
                {
                    CoreIndex = cpuSet.CoreIndex,
                    Name = $"Core {coreCount}"
                };
                cores[cpuSet.CoreIndex] = (core, coreCount);
                coreCount++;
            }

            ulong bitMask = 1UL << cpuSet.LogicalProcessorIndex;

            var thread = new CpuThread
            {
                CpuId = cpuSet.Id,
                Name = $"Thread {cpuSet.LogicalProcessorIndex}",
                BitMask = bitMask
            };

            cores[cpuSet.CoreIndex].Core.Threads.Add(thread);
        }

        return [.. cores.Values.OrderBy(c => c.SequentialNumber).Select(c => c.Core)];
    }
}
