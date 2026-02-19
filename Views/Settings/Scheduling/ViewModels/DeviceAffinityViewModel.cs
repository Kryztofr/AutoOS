using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoOS.Views.Settings.Scheduling.Models;
using AutoOS.Helpers.CPU;
using AutoOS.Views.Settings.Scheduling.Services;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace AutoOS.Views.Settings.Scheduling.ViewModels;

public class IrqPolicyItem
{
    public uint Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class IrqPriorityItem
{
    public uint Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DeviceAffinityViewModel : INotifyPropertyChanged
{
    private DeviceType DeviceType { get; set; }
    private List<DeviceInfo> Devices { get; set; } = [];
    private DeviceInfo SelectedDevice { get; set; }
    private readonly string _targetDevObjName;
    private HDEVINFO _deviceInfoSet = default;

    public bool MsiSupported
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(IsMsiLimitEnabled));
        }
    }

    public double MessageNumberLimit
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsMsiLimitEnabled => MsiSupported;

    public int DevicePriority
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int DevicePolicy
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(IsCoreSelectionEnabled));
        }
    }

    public bool IsCoreSelectionEnabled => DevicePolicy == 4;

    public ObservableCollection<CpuCore> PCores { get; set; } = [];
    public ObservableCollection<CpuCore> ECores { get; set; } = [];

    public ulong ProcessMask
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool HasEfficiencyClass { get; private set; }

    public uint MaxMSILimit
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(EffectiveMaxMSILimit));
        }
    }

    public double EffectiveMaxMSILimit => MaxMSILimit > 0 ? MaxMSILimit : 2048;

    public ObservableCollection<IrqPolicyItem> IrqPolicies { get; } = [];
    public ObservableCollection<IrqPriorityItem> IrqPriorities { get; } = [];

    public GridLength ECoreColumnWidth => HasEfficiencyClass ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
    public double ColumnSpacing => HasEfficiencyClass ? 12 : 0;

    public DeviceAffinityViewModel(DeviceType deviceType, string targetDevObjName = null)
    {
        DeviceType = deviceType;
        _targetDevObjName = targetDevObjName;
        InitializeIrqOptions();
        LoadDevices();
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

    private void LoadDevices()
    {
        Devices = DeviceDetectionService.FindDevicesByType(DeviceType);

        if (Devices.Count > 0)
            _deviceInfoSet = Devices[0].DeviceInfoSet;

        if (!string.IsNullOrWhiteSpace(_targetDevObjName))
            SelectedDevice = Devices.FirstOrDefault(d => string.Equals(d.DevObjName, _targetDevObjName, StringComparison.OrdinalIgnoreCase));

        SelectedDevice ??= Devices.FirstOrDefault(d => d.RegistryKey != null) ?? Devices.FirstOrDefault();
        LoadCurrentSettings();
    }

    public void Cleanup()
    {
        if (_deviceInfoSet.Value != 0 && _deviceInfoSet.Value != (nint)(-1))
        {
            PInvoke.SetupDiDestroyDeviceInfoList(_deviceInfoSet);
            _deviceInfoSet = default;
        }

        foreach (var device in Devices)
            if (device.RegistryKey != null)
                device.RegistryKey.Close();
    }

    private void LoadCurrentSettings()
    {
        LoadCpuInformation();

        if (SelectedDevice == null || SelectedDevice.RegistryKey == null)
            return;

        var settings = RegistryService.ReadDeviceSettings(SelectedDevice.RegistryKey, SelectedDevice.MaxMSILimit);

        MsiSupported = settings.MsiSupported == 1u;
        MessageNumberLimit = settings.MessageNumberLimit;
        DevicePolicy = (int)settings.DevicePolicy;
        DevicePriority = (int)settings.DevicePriority;
        ProcessMask = settings.AssignmentSetOverride;
        MaxMSILimit = settings.MaxMSILimit;

        SetCpuSelectionFromMask(ProcessMask);
    }

    private void LoadCpuInformation()
    {
        //CpuSetInformationFake.Fake13600KF();
        //CpuSetInformationFake.Fake13900();
        //CpuSetInformationFake.Fake13900WithoutHT();
        //CpuSetInformationFake.Fake5900x();
        //CpuSetInformationFake.Fake8Threads();
        //CpuSetInformationFake.FakeNumaCCD12Core();
        //CpuSetInformationFake.Fake2CCD12CoreHT();

        var cpuSetsInfo = CpuDetectionService.GetCpuSets();
        HasEfficiencyClass = cpuSetsInfo.EfficiencyClass;

        var (pCores, eCores) = CpuDetectionService.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);

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
        if (SelectedDevice == null)
        {
            if (OnSettingsApplied != null)
                OnSettingsApplied(new DeviceSettingsService.ApplyResult
            {
                Success = false,
                NeedsRestart = false,
                Message = "No device selected to apply settings to."
            });
            return;
        }

        var result = DeviceSettingsService.ApplySettingsToDevices(
            [SelectedDevice],
            MsiSupported,
            (uint)MessageNumberLimit,
            (uint)DevicePolicy,
            (uint)DevicePriority,
        ProcessMask,
        DeviceType
        );

        if (OnSettingsApplied != null)
            OnSettingsApplied(result);
    }

    public event Action<DeviceSettingsService.ApplyResult> OnSettingsApplied;
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
