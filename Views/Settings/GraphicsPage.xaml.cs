using AutoOS.Helpers;
using AutoOS.Views.Installer.Actions;
using AutoOS.Views.Settings.Scheduling.Models;
using AutoOS.Views.Settings.Scheduling.Services;
using Downloader;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class GraphicsPage : Page
{
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    private bool isInitializingHDCPState = true;
    //private bool isInitializingHDMIDPAudioState = true;
    private bool isInitializingOBSState = true;
    private Dictionary<string, (DeviceSettings settings, string devObjName)> deviceConfig = new Dictionary<string, (DeviceSettings, string)>();

    public GraphicsPage()
    {
        InitializeComponent();
        LoadGpus();
        GetHDCPState();
        //GetHDMIDPAudioState();
        GetOBSState();
    }

    private async void LoadGpus()
    {
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
        {
            foreach (var obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString();
                string version = obj["DriverVersion"]?.ToString();

                if (name != null)
                {
                    if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                    {
                        Nvidia_SettingsGroup.Visibility = Visibility.Visible;
                        Nvidia_SettingsGroup.Header = name;
                        Nvidia_SettingsGroup.Description = "Current Version: " + (await Task.Run(() => Process.Start(new ProcessStartInfo("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader") { CreateNoWindow = true, RedirectStandardOutput = true })?.StandardOutput.ReadToEndAsync()))?.Trim();
                        NvidiaUpdateCheck.IsChecked = true;
                    }
                    if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                    {
                        Amd_SettingsGroup.Visibility = Visibility.Visible;
                        Amd_SettingsGroup.Header = name;
                        Amd_SettingsGroup.Description = $"Current Version: {AmdHelper.GetCurrentVersion()}";
                        AmdUpdateCheck.IsChecked = true;
                    }
                    if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    {
                        Intel_SettingsGroup.Visibility = Visibility.Visible;
                        Intel_SettingsGroup.Header = name;
                        Intel_SettingsGroup.Description = "Current Version: " + (version?.Split('.')[2] + "." + version?.Split('.')[3]);
                        IntelUpdateCheck.IsChecked = true;
                    }
                }
            }
        }
    }

    private async void NvidiaUpdateCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (NvidiaUpdateCheck.Content.ToString().Contains("Update to"))
        {
            NvidiaUpdateCheck.CheckedContent = NvidiaUpdateCheck.Content.ToString();

            if (new ServiceController("Beep").Status == ServiceControllerStatus.Running)
            {
                var (_, newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate();

                NvidiaUpdateCheck.IsHitTestVisible = false;

                // download the nvidia driver   
                NvidiaUpdateCheck.CheckedContent = "Downloading the NVIDIA driver...";
                await RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.exe");

                // extract the driver
                NvidiaUpdateCheck.CheckedContent = "Extracting the NVIDIA driver...";
                await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver"));

                // strip the driver
                NvidiaUpdateCheck.CheckedContent = "Stripping the NVIDIA driver...";
                await ProcessActions.RunNvidiaStrip();

                // close obs studio
                if (Process.GetProcessesByName("obs64").Length > 0)
                {
                    foreach (var process in Process.GetProcessesByName("obs64"))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }

                // save config
                var gpuDevices = DeviceDetectionService.FindDevicesByType(DeviceType.GPU);
                IntPtr deviceInfoSetHandle = IntPtr.Zero;
                foreach (var device in gpuDevices)
                {
                    if (deviceInfoSetHandle == IntPtr.Zero)
                        deviceInfoSetHandle = device.DeviceInfoSet;

                    if (device.RegistryKey != null)
                    {
                        string deviceKey = !string.IsNullOrEmpty(device.PnpDeviceId) ? device.PnpDeviceId : device.DevObjName ?? string.Empty;
                        
                        var settings = RegistryService.ReadDeviceSettings(device.RegistryKey, device.MaxMSILimit);
                        deviceConfig[deviceKey] = (settings, device.DevObjName ?? string.Empty);
                    }
                }

                foreach (var device in gpuDevices)
                    device.RegistryKey?.Close();
                
                if (deviceInfoSetHandle != IntPtr.Zero && deviceInfoSetHandle != new IntPtr(-1))
                    SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSetHandle);

                // update driver
                NvidiaUpdateCheck.CheckedContent = "Updating the NVIDIA driver...";
                await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\setup.exe"" /s /clean");
                await ProcessActions.Sleep(3000);
                Nvidia_SettingsGroup.Description = "Current Version: " + (await Task.Run(() => Process.Start(new ProcessStartInfo("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader") { CreateNoWindow = true, RedirectStandardOutput = true })?.StandardOutput.ReadToEndAsync()))?.Trim();

                // disable the nvidia tray icon
                NvidiaUpdateCheck.CheckedContent = "Disabling the NVIDIA tray icon...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\NvTray"" /v StartOnLogin /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""HideXGpuTrayIcon"" /t REG_DWORD /d 1 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager"" /v ""ShowTrayIcon"" /t REG_DWORD /d 0 /f");

                // disable the dlss indicator
                NvidiaUpdateCheck.CheckedContent = "Disabling the DLSS Indicator...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\NGXCore"" /v ""ShowDlssIndicator"" /t REG_DWORD /d 0 /f");

                // disable automatic updates
                NvidiaUpdateCheck.CheckedContent = "Disabling automatic updates...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager"" /v AutoDownload /t REG_DWORD /d 0 /f");

                // disable telemetry
                NvidiaUpdateCheck.CheckedContent = "Disabling telemetry...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Nvidia Corporation\NvControlPanel2\Client"" /v ""OptInOrOutPreference"" /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup"" /v ""SendTelemetryData"" /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID44231 /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID64640 /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID66610 /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c cd /d ""C:\Windows\System32\DriverStore\FileRepository\"" & dir NvTelemetry64.dll /a /b /s & del NvTelemetry64.dll /a /s");

                // disable logging
                NvidiaUpdateCheck.CheckedContent = "Disabling logging...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogDisableMasks /t REG_BINARY /d ""00ffff0f01ffff0f02ffff0f03ffff0f04ffff0f05ffff0f06ffff0f07ffff0f08ffff0f09ffff0f0affff0f0bffff0f0cffff0f0dffff0f0effff0f0fffff0f10ffff0f11ffff0f12ffff0f13ffff0f14ffff0f15ffff0f16ffff0f00ffff1f01ffff1f02ffff1f03ffff1f04ffff1f05ffff1f06ffff1f07ffff1f08ffff1f09ffff1f0affff1f0bffff1f0cffff1f0dffff1f0effff1f0fffff1f00ffff2f01ffff2f02ffff2f03ffff2f04ffff2f05ffff2f06ffff2f07ffff2f08ffff2f09ffff2f0affff2f0bffff2f0cffff2f0dffff2f0effff2f0fffff2f00ffff3f01ffff3f02ffff3f03ffff3f04ffff3f05ffff3f06ffff3f07ffff3f"" /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogWarningEntries /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogPagingEntries /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogEventEntries /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogErrorEntries /t REG_DWORD /d 0 /f");

                // use the advanced 3d image settings
                NvidiaUpdateCheck.CheckedContent = "Using the advanced 3D image settings...";
                await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\NVIDIA Corporation\Global\NVTweak"" /v ""Gestalt"" /t REG_DWORD /d 515 /f");

                // import the optimized nvidia profile
                NvidiaUpdateCheck.CheckedContent = "Importing the optimized NVIDIA profile...";
                await ProcessActions.ImportProfile("BaseProfile.nip");

                // configure physx to use gpu
                NvidiaUpdateCheck.CheckedContent = "Configuring PhysX to use GPU...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""NvCplPhysxAuto"" /t REG_DWORD /d 0 /f");

                // configure color settings
                NvidiaUpdateCheck.CheckedContent = "Configuring color settings...";
                await ProcessActions.RunPowerShellScript("colorsettings.ps1", "");
                await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""delims="" %a in ('reg query HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase') do reg add ""%a"" /v ""ColorformatConfig"" /t REG_BINARY /d ""DB02000014000000000A00080000000003010000"" /f");

                // disable high-definition-content-protection (hdcp)
                if (!HDCP.IsOn)
                {
                    NvidiaUpdateCheck.CheckedContent = "Disabling high-definition-content-protection (HDCP)...";
                    await ProcessActions.RunPowerShellScript("hdcp.ps1", "");
                }

                // disable error code correction (ecc)
                NvidiaUpdateCheck.CheckedContent = "Disabling error code correction (ECC)...";
                await ProcessActions.RunPowerShellScript("ecc.ps1", "");

                // configure miscellaneous nvidia settings
                NvidiaUpdateCheck.CheckedContent = "Configuring miscellaneous NVIDIA settings...";
                await ProcessActions.RunPowerShellScript("nvidiamisc.ps1", "");

                // disable scaling
                NvidiaUpdateCheck.CheckedContent = "Disabling scaling...";
                await ProcessActions.RunNsudo("CurrentUser", @"cmd /c for %i in (Scaling) do for /f ""tokens=*"" %a in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /s /f ""%i""^| findstr ""HKEY""') do reg add ""%a"" /v ""Scaling"" /t REG_DWORD /d 1 /f");

                // disable dynamic p-state
                NvidiaUpdateCheck.CheckedContent = "Disabling dynamic P-State...";
                await ProcessActions.RunPowerShellScript("pstate.ps1", "");

                // disable display power savings
                NvidiaUpdateCheck.CheckedContent = "Disabling Display Power Savings...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""DisplayPowerSaving"" /t REG_DWORD /d 0 /f");
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\NVIDIA Corporation\Global\NVTweak"" /v ""DisplayPowerSaving"" /t REG_DWORD /d 0 /f");

                // disable hd audio power savings
                NvidiaUpdateCheck.CheckedContent = "Disabling HD Audio Power Savings...";
                await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm"" /v ""EnableHDAudioD3Cold"" /t REG_DWORD /d 0 /f");

                // reapply config
                if (deviceConfig.Count > 0)
                {
                    NvidiaUpdateCheck.CheckedContent = "Reapplying Affinity...";
                    var changedDevices = new List<DeviceInfo>();
                    var gpuDevicesAfterUpdate = DeviceDetectionService.FindDevicesByType(DeviceType.GPU);
                    IntPtr deviceInfoSetHandleAfterUpdate = IntPtr.Zero;
                    
                    foreach (var device in gpuDevicesAfterUpdate)
                    {
                        if (deviceInfoSetHandleAfterUpdate == IntPtr.Zero)
                            deviceInfoSetHandleAfterUpdate = device.DeviceInfoSet;

                        if (device.RegistryKey == null)
                            continue;

                        string deviceKey = !string.IsNullOrEmpty(device.PnpDeviceId) ? device.PnpDeviceId : device.DevObjName ?? string.Empty;

                        if (!string.IsNullOrEmpty(deviceKey) && deviceConfig.TryGetValue(deviceKey, out var savedData))
                        {
                            var savedSettings = savedData.settings;
                            var currentSettings = RegistryService.ReadDeviceSettings(device.RegistryKey, device.MaxMSILimit);
                            bool settingsChanged = currentSettings.DevicePolicy != savedSettings.DevicePolicy || currentSettings.DevicePriority != savedSettings.DevicePriority || currentSettings.AssignmentSetOverride != savedSettings.AssignmentSetOverride;
                            
                            if (settingsChanged)
                            {
                                RegistryService.SetAffinityPolicy(device.RegistryKey, savedSettings.DevicePolicy, savedSettings.DevicePriority, savedSettings.AssignmentSetOverride);
                                changedDevices.Add(device);
                            }
                        }
                    }

                    if (changedDevices.Count > 0)
                    {
                        await Task.Run(() =>
                        {
                            foreach (var device in changedDevices)
                            {
                                if (device.DeviceInfoSet != IntPtr.Zero)
                                {
                                    DeviceSettingsService.RestartDevice(device);
                                }
                            }
                        });
                    }

                    foreach (var device in gpuDevicesAfterUpdate)
                    {
                        device.RegistryKey?.Close();
                    }

                    if (deviceInfoSetHandleAfterUpdate != IntPtr.Zero && deviceInfoSetHandleAfterUpdate != new IntPtr(-1))
                        SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSetHandleAfterUpdate);
                    
                    deviceConfig.Clear();
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

                NvidiaUpdateCheck.IsHitTestVisible = true;
                NvidiaUpdateCheck.IsChecked = false;
                NvidiaUpdateCheck.Content = "Checking for updates";
                NvidiaUpdateCheck.IsChecked = true;
            }
            else
            {
                NvidiaUpdateCheck.IsChecked = false;

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
        }
        else
        {
            NvidiaUpdateCheck.CheckedContent = "Checking for updates...";

            try
            {
                var (currentVersion, newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate();

                // delay
                await Task.Delay(800);

                // check if update is needed
                if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
                {
                    NvidiaUpdateCheck.IsChecked = false;
                    NvidiaUpdateCheck.Content = "Update to " + newestVersion;
                }
                else if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) == 0)
                {
                    NvidiaUpdateCheck.IsChecked = false;
                    NvidiaUpdateCheck.Content = "No updates available";
                }
            }
            catch
            {
                // delay
                await Task.Delay(800);

                NvidiaUpdateCheck.IsChecked = false;
                NvidiaUpdateCheck.Content = "Failed to check for updates";
            }
        }
    }

    public async Task RunDownload(string url, string path, string file)
    {
        var uiContext = SynchronizationContext.Current;

        var download = DownloadBuilder.New()
            .WithUrl(url)
            .WithDirectory(path)
            .WithFileName(file)
            .WithConfiguration(new DownloadConfiguration())
            .Build();

        double speedMB = 0.0;
        double receivedMB = 0.0;
        double totalMB = 0.0;
        double percentage = 0.0;

        DateTime lastLoggedTime = DateTime.MinValue;

        download.DownloadProgressChanged += (sender, e) =>
        {
            if ((DateTime.Now - lastLoggedTime).TotalMilliseconds < 50) return;

            lastLoggedTime = DateTime.Now;

            speedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
            receivedMB = e.ReceivedBytesSize / (1024.0 * 1024.0);
            totalMB = e.TotalBytesToReceive / (1024.0 * 1024.0);
            percentage = e.ProgressPercentage;

            uiContext?.Post(_ =>
            {
                NvidiaUpdateCheck.IsIndeterminate = false;
                NvidiaUpdateCheck.Progress = percentage;
            }, null);
        };

        download.DownloadFileCompleted += (sender, e) =>
        {
            uiContext?.Post(_ =>
            {
                NvidiaUpdateCheck.Progress = 100;
                NvidiaUpdateCheck.IsIndeterminate = true;
            }, null);
        };

        await download.StartAsync();
    }
    private async void AmdUpdateCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (AmdUpdateCheck.Content.ToString().Contains("Update to"))
        {
            if (new ServiceController("Beep").Status == ServiceControllerStatus.Running)
            {
                var dialog = new ContentDialog
                {
                    Title = "Not implemented yet",
                    Content = "AMD Driver Update functionality has not been added yet.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = App.MainWindow.Content.XamlRoot
                };
                await dialog.ShowAsync();

                AmdUpdateCheck.IsHitTestVisible = true;
                AmdUpdateCheck.IsChecked = false;
                AmdUpdateCheck.Content = "Checking for updates";
                AmdUpdateCheck.IsChecked = true;
            }
            else
            {
                AmdUpdateCheck.IsChecked = false;

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
        }
        else
        {
            AmdUpdateCheck.CheckedContent = "Checking for updates...";

            try
            {
                var (currentVersion, newestVersion, newestDownloadUrl) = await AmdHelper.CheckUpdate();

                // delay
                await Task.Delay(800);

                // check if update is needed
                if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
                {
                    AmdUpdateCheck.IsChecked = false;
                    AmdUpdateCheck.Content = "Update to " + newestVersion;
                }
                else if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) == 0)
                {
                    AmdUpdateCheck.IsChecked = false;
                    AmdUpdateCheck.Content = "No updates available";
                }
            }
            catch
            {
                // delay
                await Task.Delay(800);

                AmdUpdateCheck.IsChecked = false;
                AmdUpdateCheck.Content = "Failed to check for updates";
            }
        }
    }

    private async void IntelUpdateCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (IntelUpdateCheck.Content.ToString().Contains("Update to"))
        {
            if (new ServiceController("Beep").Status == ServiceControllerStatus.Running)
            {
                var dialog = new ContentDialog
                {
                    Title = "Not implemented yet",
                    Content = "Intel Driver Update functionality has not been added yet.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = App.MainWindow.Content.XamlRoot
                };
                await dialog.ShowAsync();

                IntelUpdateCheck.IsHitTestVisible = true;
                IntelUpdateCheck.IsChecked = false;
                IntelUpdateCheck.Content = "Checking for updates";
                IntelUpdateCheck.IsChecked = true;
            }
            else
            {
                IntelUpdateCheck.IsChecked = false;

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
        }
        else
        {
            IntelUpdateCheck.CheckedContent = "Checking for updates...";

            try
            {
                string currentVersion = Intel_SettingsGroup.Description.ToString();
                string newestVersion = Intel_SettingsGroup.Description.ToString();

                // delay
                await Task.Delay(800);

                // check if update is needed
                if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) > 0)
                {
                    IntelUpdateCheck.IsChecked = false;
                    IntelUpdateCheck.Content = "Update to " + newestVersion;
                }
                else if (string.Compare(newestVersion, currentVersion, StringComparison.Ordinal) == 0)
                {
                    IntelUpdateCheck.IsChecked = false;
                    IntelUpdateCheck.Content = "No updates available";
                }
            }
            catch
            {
                // delay
                await Task.Delay(800);

                IntelUpdateCheck.IsChecked = false;
                IntelUpdateCheck.Content = "Failed to check for updates";
            }
        }
    }

    private void GetHDCPState()
    {
        // get registry values
        for (int i = 0; i <= 9; i++)
        {
            if (Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}", "ProviderName", null)?.ToString() == "NVIDIA" &&
                (int?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}", "RMHdcpKeyglobZero", null) == 0)
            {
                HDCP.IsOn = true;
            }
        }
        isInitializingHDCPState = false;
    }

    private async void HDCP_Toggled(object sender, RoutedEventArgs e)
    {
        // return if still initializing
        if (isInitializingHDCPState) return;

        // disable hittestvisible to avoid double-clicking
        HDCP.IsHitTestVisible = false;

        // remove infobar
        GpuInfo.Children.Clear();

        // add infobar
        GpuInfo.Children.Add(new InfoBar
        {
            Title = HDCP.IsOn ? "Enabling High-Bandwidth Digital Content Protection (HDCP)..." : "Disabling High-Bandwidth Digital Content Protection (HDCP)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -4, 4, 12)
        });

        // toggle hdcp
        for (int i = 0; i <= 9; i++)
        {
            var path = $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}";
            if (Registry.GetValue(path, "ProviderName", null)?.ToString() == "NVIDIA")
            {
                if (HDCP.IsOn)
                {
                    Registry.SetValue(path, "RMHdcpKeyglobZero", 0, RegistryValueKind.DWord);
                    using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{i}", true);
                    key?.DeleteValue("RmDisableHdcp22", false);
                    key?.DeleteValue("RmSkipHdcp22Init", false);
                }
                else
                {
                    Registry.SetValue(path, "RMHdcpKeyglobZero", 1, RegistryValueKind.DWord);
                    Registry.SetValue(path, "RmDisableHdcp22", 1, RegistryValueKind.DWord);
                    Registry.SetValue(path, "RmSkipHdcp22Init", 1, RegistryValueKind.DWord);
                }
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
        HDCP.IsHitTestVisible = true;

        // remove infobar
        GpuInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = HDCP.IsOn ? "Successfully enabled High-Bandwidth Digital Content Protection (HDCP)." : "Successfully disabled High-Bandwidth Digital Content Protection (HDCP).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(4, -4, 4, 12)
        };
        GpuInfo.Children.Add(infoBar);

        // delay
        await Task.Delay(2000);

        // remove infobar
        GpuInfo.Children.Clear();
    }

    //private async void GetHDMIDPAudioState()
    //{
    //    var toggles = new[] { NVIDIA_HDMIDPAudio, AMD_HDMIDPAudio};

    //    foreach (var toggle in toggles)
    //    {
    //        toggle.IsOn = await Task.Run(() =>
    //        {
    //            return new ManagementObjectSearcher(
    //                "SELECT * FROM Win32_PnPEntity WHERE Description = 'High Definition Audio Device'")
    //                   .Get()
    //                   .Cast<ManagementObject>()
    //                   .Any(device => device["Status"]?.ToString() == "OK");
    //        });
    //    }

    //    INTEL_HDMIDPAudio.IsOn = await Task.Run(() =>
    //    {
    //        return new ManagementObjectSearcher(
    //            "SELECT * FROM Win32_PnPEntity WHERE Description = 'Intel(R) Display Audio'")
    //               .Get()
    //               .Cast<ManagementObject>()
    //               .Any(device => device["Status"]?.ToString() == "OK");
    //    });

    //    isInitializingHDMIDPAudioState = false;
    //}

    //private async void HDMIDPAudio_Toggled(object sender, RoutedEventArgs e)
    //{
    //    // return if still initializing
    //    if (isInitializingHDMIDPAudioState) return;

    //    var toggle = sender as ToggleSwitch;

    //    // remove infobar
    //    GpuInfo.Children.Clear();

    //    // add infobar
    //    GpuInfo.Children.Add(new InfoBar
    //    {
    //        Title = toggle.IsOn ? "Enabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio..." : "Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio...",
    //        IsClosable = false,
    //        IsOpen = true,
    //        Severity = InfoBarSeverity.Informational,
    //        Margin = new Thickness(5)
    //    });

    //    // toggle hdmi/dp audio
    //    bool isOn = toggle.IsOn;

    //    if (toggle == NVIDIA_HDMIDPAudio || toggle == AMD_HDMIDPAudio)
    //    {
    //        await Task.Run(() =>
    //        {
    //            var process = new Process
    //            {
    //                StartInfo = new ProcessStartInfo
    //                {
    //                    FileName = "powershell.exe",
    //                    Arguments = @$"-ExecutionPolicy Bypass -Command ""Get-PnpDevice | Where-Object {{ $_.FriendlyName -eq 'High Definition Audio Device' }} | {(isOn ? "Enable" : "Disable")}-PnpDevice -Confirm:$false""",
    //                    CreateNoWindow = true
    //                }
    //            };

    //            process.Start();
    //            process.WaitForExit();
    //        });
    //    }
    //    else if (toggle == INTEL_HDMIDPAudio)
    //    {
    //        await Task.Run(() =>
    //        {
    //            var process = new Process
    //            {
    //                StartInfo = new ProcessStartInfo
    //                {
    //                    FileName = "powershell.exe",
    //                    Arguments = @$"-ExecutionPolicy Bypass -Command ""Get-PnpDevice | Where-Object {{ $_.FriendlyName -eq 'Intel(R) Display Audio' }} | {(isOn ? "Enable" : "Disable")}-PnpDevice -Confirm:$false""",
    //                    CreateNoWindow = true
    //                }
    //            };

    //            process.Start();
    //            process.WaitForExit();
    //        });
    //    }

    //    // delay
    //    await Task.Delay(400);

    //    // remove infobar
    //    GpuInfo.Children.Clear();

    //    // add infobar
    //    var infoBar = new InfoBar
    //    {
    //        Title = toggle.IsOn ? "Successfully enabled High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio." : "Successfully disabled High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio.",
    //        IsClosable = false,
    //        IsOpen = true,
    //        Severity = InfoBarSeverity.Success,
    //        Margin = new Thickness(5)
    //    };
    //    GpuInfo.Children.Add(infoBar);

    //    // delay
    //    await Task.Delay(2000);

    //    // remove infobar
    //    GpuInfo.Children.Clear();
    //}

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
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -4, 4, 12)
        });

        // delay
        await Task.Delay(300);

        // launch file picker
        var picker = new FilePicker(App.MainWindow)
        {
            ShowAllFilesOption = false
        };
        picker.FileTypeChoices.Add("MSI Afterburner profile", new List<string> { "*.cfg" });
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            string fileContent = await FileIO.ReadTextAsync(file);

            if (fileContent.Contains("[Startup]"))
            {
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
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(4, -4, 4, 12)
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
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(4, -4, 4, 12)
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
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(4, -4, 4, 12)
                });

                // delay
                await Task.Delay(2000);

                // remove infobar
                MsiAfterburnerInfo.Children.Clear();
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
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -4, 4, 12)
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
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(4, -4, 4, 12)
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
}