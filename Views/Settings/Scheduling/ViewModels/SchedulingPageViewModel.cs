using AutoOS.Views.Settings.Scheduling.Services;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace AutoOS.Views.Settings.Scheduling.ViewModels;

public class SchedulingPageViewModel : INotifyPropertyChanged
{
    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<DeviceItemViewModel> GpuDevices { get; } = [];
    public ObservableCollection<DeviceItemViewModel> XhciDevices { get; } = [];
    public ObservableCollection<DeviceItemViewModel> NicDevices { get; } = [];
    public ObservableCollection<DeviceGroup> DeviceGroups { get; } = [];

    public SchedulingPageViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        DeviceGroups.Add(new DeviceGroup("GPUs", DeviceType.GPU, GpuDevices));
        DeviceGroups.Add(new DeviceGroup("XHCI Controllers", DeviceType.XHCI, XhciDevices));
        DeviceGroups.Add(new DeviceGroup("NICs", DeviceType.NIC, NicDevices));
        _ = LoadDevicesAsync();
    }

    private async Task LoadDevicesAsync()
    {
        var tasks = new[]
        {
            Task.Run(() => LoadDeviceGroup(DeviceType.GPU)),
            Task.Run(() => LoadDeviceGroup(DeviceType.XHCI)),
            Task.Run(() => LoadDeviceGroup(DeviceType.NIC))
        };
        await Task.WhenAll(tasks);
    }

    private void LoadDeviceGroup(DeviceType deviceType)
    {
        var devices = DeviceDetectionService.FindDevicesByType(deviceType);
        HDEVINFO handle = default;
        var viewModels = new List<DeviceItemViewModel>();

        foreach (var device in devices)
        {
            if (handle.Value == 0)
                handle = device.DeviceInfoSet;

            viewModels.Add(new DeviceItemViewModel(deviceType, device));
            device.RegistryKey?.Close();
        }

        if (handle.Value != 0 && handle.Value != (nint)(-1))
        {
            PInvoke.SetupDiDestroyDeviceInfoList(handle);
        }

        var collection = GetCollection(deviceType);
        if (_dispatcherQueue != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var vm in viewModels)
                    collection.Add(vm);
            });
        }
        else
        {
            foreach (var vm in viewModels)
                collection.Add(vm);
        }
    }

    private ObservableCollection<DeviceItemViewModel> GetCollection(DeviceType type) => type switch
    {
        DeviceType.GPU => GpuDevices,
        DeviceType.XHCI => XhciDevices,
        DeviceType.NIC => NicDevices,
        _ => GpuDevices
    };

    public void UpdateDevice(DeviceType deviceType, string devObjName, DeviceSettings appliedSettings = null)
    {
        if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(() => UpdateDeviceInternal(deviceType, devObjName, appliedSettings));
            return;
        }

        UpdateDeviceInternal(deviceType, devObjName, appliedSettings);
    }

    private void UpdateDeviceInternal(DeviceType deviceType, string devObjName, DeviceSettings appliedSettings = null)
    {
        var collection = GetCollection(deviceType);
        var deviceViewModel = collection.FirstOrDefault(d => 
            string.Equals(d.DevObjName, devObjName, StringComparison.OrdinalIgnoreCase));

        if (deviceViewModel == null)
            return;

        var devices = DeviceDetectionService.FindDevicesByType(deviceType);
        var device = devices.FirstOrDefault(d => 
            string.Equals(d.DevObjName, devObjName, StringComparison.OrdinalIgnoreCase));

        if (device != null && device.RegistryKey != null)
        {
            deviceViewModel.RefreshSettings(device);
            device.RegistryKey?.Close();
        }

        if (devices.Count > 0)
        {
            var handle = devices[0].DeviceInfoSet;
            if (handle.Value != 0 && handle.Value != (nint)(-1))
            {
                PInvoke.SetupDiDestroyDeviceInfoList(handle);
            }
        }

        var index = collection.IndexOf(deviceViewModel);
        if (index >= 0)
        {
            collection[index] = deviceViewModel;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class DeviceGroup : INotifyPropertyChanged
{
    public string Title { get; }
    public DeviceType DeviceType { get; }
    public ObservableCollection<DeviceItemViewModel> Items { get; }
    public bool HasItems => Items.Count > 0;

    public DeviceGroup(string title, DeviceType deviceType, ObservableCollection<DeviceItemViewModel> items)
    {
        Title = title;
        DeviceType = deviceType;
        Items = items;
        Items.CollectionChanged += (_, _) =>
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(HasItems)));
        };
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
