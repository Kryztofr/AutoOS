using AutoOS.Views.Settings.Scheduling.ViewModels;
using AutoOS.Helpers.CPU;

namespace AutoOS.Views.Settings.Scheduling;

public sealed partial class SchedulingDialog : Page
{
    public DeviceAffinityViewModel ViewModel { get; }

    public string Location { get; }

    public SchedulingDialog()
    {
        InitializeComponent();
    }

    internal SchedulingDialog(SchedulingItem device, CpuSetsInfo cpuSetsInfo)
    {
        Location = device.Location;
        ViewModel = new DeviceAffinityViewModel(device, cpuSetsInfo);
        InitializeComponent();
    }
}