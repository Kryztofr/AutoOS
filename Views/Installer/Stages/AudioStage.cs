using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class AudioStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool? NetAdapterCx = PreparingStage.NetAdapterCx;

        InstallPage.Status.Text = "Configuring Audio Devices...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable system beeps
            ("Disabling system beeps", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Sound"" /v Beep /t REG_SZ /d no /f"), null),

            // sound -> communications
            (@"Setting ""When Windows detects communication activity"" to ""Do nothing""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Multimedia\Audio"" /v UserDuckingPreference /t REG_DWORD /d 3 /f"), null),

            // disable audio enhancements
            ("Disabling audio enhancements", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),
            ("Disabling audio enhancements", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"powershell -Command ""$Keys = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture'); foreach ($Key in $Keys) { Get-ChildItem $Key -Recurse | Where-Object { $_.PSPath -match '\\FxProperties$' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name '{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5' -Value 1 } }"""), null),

            // disable audio idle states
            ("Disabling audio idle states", async () => await ProcessActions.RunPowerShellScript("audioidlestates.ps1", ""), null),

            // split audio services
            ("Splitting audio services", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c copy /y %windir%\System32\svchost.exe %windir%\System32\audiosvchost.exe"), null),
            ("Splitting audio services", async () => await ProcessActions.RunPowerShell(@"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Audiosrv' -Name 'ImagePath' -Value '%systemroot%\system32\audiosvchost.exe -k LocalServiceNetworkRestricted -p' -Type ExpandString"), null),
            ("Splitting audio services", async () => await ProcessActions.RunPowerShell(@"Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\AudioEndpointBuilder' -Name 'ImagePath' -Value '%systemroot%\system32\audiosvchost.exe -k LocalSystemNetworkRestricted -p' -Type ExpandString"), null),
        
            // optimize multimedia class scheduler service (mmcss)
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v NoLazyMode /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v LazyModeTimeout /t REG_DWORD /d 4294967295 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SchedulerPeriod /t REG_DWORD /d 1000000 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v IdleDetectionCycles /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SchedulerTimerResolution /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio"" /v Priority /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio"" /v ""Scheduling Category"" /t REG_SZ /d High /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio"" /v ""Priority When Yielded"" /t REG_DWORD /d 19 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio"" /v Priority /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio"" /v ""Scheduling Category"" /t REG_SZ /d High /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio"" /v ""Priority When Yielded"" /t REG_DWORD /d 19 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback"" /v Priority /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback"" /v ""Scheduling Category"" /t REG_SZ /d High /f"), null),
            ("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback"" /v ""Priority When Yielded"" /t REG_DWORD /d 19 /f"), null),

            // disable multimedia class scheduler service (mmcss)
            ("Disabling Multimedia Class Scheduler Service (MMCSS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MMCSS"" /v Start /t REG_DWORD /d 4 /f"), () => NetAdapterCx == true),

            //// download dolby ac-3 feature on demand
            //("Downloading Dolby AC-3 Feature on Demand", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/g7qcrrpxt3o3gudzk1icg/Dolby-AC-3-FoD.zip?rlkey=i9koe4r0cu0nemf1f4j7pm026&st=bhgsaiec&dl=0", Path.GetTempPath(), "Dolby-AC-3-FoD.zip"), null),

            //// install dolby ac-3 feature on demand
            //("Installing Dolby AC-3 Feature on Demand", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "Dolby-AC-3-FoD.zip"), Path.Combine(Path.GetTempPath(), "Dolby-AC-3-FoD")), null),
            //("Installing Dolby AC-3 Feature on Demand", async () => await ProcessActions.RunNsudo("CurrentUser", @"DISM /online /Add-Package /PackagePath:""%TEMP%\Dolby-AC-3-FoD\Microsoft-Windows-DolbyCodec-Package~31bf3856ad364e35~amd64~~10.0.26100.1.mum"""), null),
            //("Installing Dolby AC-3 Feature on Demand", async () => await ProcessActions.RunNsudo("CurrentUser", @"DISM /online /Add-Package /PackagePath:""%TEMP%\Dolby-AC-3-FoD\Microsoft-Windows-DolbyCodec-WOW64-Package~31bf3856ad364e35~wow64~~10.0.26100.1.mum"""), null),
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