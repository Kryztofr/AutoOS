using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class DeviceStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);

        InstallPage.Status.Text = "Configuring Devices...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // properties -> policies -> write-caching policy
            (@"Enabling ""Enable write caching on the device""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg add ""%a\Device Parameters\Disk"" /v ""UserWriteCacheSetting"" /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Windows write-cache buffer flushing on the device""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg add ""%a\Device Parameters\Disk"" /v ""CacheIsPowerProtected"" /t REG_DWORD /d 1 /f"), null),

            // disable drive powersaving features
            ("Disabling drive powersaving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (EnableHIPM EnableDIPM EnableHDDParking) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling drive powersaving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %%s in ('reg query ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Enum"" /S /F ""StorPort"" ^| findstr /e ""StorPort""') do reg add ""%%s"" /v ""EnableIdlePowerManagement"" /t REG_DWORD /d ""0"" /f"), null),
            ("Disabling drive powersaving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (EnhancedPowerManagementEnabled AllowIdleIrpInD3 EnableSelectiveSuspend DeviceSelectiveSuspended SelectiveSuspendEnabled SelectiveSuspendOn EnumerationRetryCount ExtPropDescSemaphore WaitWakeEnabled D3ColdSupported WdfDirectedPowerTransitionEnable EnableIdlePowerManagement IdleInWorkingState IdleTimeoutInMS MinimumIdleTimeoutInMS WakeEnabled) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling drive powersaving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (DisableIdlePowerManagement DisableRuntimePowerManagement) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 1 /f"), null),

            // disable dma remapping
            ("Disabling DMA remapping", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for %a in (DmaRemappingCompatible) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f"), null),

            // disable device power management
            ("Disabling device power management", async () => await ProcessActions.RunPowerShellScript("devicepowermanagement.ps1", ""), null),

            // enable msi mode for supported devices
            ("Enabling MSI mode for supported devices", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\PCI"" ^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i"" ^| findstr ""HKEY""') do @for /f ""tokens=3"" %v in ('reg query ""%a\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v MSISupported 2^>nul ^| findstr MSISupported') do @if ""%v""==""0x0"" reg add ""%a\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v MSISupported /t REG_DWORD /d 1 /f"), null),

            // set msi mode to undefined for all devices
            ("Setting MSI mode to undefined for all devices", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\PCI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg delete ""%a\Device Parameters\Interrupt Management\Affinity Policy"" /v ""DevicePriority"" /f"), null),

            // disable asmedia usb controllers
            ("Disabling ASMedia USB controllers", async () => await ProcessActions.RunPowerShell(@"Get-PnpDevice -FriendlyName ""*ASMedia USB*"" | Disable-PnpDevice -Confirm:$false"), null),

            // save xhci interrupt moderation (imod) data
            ("Saving XHCI Interrupt Moderation (IMOD) data", async () => await ProcessActions.RunPowerShellScript("imod.ps1", $"-save \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Chiptool", "chiptool.exe")}\""), null),

            // disable xhci interrupt moderation (imod)
            ("Disabling XHCI Interrupt Moderation (IMOD)", async () => await ProcessActions.RunPowerShellScript("imod.ps1", $"-disable \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Chiptool", "chiptool.exe")}\""), null),
            
            // disable reserved storage
            ("Disabling reserved storage", async () => await ProcessActions.RunPowerShell(@"DISM /Online /Set-ReservedStorageState /State:Disabled"), null),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        int groupedTitleCount = 0;

        List<Func<Task>> currentGroup = [];

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        InstallPage.Info.Title += ": " + ex.Message;
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Error);
                        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;
                        await ProcessActions.LogError(ex);

                        var tcs = new TaskCompletionSource<bool>();

                        InstallPage.ResumeButton.Click += (sender, e) =>
                        {
                            tcs.TrySetResult(true);
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                            TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
                            InstallPage.ProgressRingControl.Foreground = null;
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                        };

                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title += ": " + ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Error);
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;
                    await ProcessActions.LogError(ex);

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
            TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
        if (filteredActions.Count == 0)
        {
            InstallPage.Progress.Value += stagePercentage;
            TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
    }
}