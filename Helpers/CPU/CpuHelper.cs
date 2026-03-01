using Microsoft.Win32;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinRT;

namespace AutoOS.Helpers.CPU;

public sealed class CpuSet
{
    public uint Id { get; set; }
    public byte CoreIndex { get; set; }
    public byte LogicalProcessorIndex { get; set; }
    public byte EfficiencyClass { get; set; }
    public byte LastLevelCacheIndex { get; set; }
    public byte NumaNodeIndex { get; set; }
}

[GeneratedBindableCustomProperty]
public sealed partial class CpuThread : INotifyPropertyChanged
{
    private bool _isSelected;
    public uint CpuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ulong BitMask { get; set; }
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

[GeneratedBindableCustomProperty]
public sealed partial class CpuCore
{
    public byte CoreIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<CpuThread> Threads { get; set; } = [];
}

public sealed class CpuSetsInfo
{
    public bool HyperThreading { get; set; }
    public int CoreCount { get; set; }
    public int MaxThreadsPerCore { get; set; }
    public bool NumaNode { get; set; }
    public bool LastLevelCache { get; set; }
    public bool EfficiencyClass { get; set; }
    public List<CpuSet> CpuSets { get; set; } = [];
}

public partial class CpuHelper
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SYSTEM_CPU_SET_INFORMATION
    {
        public uint Size;
        public uint Type;
        public SYSTEM_CPU_SET_INFORMATION_ANONYMOUS Anonymous;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SYSTEM_CPU_SET_INFORMATION_ANONYMOUS
    {
        [FieldOffset(0)]
        public SYSTEM_CPU_SET_INFORMATION_CPU_SET CpuSet;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SYSTEM_CPU_SET_INFORMATION_CPU_SET
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

    public static bool IsIntel()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        var vendor = key?.GetValue("VendorIdentifier")?.ToString() ?? "";
        return vendor.Contains("GenuineIntel", StringComparison.OrdinalIgnoreCase);
    }

    public unsafe static CpuSetsInfo GetCpuSets()
    {
        var info = new CpuSetsInfo();
        var cpuSets = new List<CpuSet>();

        uint bufferSize = 0;
        PInvoke.GetSystemCpuSetInformation(null, 0, &bufferSize, HANDLE.Null, 0);
        
        if (bufferSize == 0) return info;

        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            uint returnedLength = 0;
            if (PInvoke.GetSystemCpuSetInformation(
                (Windows.Win32.System.SystemInformation.SYSTEM_CPU_SET_INFORMATION*)buffer,
                bufferSize,
                &returnedLength,
                HANDLE.Null,
                0))
            {
                int offset = 0;
                while (offset < (int)returnedLength)
                {
                    var cpuSetInfo = Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(IntPtr.Add(buffer, offset));
                    if (cpuSetInfo.Size == 0) break;

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
        if (cpuSets.Count == 0) return;

        byte lastEfficiencyClass = cpuSets[0].EfficiencyClass;
        byte lastLevelCache = cpuSets[0].LastLevelCacheIndex;
        byte lastNumaNodeIndex = cpuSets[0].NumaNodeIndex;

        for (int i = 0; i < cpuSets.Count; i++)
        {
            var cpuSet = cpuSets[i];

            if (cpuSet.CoreIndex != cpuSet.LogicalProcessorIndex)
            {
                info.HyperThreading = true;
                int threadsDiff = Math.Abs(cpuSet.LogicalProcessorIndex - cpuSet.CoreIndex);
                if (info.MaxThreadsPerCore < threadsDiff)
                    info.MaxThreadsPerCore = threadsDiff;
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
            pCores.AddRange(GroupCpuSetsByCore(cpuSetsInfo.CpuSets));
            return (pCores, eCores);
        }

        var groupedByEfficiency = cpuSetsInfo.CpuSets
            .GroupBy(c => c.EfficiencyClass)
            .OrderBy(g => g.Key)
            .ToList();

        bool isIntel = IsIntel();

        foreach (var group in groupedByEfficiency)
        {
            var cores = GroupCpuSetsByCore(group.ToList());
            if (isIntel)
            {
                if (group.Key == 0) eCores.AddRange(cores);
                else pCores.AddRange(cores);
            }
            else
            {
                pCores.AddRange(cores);
            }
        }

        return (pCores, eCores);
    }

    public static List<CpuCore> GroupCpuSetsByCore(List<CpuSet> cpuSets)
    {
        var cores = new Dictionary<byte, CpuCore>();
        int sequentialNumber = 0;

        foreach (var cpuSet in cpuSets.OrderBy(c => c.LogicalProcessorIndex))
        {
            if (!cores.TryGetValue(cpuSet.CoreIndex, out var core))
            {
                core = new CpuCore
                {
                    CoreIndex = cpuSet.CoreIndex,
                    Name = $"Core {sequentialNumber++}"
                };
                cores[cpuSet.CoreIndex] = core;
            }

            core.Threads.Add(new CpuThread
            {
                CpuId = cpuSet.Id,
                Name = $"Thread {cpuSet.LogicalProcessorIndex}",
                BitMask = 1UL << cpuSet.LogicalProcessorIndex
            });
        }

        return [.. cores.Values];
    }

    public static class CpuSetInformationFake
    {
        private static List<CpuSet> _fakeCpuSets;

        public static List<CpuSet> FakeCpuSets
        {
            get => _fakeCpuSets;
            set => _fakeCpuSets = value;
        }

        // 12 cores, 24 threads
        public static void Fake5900x()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 24;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    cpuSet.CoreIndex = (byte)(i / 2);
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i > 11)
                {
                    cpuSet.LastLevelCacheIndex = 12;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 24 cores (8 P-cores + 16 E-cores), 32 threads
        public static void Fake13900()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 32;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i < 16 && i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    cpuSet.CoreIndex = (byte)(i < 16 ? i / 2 : i - 8);
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i < 16)
                {
                    cpuSet.EfficiencyClass = 1;
                }
                else
                {
                    cpuSet.EfficiencyClass = 0;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 24 cores (8 P-cores + 16 E-cores), 24 threads
        public static void Fake13900WithoutHT()
        {
            var cpuSets = new List<CpuSet>();
            int count = 24;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i,
                    CoreIndex = (byte)i
                };

                if (i < 8)
                {
                    cpuSet.EfficiencyClass = 1;
                }
                else
                {
                    cpuSet.EfficiencyClass = 0;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 8 cores, 8 threads
        public static void Fake8Threads()
        {
            var cpuSets = new List<CpuSet>();
            int count = 8;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i,
                    CoreIndex = (byte)i
                };

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 12 cores
        public static void FakeNumaCCD12Core()
        {
            var cpuSets = new List<CpuSet>();
            int count = 12;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i,
                    CoreIndex = (byte)i
                };

                if (i > 5)
                {
                    cpuSet.LastLevelCacheIndex = 6;
                    cpuSet.NumaNodeIndex = 6;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 12 cores with hyperthreading, 2 CCDs
        public static void Fake2CCD12CoreHT()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 24;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    cpuSet.CoreIndex = (byte)(i / 2);
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i > 11)
                {
                    cpuSet.LastLevelCacheIndex = 12;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }

        // 14 cores (6 P-cores + 8 E-cores), 20 threads
        public static void Fake13600KF()
        {
            var cpuSets = new List<CpuSet>();
            byte lastCoreIndex = 0;
            int count = 20;
            uint index = 0x100;

            for (int i = 0; i < count; i++)
            {
                var cpuSet = new CpuSet
                {
                    Id = index + (uint)i,
                    LogicalProcessorIndex = (byte)i
                };

                if (i < 12 && i % 2 != 0)
                {
                    cpuSet.CoreIndex = lastCoreIndex;
                }
                else
                {
                    if (i < 12)
                    {
                        cpuSet.CoreIndex = (byte)(i / 2);
                    }
                    else
                    {
                        cpuSet.CoreIndex = (byte)(6 + (i - 12));
                    }
                    lastCoreIndex = cpuSet.CoreIndex;
                }

                if (i < 12)
                {
                    cpuSet.EfficiencyClass = 1;
                }
                else
                {
                    cpuSet.EfficiencyClass = 0;
                }

                cpuSets.Add(cpuSet);
            }

            _fakeCpuSets = cpuSets;
        }
    }
}
