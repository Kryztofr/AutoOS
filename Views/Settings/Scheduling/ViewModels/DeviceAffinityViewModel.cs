using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.Helpers.Device;
using AutoOS.Helpers.CPU;
using WinRT;

namespace AutoOS.Views.Settings.Scheduling.ViewModels;

[GeneratedBindableCustomProperty]
public sealed partial class IrqPolicyItem
{
    public uint Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

[GeneratedBindableCustomProperty]
public sealed partial class IrqPriorityItem
{
    public uint Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public partial class DeviceAffinityViewModel : INotifyPropertyChanged
{
    private readonly SchedulingItem _selectedItem;

    private bool _msiSupported;
    public bool MsiSupported
    {
        get => _msiSupported;
        set
        {
            if (SetProperty(ref _msiSupported, value))
                OnPropertyChanged(nameof(IsMsiLimitEnabled));
        }
    }

    private double _MsiLimit;
    public double MsiLimit
    {
        get => _MsiLimit;
        set => SetProperty(ref _MsiLimit, value);
    }

    public bool IsMsiLimitEnabled => MsiSupported;

    private int _devicePriority;
    public int DevicePriority
    {
        get => _devicePriority;
        set => SetProperty(ref _devicePriority, value);
    }

    private int _devicePolicy;
    public int DevicePolicy
    {
        get => _devicePolicy;
        set
        {
            if (SetProperty(ref _devicePolicy, value))
                OnPropertyChanged(nameof(IsCoreSelectionEnabled));
        }
    }

    public bool IsCoreSelectionEnabled => DevicePolicy == 4;

    private ObservableCollection<CpuCore> _pCores = [];
    public ObservableCollection<CpuCore> PCores
    {
        get => _pCores;
        set => SetProperty(ref _pCores, value);
    }

    private ObservableCollection<CpuCore> _eCores = [];
    public ObservableCollection<CpuCore> ECores
    {
        get => _eCores;
        set => SetProperty(ref _eCores, value);
    }

    private ulong _processMask;
    public ulong ProcessMask
    {
        get => _processMask;
        set => SetProperty(ref _processMask, value);
    }

    public bool HasEfficiencyClass { get; private set; }

    private uint _MaxMsiLimit;
    public uint MaxMsiLimit
    {
        get => _MaxMsiLimit;
        private set
        {
            if (SetProperty(ref _MaxMsiLimit, value))
                OnPropertyChanged(nameof(EffectiveMaxMsiLimit));
        }
    }

    public double EffectiveMaxMsiLimit => MaxMsiLimit > 0 ? MaxMsiLimit : 2048;

    public ObservableCollection<IrqPolicyItem> IrqPolicies { get; } = [];
    public ObservableCollection<IrqPriorityItem> IrqPriorities { get; } = [];

    public GridLength ECoreColumnWidth => HasEfficiencyClass ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
    public double ColumnSpacing => HasEfficiencyClass ? 12 : 0;

    public DeviceAffinityViewModel(SchedulingItem selectedItem, CpuSetsInfo cpuSetsInfo)
    {
        _selectedItem = selectedItem;
        InitializeIrqOptions();
        LoadCpuInformation(cpuSetsInfo);
        LoadCurrentSettings();
    }

    private void InitializeIrqOptions()
    {
        IrqPolicies.Add(new IrqPolicyItem { Value = 0, Name = "IrqPolicyMachineDefault" });
        IrqPolicies.Add(new IrqPolicyItem { Value = 1, Name = "IrqPolicyAllCloseProcessors" });
        IrqPolicies.Add(new IrqPolicyItem { Value = 2, Name = "IrqPolicyOneCloseProcessor" });
        IrqPolicies.Add(new IrqPolicyItem { Value = 3, Name = "IrqPolicyAllProcessorsInMachine" });
        IrqPolicies.Add(new IrqPolicyItem { Value = 4, Name = "IrqPolicySpecifiedProcessors" });
        IrqPolicies.Add(new IrqPolicyItem { Value = 5, Name = "IrqPolicySpreadMessagesAcrossAllProcessors" });

        IrqPriorities.Add(new IrqPriorityItem { Value = 0, Name = "Undefined" });
        IrqPriorities.Add(new IrqPriorityItem { Value = 1, Name = "Low" });
        IrqPriorities.Add(new IrqPriorityItem { Value = 2, Name = "Normal" });
        IrqPriorities.Add(new IrqPriorityItem { Value = 3, Name = "High" });
    }

    private void LoadCurrentSettings()
    {
        MsiSupported = _selectedItem.MsiSupported == 1u;
        MsiLimit = _selectedItem.MsiLimit;
        DevicePolicy = (int)_selectedItem.DevicePolicy;
        DevicePriority = (int)_selectedItem.DevicePriority;
        ProcessMask = _selectedItem.AssignmentSetOverride;
        MaxMsiLimit = _selectedItem.MaxMsiLimit;

        SetCpuSelectionFromMask(ProcessMask);
    }

    private void LoadCpuInformation(CpuSetsInfo cpuSetsInfo)
    {
        HasEfficiencyClass = cpuSetsInfo.EfficiencyClass;

        var (pCores, eCores) = CpuHelper.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);

        PCores = new ObservableCollection<CpuCore>(pCores);
        ECores = new ObservableCollection<CpuCore>(eCores);

        SetCpuSelectionFromMask(ProcessMask);

        foreach (var thread in PCores.Concat(ECores).SelectMany(c => c.Threads))
            thread.PropertyChanged += Thread_PropertyChanged;
    }

    private void SetCpuSelectionFromMask(ulong mask)
    {
        foreach (var thread in PCores.Concat(ECores).SelectMany(c => c.Threads))
            thread.IsSelected = (mask & thread.BitMask) != 0;
    }

    private void Thread_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CpuThread.IsSelected) && sender is CpuThread thread)
            ProcessMask = thread.IsSelected ? ProcessMask | thread.BitMask : ProcessMask & ~thread.BitMask;
    }

    public void ApplySettings()
    {
        var targetDevice = DeviceHelper.GetDevices(_selectedItem.DeviceType).FirstOrDefault(device => string.Equals(device.PnpDeviceId, _selectedItem.PnpDeviceId, StringComparison.OrdinalIgnoreCase));
        
        var result = DeviceHelper.ApplySettingsToDevices(
            [targetDevice],
            MsiSupported,
            (uint)MsiLimit,
            (uint)DevicePolicy,
            (uint)DevicePriority,
            ProcessMask,
            _selectedItem.DeviceType
        );

        OnSettingsApplied?.Invoke(result);
    }

    internal event Action<DeviceHelper.ApplyResult> OnSettingsApplied;
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}