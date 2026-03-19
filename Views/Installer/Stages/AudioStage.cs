using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Registry;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using AutoOS.Helpers.Device;
using Microsoft.Win32;
using Windows.Storage;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class AudioStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool NetAdapterCx = PreparingStage.NetAdapterCx;

        InstallPage.Status.Text = "Configuring Audio Devices...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable system beeps
            ("Disabling system beeps", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\Sound", "Beep", "no", RegistryValueKind.String), null),

            // sound -> communications
            (@"Setting ""When Windows detects communication activity"" to ""Do nothing""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Multimedia\Audio", "UserDuckingPreference", 3, RegistryValueKind.DWord), null),

            // disable audio enhancements
            ("Disabling audio enhancements", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }""") { CreateNoWindow = true }), null),
            ("Disabling audio enhancements", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }""") { CreateNoWindow = true }), null),

            // disable audio idle states
            ("Disabling audio idle states", async () => DeviceHelper.GetDevices(DeviceType.HDAUD).Select(device => Registry.LocalMachine.OpenSubKey($@"{device.RegistryPath}\PowerSettings", true)).Where(key => key != null).ToList().ForEach(key => { key.SetValue("PerformanceIdleTime", new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, RegistryValueKind.Binary); key.Dispose(); }), null),

            // optimize multimedia class scheduler service (mmcss)
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NoLazyMode", 0, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "LazyModeTimeout", unchecked((int)4294967295), RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SchedulerPeriod", 1000000, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "IdleDetectionCycles", 1, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SchedulerTimerResolution", 1, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Priority", 1, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Scheduling Category", "High", RegistryValueKind.String), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Priority When Yielded", 19, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio", "Priority", 1, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio", "Scheduling Category", "High", RegistryValueKind.String), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio", "Priority When Yielded", 19, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback", "Priority", 1, RegistryValueKind.DWord), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback", "Scheduling Category", "High", RegistryValueKind.String), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback", "Priority When Yielded", 19, RegistryValueKind.DWord), null),

            // disable multimedia class scheduler service (mmcss)
            ("Disabling Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MMCSS", "Start", 4, RegistryValueKind.DWord), () => NetAdapterCx == true),

            // download dolby ac-3 feature on demand
            ("Downloading Dolby AC-3 Feature on Demand", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/g7qcrrpxt3o3gudzk1icg/Dolby-AC-3-FoD.zip?rlkey=i9koe4r0cu0nemf1f4j7pm026&st=bhgsaiec&dl=0", ApplicationData.Current.TemporaryFolder.Path, "Dolby-AC-3-FoD.zip"), null),

            // install dolby ac-3 feature on demand
            ("Installing Dolby AC-3 Feature on Demand", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Dolby-AC-3-FoD.zip"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Dolby-AC-3-FoD")); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Dolby-AC-3-FoD.zip")).DeleteAsync(); }, null),
            ("Installing Dolby AC-3 Feature on Demand", async () => await Process.Start(new ProcessStartInfo { FileName = "dism.exe", Arguments = $@"/online /Add-Package /PackagePath:""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Dolby-AC-3-FoD\Microsoft-Windows-DolbyCodec-Package~31bf3856ad364e35~amd64~~10.0.26100.1.mum") }""", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
            ("Installing Dolby AC-3 Feature on Demand", async () => await Process.Start(new ProcessStartInfo { FileName = "dism.exe", Arguments = $@"/online /Add-Package /PackagePath:""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Dolby-AC-3-FoD\Microsoft-Windows-DolbyCodec-WOW64-Package~31bf3856ad364e35~wow64~~10.0.26100.1.mum") }""", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null)
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
                        await ProcessActions.LogError(ex);

                        InstallPage.Info.Title = $"{previousTitle}: {ex.Message}";
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Error);
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;

                        var tcs = new TaskCompletionSource<bool>();

                        RoutedEventHandler resumeHandler = null;
                        resumeHandler = (sender, e) =>
                        {
                            InstallPage.ResumeButton.Click -= resumeHandler;
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                            Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;

                            tcs.TrySetResult(true);
                        };

                        InstallPage.ResumeButton.Click += resumeHandler;
                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
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
                    Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Error);
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
                        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
            Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
        if (filteredActions.Count == 0)
        {
            InstallPage.Progress.Value += stagePercentage;
            Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
    }
}
