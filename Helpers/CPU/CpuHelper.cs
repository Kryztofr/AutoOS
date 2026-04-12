using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinRT;

namespace AutoOS.Helpers.CPU;

public enum CpuVendor
{
    Unknown,
    Intel,
    AMD
}

public sealed class CpuArchitecture
{
    public CpuVendor Vendor { get; set; }
    public uint Family { get; set; }
    public uint Model { get; set; }
    public uint Stepping { get; set; }
    public uint DisplayFamily { get; set; }
    public uint DisplayModel { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ArchitectureName { get; set; } = string.Empty;
}

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
    public static CpuArchitecture GetCpuArchitecture()
    {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        
        var vendorId = key?.GetValue("VendorIdentifier")?.ToString() ?? "";
        var processorName = key?.GetValue("ProcessorNameString")?.ToString() ?? "";
        var identifier = key?.GetValue("Identifier")?.ToString() ?? "";

        var vendor = CpuVendor.Unknown;
        if (vendorId.Contains("GenuineIntel", StringComparison.OrdinalIgnoreCase))
            vendor = CpuVendor.Intel;
        else if (vendorId.Contains("AuthenticAMD", StringComparison.OrdinalIgnoreCase))
            vendor = CpuVendor.AMD;

        uint family = 0, model = 0, stepping = 0;
        
        var parts = identifier.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "Family" && i + 1 < parts.Length && uint.TryParse(parts[i + 1], out uint f))
                family = f;
            else if (parts[i] == "Model" && i + 1 < parts.Length && uint.TryParse(parts[i + 1], out uint m))
                model = m;
            else if (parts[i] == "Stepping" && i + 1 < parts.Length && uint.TryParse(parts[i + 1], out uint s))
                stepping = s;
        }

        uint displayFamily = family;
        uint displayModel = model;
        
        var arch = new CpuArchitecture
        {
            Vendor = vendor,
            Family = family,
            Model = model,
            Stepping = stepping,
            DisplayFamily = displayFamily,
            DisplayModel = displayModel
        };

        if (vendor == CpuVendor.Intel)
        {
            arch.DisplayName = processorName;
            arch.ArchitectureName = GetIntelArchName(displayFamily, displayModel);
            arch.DisplayFamily = 0x06;
        }
        else if (vendor == CpuVendor.AMD)
        {
            arch.DisplayName = processorName;
            arch.ArchitectureName = GetAmdArchName(displayFamily, displayModel);
        }

        return arch;
    }

    private static string GetIntelArchName(uint family, uint model)
    {
        if (family != 0x06) return "Unknown Intel";

        return model switch
        {
            0x97 or 0x9A => "Alder Lake",
            0xBA or 0xB7 or 0xBF => "Raptor Lake",
            0xAA => "Meteor Lake",
            0xBD => "Lunar Lake",
            0xAC or 0xAE => "Granite Rapids",
            0xAF => "Sierra Forest",
            0xCF => "Emerald Rapids",
            0x8F => "Sapphire Rapids",
            0x8C or 0x8D => "Tiger Lake",
            0xA7 => "Rocket Lake",
            0x7E => "Ice Lake",
            0xA5 or 0xA6 => "Comet Lake",
            0x66 => "Cannon Lake",
            0x8E or 0x9E => "Kaby Lake / Coffee Lake",
            0x55 => "Skylake-X / Cascade Lake",
            0x4E or 0x5E => "Skylake",
            0x3D or 0x47 or 0x4F or 0x56 => "Broadwell",
            0x3C or 0x45 or 0x46 or 0x3F => "Haswell",
            0x3A or 0x3E => "Ivy Bridge",
            0x2A or 0x2D => "Sandy Bridge",
            0xBE => "Gracemont (N-series)",
            _ => $"Intel Model {model:X2}H"
        };
    }

    private static string GetAmdArchName(uint family, uint model)
    {
        if (family == 0x17)
        {
            if (model <= 0x0F || (model >= 0x10 && model <= 0x1F) || model == 0x20)
            {
                if (model == 0x08 || model == 0x18) return "Zen+";
                return "Zen 1";
            }
            if ((model >= 0x30 && model <= 0x3F) || (model >= 0x60 && model <= 0x6F) || (model >= 0x70 && model <= 0x7F) || (model >= 0x90 && model <= 0x9F))
                return "Zen 2";
        }
        else if (family == 0x19)
        {
            if ((model >= 0x00 && model <= 0x0F) || (model >= 0x20 && model <= 0x2F) || (model >= 0x50 && model <= 0x5F))
                return "Zen 3";
            if ((model >= 0x10 && model <= 0x1F) || (model >= 0x60 && model <= 0x6F) || (model >= 0x70 && model <= 0x7F))
                return "Zen 4";
        }
        else if (family == 0x1A)
        {
            return "Zen 5";
        }

        return $"AMD Family {family:X2}H Model {model:X2}H";
    }

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

            info.CpuSets = [.. cpuSets.OrderBy(c => c.EfficiencyClass).ThenBy(c => c.CoreIndex).ThenBy(c => c.LogicalProcessorIndex)];
            ProcessCpuSets(info.CpuSets, info);
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

        info.CoreCount = cpuSets.Count;
        byte lastEfficiencyClass = 0;
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
                    info.MaxThreadsPerCore = threadsDiff + 1;
            }

            if (!info.EfficiencyClass && lastEfficiencyClass != cpuSet.EfficiencyClass)
                info.EfficiencyClass = true;

            if (!info.LastLevelCache && lastLevelCache != cpuSet.LastLevelCacheIndex)
                info.LastLevelCache = true;

            if (!info.NumaNode && lastNumaNodeIndex != cpuSet.NumaNodeIndex)
                info.NumaNode = true;
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

        foreach (var group in groupedByEfficiency)
        {
            var cores = GroupCpuSetsByCore(group.ToList());
            if (GetCpuArchitecture().Vendor == CpuVendor.Intel)
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
