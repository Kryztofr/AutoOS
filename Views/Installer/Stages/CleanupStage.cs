using AutoOS.Helpers.Registry;
using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.Storage;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class CleanupStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Status.Text = "Cleaning up...";

        string previousTitle = string.Empty;
        int stagePercentage = 4;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // clean temp directories
            ("Cleaning temp directories", async () => await Task.WhenAll(Process.GetProcessesByName("TiWorker").Select(async process => { process.Kill(); await process.WaitForExitAsync(); })), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\Logs"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\Panther"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\SoftwareDistribution"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\LogFiles"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\SleepStudy"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\sru"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\WDI"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\winevt\Logs"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\SystemTemp"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\Temp"); }), null),
            ("Cleaning temp directories", async () => ProcessActions.CleanDirectory(ApplicationData.Current.TemporaryFolder.Path), null),
            ("Cleaning temp directories", async () => File.Delete(@"C:\DumpStack.log"), null),

            // run disk cleanup
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\BranchCache", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Content Indexer Cleaner", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Delivery Optimization Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Device Driver Packages", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Diagnostic Data Viewer database files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Feedback Hub Archive log files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Language Pack", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Offline Pages Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\RetailDemo Offline Content", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Update Cleanup", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Upgrade Discarded Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\User file versions", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Defender", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows ESD installation files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Reset Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr.exe", Arguments = "/sagerun:0", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
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

        InstallPage.Status.Text = "Installation finished.";
        InstallPage.Info.Severity = InfoBarSeverity.Success;
        InstallPage.Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
        InstallPage.ProgressRingControl.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
        localSettings.Values["Version"] = ProcessInfoHelper.Version;
        localSettings.Values["Install_End"] = DateTimeOffset.Now.ToString("O");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\AutoOS", "IsInstalled", 1, RegistryValueKind.DWord);
        StartupTaskState state = await (await StartupTask.GetAsync("AutoOS")).RequestEnableAsync();
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer", "LockedStartLayout", 0, RegistryValueKind.DWord);
        await ProcessActions.Log();
        InstallPage.Info.Title = "Restarting in 3...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 2...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 1...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting...";
        await Task.Delay(750);
        ProcessStartInfo processStartInfo = new()
        {
            FileName = "cmd.exe",
            Arguments = $"/c shutdown /r /t 0",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(processStartInfo);
    }
}
