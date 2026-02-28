using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.Helpers.Device;

namespace AutoOS.Views.Settings.Scheduling;

public partial class SchedulingItem : INotifyPropertyChanged
{
    public DeviceType DeviceType { get; set; }
    public string DeviceDescription { get; set; }
    public string FriendlyName { get; set; }
    public string DevObjName { get; set; }
    public string PnpDeviceId { get; set; }
    public string Location { get; set; }

    public string Name => string.IsNullOrWhiteSpace(FriendlyName) ? DeviceDescription : FriendlyName;

    private uint _msiSupported;
    public uint MsiSupported
    {
        get => _msiSupported;
        set { _msiSupported = value; OnPropertyChanged(); OnPropertyChanged(nameof(MsiModeDisplay)); }
    }

    private uint _msiLimit;
    public uint MsiLimit
    {
        get => _msiLimit;
        set { _msiLimit = value; OnPropertyChanged(); OnPropertyChanged(nameof(MsiLimitDisplay)); }
    }

    private uint _maxMsiLimit;
    public uint MaxMsiLimit
    {
        get => _maxMsiLimit;
        set { _maxMsiLimit = value; OnPropertyChanged(); OnPropertyChanged(nameof(MaxMsiLimitDisplay)); }
    }

    private uint _devicePolicy;
    public uint DevicePolicy
    {
        get => _devicePolicy;
        set { _devicePolicy = value; OnPropertyChanged(); OnPropertyChanged(nameof(DevicePolicyDisplay)); }
    }

    private uint _devicePriority;
    public uint DevicePriority
    {
        get => _devicePriority;
        set { _devicePriority = value; OnPropertyChanged(); OnPropertyChanged(nameof(DevicePriorityDisplay)); }
    }

    private ulong _assignmentSetOverride;
    public ulong AssignmentSetOverride
    {
        get => _assignmentSetOverride;
        set { _assignmentSetOverride = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpecifiedProcessorsDisplay)); }
    }

    public string MsiModeDisplay => MsiSupported == 1 ? "On" : "Off";
    public string MsiLimitDisplay => MsiLimit == 0 ? "Auto" : MsiLimit.ToString("F0");
    public string DevicePolicyDisplay => PolicyNames.TryGetValue(DevicePolicy, out var name) ? name : $"{DevicePolicy}";
    public string DevicePriorityDisplay => PriorityNames.TryGetValue(DevicePriority, out var name) ? name : $"{DevicePriority}";
    public string SpecifiedProcessorsDisplay => FormatProcessMask(AssignmentSetOverride);
    public string MaxMsiLimitDisplay => MaxMsiLimit == 0 ? string.Empty : MaxMsiLimit.ToString("F0");

    private static readonly Dictionary<uint, string> PolicyNames = new()
    {
        { 0, "Default" },
        { 1, "All Close Proc" },
        { 2, "One Close Proc" },
        { 3, "All Proc in Machine" },
        { 4, "Specified Proc" },
        { 5, "Spread Messages Across All Proc" }
    };

    private static readonly Dictionary<uint, string> PriorityNames = new()
    {
        { 0, "Undefined" },
        { 1, "Low" },
        { 2, "Normal" },
        { 3, "High" }
    };

    private static string FormatProcessMask(ulong mask)
    {
        if (mask == 0) return string.Empty;

        var processors = new List<string>();
        for (int index = 0; mask != 0; index++, mask >>= 1)
        {
            if ((mask & 1UL) != 0)
                processors.Add(index.ToString());
        }
        return string.Join(", ", processors);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
