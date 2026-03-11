using AutoOS.Helpers.GPU;
using AutoOS.Helpers.Device;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceProcess;
using Windows.Storage;
using AutoOS.Helpers.Picker;

namespace AutoOS.Views.Settings;

public sealed partial class GraphicsPage : Page
{
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    private bool isInitializingHDCPState = true;
    private bool isInitializingHDMIDPAudioState = true;
    private bool isInitializingOBSState = true;
    public ObservableCollection<GpuInfo> GPUs { get; } = [];
    public GraphicsPage()
    {
        InitializeComponent();
        GetGpus();
        GetOBSState();
        Loaded += GraphicsPage_Loaded;
    }

    private async void GraphicsPage_Loaded(object sender, RoutedEventArgs e)
    {
        isInitializingHDCPState = false;
        isInitializingHDMIDPAudioState = false;
    }

    public void GetGpus()
    {
        var detectedGpus = GpuHelper.GetGPUs();
        detectedGpus = [.. detectedGpus.OrderBy(g => g.Location)];
        GPUs.Clear();

        foreach (var gpu in detectedGpus)
        {
            GPUs.Add(gpu);

            if (!gpu.IsInstalled)
            {
                if (localSettings.Values.TryGetValue($"HDCP_{gpu.PnPDeviceId}", out var hdcp))
                    gpu.HDCP = Convert.ToBoolean(hdcp);

                if (localSettings.Values.TryGetValue($"HDMIDPAudio_{gpu.PnPDeviceId}", out var hdmidpaudio))
                    gpu.HDMIDPAudio = Convert.ToBoolean(hdmidpaudio);
            }
        }
    }

    private async void ProgressButton_Loaded(object sender, RoutedEventArgs e)
    {
        ProgressButton progressButton = (ProgressButton)sender;

        progressButton.IsChecked = true;
    }

    private async void ProgressButton_Checked(object sender, RoutedEventArgs e)
    {
        
        string newestVersion = string.Empty;
        string newestDownloadUrl = string.Empty;

        ProgressButton progressButton = (ProgressButton)sender;
        GpuInfo gpu = (GpuInfo)progressButton.DataContext;

        progressButton.IsHitTestVisible = false;

        try
        {
            // update logic
            if (progressButton.Content?.ToString().Contains("Update to") == true)
            {
                if (new ServiceController("Beep").Status == ServiceControllerStatus.Running)
                {
                    // save affinity
                    var savedConfig = SaveAffinity();

                    // close obs studio
                    if (Process.GetProcessesByName("obs64").Length > 0)
                    {
                        foreach (var process in Process.GetProcessesByName("obs64"))
                        {
                            process.Kill();
                            process.WaitForExit();
                        }
                    }

                    switch (gpu.VendorId)
                    {
                        case "10de":
                            (newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate(gpu);
                            var nvidiaActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
                            nvidiaActions = NvidiaHelper.DriverActions(gpu, newestDownloadUrl, progressButton);
                            await RunActions(progressButton, nvidiaActions);
                            break;
                        case "1002":
                            (newestVersion, newestDownloadUrl) = await AmdHelper.CheckUpdate(gpu);
                            var amdActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
                            amdActions = AmdHelper.DriverActions(gpu, newestDownloadUrl, progressButton);
                            await RunActions(progressButton, amdActions);
                            break;
                        case "8086":
                            (newestVersion, newestDownloadUrl) = await IntelHelper.CheckUpdate(gpu);
                            var intelActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
                            intelActions = IntelHelper.DriverActions(gpu, newestDownloadUrl, progressButton);
                            await RunActions(progressButton, intelActions);
                            break;
                    }

                    // reapply affinity
                    await ReapplyAffinity(savedConfig, progressButton);

                    // apply profile
                    if (localSettings.Values["MsiProfile"] != null)
                    {
                        await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());
                    }

                    // launch obs studio
                    if (!(localSettings.Values["OBS"] as int? == 0))
                    {
                        await Task.Run(() => Process.Start(new ProcessStartInfo("cmd.exe") { Arguments = @"/c del ""%APPDATA%\obs-studio\.sentinel"" /s /f /q", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false })?.WaitForExit());
                        await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe", Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = @"C:\Program Files\obs-studio\bin\64bit" }));
                    }
                }
                else
                {
                    progressButton.IsChecked = false;

                    var dialog = new ContentDialog
                    {
                        Title = "Services & Drivers are disabled",
                        Content = "Please enable Services & Drivers before updating.",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = App.MainWindow.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }

                progressButton.IsChecked = false;
                return;
            }
            // install logic
            else if (progressButton.Content?.ToString().Contains("Install") == true)
            {
                if (new ServiceController("Beep").Status == ServiceControllerStatus.Running)
                {
                    // close obs studio
                    if (Process.GetProcessesByName("obs64").Length > 0)
                    {
                        foreach (var process in Process.GetProcessesByName("obs64"))
                        {
                            process.Kill();
                            process.WaitForExit();
                        }
                    }

                    switch (gpu.VendorId)
                    {
                        case "10de":
                            (newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate(gpu);
                            var nvidiaActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
                            nvidiaActions = NvidiaHelper.DriverActions(gpu, newestDownloadUrl, progressButton);
                            await RunActions(progressButton, nvidiaActions);
                            break;
                        case "1002":
                            (newestVersion, newestDownloadUrl) = await AmdHelper.CheckUpdate(gpu);
                            var amdActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
                            amdActions = AmdHelper.DriverActions(gpu, newestDownloadUrl, progressButton);
                            await RunActions(progressButton, amdActions);
                            break;
                        case "8086":
                            (newestVersion, newestDownloadUrl) = await IntelHelper.CheckUpdate(gpu);
                            var intelActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
                            intelActions = IntelHelper.DriverActions(gpu, newestDownloadUrl, progressButton);
                            await RunActions(progressButton, intelActions);
                            break;
                    }

                    // apply profile
                    if (localSettings.Values["MsiProfile"] != null)
                    {
                        await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());
                    }

                    // launch obs studio
                    if (!(localSettings.Values["OBS"] as int? == 0))
                    {
                        await Task.Run(() => Process.Start(new ProcessStartInfo("cmd.exe") { Arguments = @"/c del ""%APPDATA%\obs-studio\.sentinel"" /s /f /q", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false })?.WaitForExit());
                        await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe", Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = @"C:\Program Files\obs-studio\bin\64bit" }));
                    }
                }
                else
                {
                    progressButton.IsChecked = false;

                    var dialog = new ContentDialog
                    {
                        Title = "Services & Drivers are disabled",
                        Content = "Please enable Services & Drivers before installing.",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = App.MainWindow.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }

                progressButton.IsChecked = false;
                return;
            }

            // update check logic
            progressButton.CheckedContent = gpu.IsInstalled ? "Checking for updates..." : "Checking for latest version...";

            switch (gpu.VendorId)
            {
                case "10de":
                    (newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate(gpu);
                    break;
                case "1002":
                    (newestVersion, newestDownloadUrl) = await AmdHelper.CheckUpdate(gpu);
                    break;
                case "8086":
                    (newestVersion, newestDownloadUrl) = await IntelHelper.CheckUpdate(gpu);
                    break;
            }

            if (gpu.IsInstalled)
            {
                await Task.Delay(500);

                var currentVersion = gpu.CurrentVersion.Replace("Current Version: ", "", StringComparison.OrdinalIgnoreCase);

                if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
                {
                    progressButton.Content = $"Update to {newestVersion}";
                }
                else
                {
                    progressButton.Content = "No updates available";
                }
            }
            else if (!gpu.IsInstalled)
            {
                await Task.Delay(500);

                progressButton.Content = $"Install {newestVersion}";
            }
        }
        catch
        {
            progressButton.Content = "Failed to check for updates";
        }
        finally
        {
            progressButton.IsChecked = false;
            progressButton.IsHitTestVisible = true;
        }
    }

    public static async Task RunActions(ProgressButton progressButton, List<(string Title, Func<Task> Action, Func<bool> Condition)> actions)
    {
        if (actions == null || actions.Count == 0) return;

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition()).ToList();
        if (filteredActions.Count == 0) return;

        List<Func<Task>> currentGroup = [];
        string previousTitle = string.Empty;

        foreach (var (title, action, _) in filteredActions)
        {
            if (!string.IsNullOrEmpty(previousTitle) && previousTitle != title && currentGroup.Count > 0)
            {
                progressButton.CheckedContent = previousTitle;

                foreach (var groupedAction in currentGroup)
                {
                    await groupedAction();
                }

                currentGroup.Clear();
                await Task.Delay(250);
            }

            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            progressButton.CheckedContent = previousTitle;
            foreach (var groupedAction in currentGroup)
            {
                await groupedAction();
            }
            await Task.Delay(250);
        }

        progressButton.IsHitTestVisible = true;
        progressButton.IsChecked = false;
        progressButton.Content = "Checking for updates";
        progressButton.IsChecked = true;
    }

    internal static Dictionary<string, DeviceInfo> SaveAffinity()
    {
        var deviceConfig = new Dictionary<string, DeviceInfo>();
        var gpuDevices = DeviceHelper.GetDevices(DeviceType.GPU);

        foreach (var device in gpuDevices)
        {
            string deviceKey = !string.IsNullOrEmpty(device.PnpDeviceId) ? device.PnpDeviceId : device.DevObjName ?? string.Empty;
            deviceConfig[deviceKey] = device;
        }

        return deviceConfig;
    }

    internal static async Task ReapplyAffinity(Dictionary<string, DeviceInfo> deviceConfig, ProgressButton progressButton)
    {
        if (deviceConfig == null || deviceConfig.Count == 0)
            return;

        progressButton.CheckedContent = "Reapplying GPU Affinity...";
        await Task.Delay(500);

        var changedDevices = new List<DeviceInfo>();
        var gpuDevicesAfterUpdate = DeviceHelper.GetDevices(DeviceType.GPU);

        foreach (var device in gpuDevicesAfterUpdate)
        {
            string deviceKey = !string.IsNullOrEmpty(device.PnpDeviceId) ? device.PnpDeviceId : device.DevObjName ?? string.Empty;

            if (!string.IsNullOrEmpty(deviceKey) && deviceConfig.TryGetValue(deviceKey, out var savedDevice))
            {
                bool settingsChanged = device.DevicePolicy != savedDevice.DevicePolicy || device.DevicePriority != savedDevice.DevicePriority || device.AssignmentSetOverride != savedDevice.AssignmentSetOverride;

                if (settingsChanged)
                {
                    DeviceHelper.SetAffinityPolicy(device.PnpDeviceId, savedDevice.DevicePolicy, savedDevice.DevicePriority, savedDevice.AssignmentSetOverride);
                    changedDevices.Add(device);
                }
            }
        }

        if (changedDevices.Count > 0)
        {
            foreach (var device in changedDevices)
            {
                await Task.Run(() => DeviceHelper.RestartDevice(device));
            }
        }

        progressButton.CheckedContent = null;
    }

    private async void HDCP_Toggled(object sender, RoutedEventArgs e)
    {
        // return if still initializing
        if (isInitializingHDCPState) return;

        ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
        GpuInfo gpu = (GpuInfo)toggleSwitch.DataContext;
        var GpuInfo = FindParent<StackPanel>(toggleSwitch).FindName("GpuInfo") as StackPanel;
        if (!gpu.IsInstalled)
        {
            localSettings.Values[$"HDCP_{gpu.PnPDeviceId}"] = toggleSwitch.IsOn;
            return;
        }

        // disable hittestvisible to avoid double-clicking
        toggleSwitch.IsHitTestVisible = false;

        // remove infobar
        GpuInfo.Children.Clear();

        // add infobar
        GpuInfo.Children.Add(new InfoBar
        {
            Title = toggleSwitch.IsOn ? "Enabling High-Bandwidth Digital Content Protection (HDCP)..." : "Disabling High-Bandwidth Digital Content Protection (HDCP)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational
        });

        // toggle hdcp
        if (gpu.VendorId == "10de")
        {
            if (toggleSwitch.IsOn)
            {
                Registry.SetValue(gpu.RegistryPath, "RMHdcpKeyglobZero", 0, RegistryValueKind.DWord);
                using var key = Registry.LocalMachine.OpenSubKey(gpu.RegistryPath["HKEY_LOCAL_MACHINE\\".Length..], writable: true);
                key?.DeleteValue("RmDisableHdcp22", false);
                key?.DeleteValue("RmSkipHdcp22Init", false);
            }
            else
            {
                Registry.SetValue(gpu.RegistryPath, "RMHdcpKeyglobZero", 1, RegistryValueKind.DWord);
                Registry.SetValue(gpu.RegistryPath, "RmDisableHdcp22", 1, RegistryValueKind.DWord);
                Registry.SetValue(gpu.RegistryPath, "RmSkipHdcp22Init", 1, RegistryValueKind.DWord);
            }
        }

        // close obs studio
        if (Process.GetProcessesByName("obs64").Length > 0)
        {
            foreach (var process in Process.GetProcessesByName("obs64"))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        // delay
        await Task.Delay(400);

        // restart driver
        await Task.Run(() => Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe")) { Arguments = "/q" })?.WaitForExit());

        // apply profile
        if (localSettings.Values["MsiProfile"] != null)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());
        }

        // launch obs studio
        if (!(localSettings.Values["OBS"] as int? == 0))
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo("cmd.exe") { Arguments = @"/c del ""%APPDATA%\obs-studio\.sentinel"" /s /f /q", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false })?.WaitForExit());
            await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe", Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = @"C:\Program Files\obs-studio\bin\64bit" }));
        }

        // re-enable hittestvisible
        toggleSwitch.IsHitTestVisible = true;

        // remove infobar
        GpuInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = toggleSwitch.IsOn ? "Successfully enabled High-Bandwidth Digital Content Protection (HDCP)." : "Successfully disabled High-Bandwidth Digital Content Protection (HDCP).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success
        };
        GpuInfo.Children.Add(infoBar);

        // delay
        await Task.Delay(2000);

        // remove infobar
        GpuInfo.Children.Clear();
    }

    private async void HDMIDPAudio_Toggled(object sender, RoutedEventArgs e)
    {
        // return if still initializing
        if (isInitializingHDMIDPAudioState) return;

        ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
        GpuInfo gpu = (GpuInfo)toggleSwitch.DataContext;
        var GpuInfo = FindParent<StackPanel>(toggleSwitch).FindName("GpuInfo") as StackPanel;
        if (!gpu.IsInstalled)
        {
            localSettings.Values[$"HDMIDPAudio_{gpu.PnPDeviceId}"] = toggleSwitch.IsOn;
            return;
        }

        // disable hittestvisible to avoid double-clicking
        toggleSwitch.IsHitTestVisible = false;

        // remove infobar
        GpuInfo.Children.Clear();

        // add infobar
        GpuInfo.Children.Add(new InfoBar
        {
            Title = toggleSwitch.IsOn ? "Enabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio..." : "Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational
        });

        // toggle hdmi/dp audio
        GpuHelper.ToggleHdmiDpAudio(gpu, toggleSwitch.IsOn);
        
        // delay
        await Task.Delay(500);

        // re-enable hittestvisible
        toggleSwitch.IsHitTestVisible = true;

        // remove infobar
        GpuInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = toggleSwitch.IsOn ? "Successfully enabled High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio." : "Successfully disabled High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success
        };
        GpuInfo.Children.Add(infoBar);

        // delay
        await Task.Delay(2000);

        // remove infobar
        GpuInfo.Children.Clear();
    }

    private async void BrowseMsi_Click(object sender, RoutedEventArgs e)
    {
        // disable the button to avoid double-clicking
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;

        // remove infobar
        MsiAfterburnerInfo.Children.Clear();

        // add infobar
        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Please select a MSI Afterburner profile (.cfg).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational
        });

        // delay
        await Task.Delay(300);

        // launch file picker
        var picker = new FilePicker(App.MainWindow)
        {
            ShowAllFilesOption = false
        };
        picker.FileTypeChoices.Add("MSI Afterburner profile", ["*.cfg"]);
        var file = await picker.PickSingleFileAsync();

        if (file?.Path != null)
        {
            string fileContent = await FileIO.ReadTextAsync(file);

            if (fileContent.Contains("[Startup]"))
            {
                if (file.Path.Contains(@"C:\Program Files (x86)\MSI Afterburner\Profiles", StringComparison.OrdinalIgnoreCase))
                {
                    // re-enable the button
                    senderButton.IsEnabled = true;

                    // remove infobar
                    MsiAfterburnerInfo.Children.Clear();

                    // add infobar
                    MsiAfterburnerInfo.Children.Add(new InfoBar
                    {
                        Title = "The selected MSI Afterburner profile is already imported.",
                        IsClosable = false,
                        IsOpen = true,
                        Severity = InfoBarSeverity.Error
                    });

                    // delay
                    await Task.Delay(2000);

                    // remove infobar
                    MsiAfterburnerInfo.Children.Clear();
                    return;
                }

                // re-enable the button
                senderButton.IsEnabled = true;

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // add infobar
                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "Applying the MSI Afterburner profile...",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational
                });

                // delay
                await Task.Delay(300);

                // delete old profiles
                Directory.GetFiles(@"C:\Program Files (x86)\MSI Afterburner\Profiles")
                .Where(file => Path.GetFileName(file) != "MSIAfterburner.cfg")
                .ToList()
                .ForEach(File.Delete);

                // import profile
                File.Copy(file.Path, Path.Combine(@"C:\Program Files (x86)\MSI Afterburner\Profiles", file.Name), true);

                // apply profile
                await Task.Run(() => Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe") { Arguments = "/Profile1 /q" })?.WaitForExit());

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // add infobar
                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "Successfully applied the MSI Afterburner profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();
            }
            else
            {
                // re-enable the button
                senderButton.IsEnabled = true;

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();

                // add infobar
                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "The selected file is not a valid MSI Afterburner profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();
                return;
            }
        }
        else
        {
            // re-enable the button
            senderButton.IsEnabled = true;

            // remove infobar
            MsiAfterburnerInfo.Children.Clear();
        }
    }

    private async void LaunchMsi_Click(object sender, RoutedEventArgs e)
    {
        // remove infobar
        MsiAfterburnerInfo.Children.Clear();

        // add infobar
        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Launching MSI Afterburner...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational
        });

        // launch
        await Task.Run(() => Process.Start(@"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe")?.WaitForInputIdle());

        // remove infobar
        MsiAfterburnerInfo.Children.Clear();

        // add infobar
        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Successfully launched MSI Afterburner.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        MsiAfterburnerInfo.Children.Clear();
    }

    private void GetOBSState()
    {
        if (!localSettings.Values.TryGetValue("OBS", out object value))
        {
            localSettings.Values["OBS"] = 0;
        }
        else
        {
            OBS.IsOn = (int)value == 1;
        }

        isInitializingOBSState = false;
    }

    private async void OBS_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingOBSState) return;

        // disable hittestvisible to avoid double-clicking
        OBS.IsHitTestVisible = false;

        // remove infobar
        ObsStudioInfo.Children.Clear();

        // add infobar
        ObsStudioInfo.Children.Add(new InfoBar
        {
            Title = OBS.IsOn ? "Launching OBS Studio..." : "Closing OBS Studio...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -28, 4, 36)
        });

        localSettings.Values["OBS"] = OBS.IsOn ? 1 : 0;

        if (OBS.IsOn)
        {
            // launch obs studio
            await Task.Run(() => Process.Start(new ProcessStartInfo("cmd.exe") { Arguments = @"/c del ""%APPDATA%\obs-studio\.sentinel"" /s /f /q", CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false })?.WaitForExit());
            await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe", Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = @"C:\Program Files\obs-studio\bin\64bit" }));

            // delay
            await Task.Delay(1500);
        }
        else
        {
            // close obs studio
            if (Process.GetProcessesByName("obs64").Length > 0)
            {
                foreach (var process in Process.GetProcessesByName("obs64"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }

            // delay
            await Task.Delay(400);
        }

        // re-enable hittestvisible
        OBS.IsHitTestVisible = true;

        // remove infobar
        ObsStudioInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = OBS.IsOn ? "Successfully launched OBS Studio." : "Successfully closed OBS Studio.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(4, -28, 4, 36)
        };
        ObsStudioInfo.Children.Add(infoBar);

        // delay
        await Task.Delay(2000);

        // remove infobar
        ObsStudioInfo.Children.Clear();
    }

    public static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);

        while (parent != null && parent is not T)
            parent = VisualTreeHelper.GetParent(parent);

        return parent as T;
    }
}