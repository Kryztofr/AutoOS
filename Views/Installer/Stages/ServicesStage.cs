using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Registry;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Microsoft.Win32;
using AutoOS.Helpers.Services;

namespace AutoOS.Views.Installer.Stages;

public static class ServicesStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Status.Text = "Configuring Services and Drivers...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // group services
            ("Grouping services", async () => ServicesHelper.GroupServices(), null),

            // disable failure actions
            ("Disabling failure actions", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "InactivityShutdownDelay", 4294967295, RegistryValueKind.DWord), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("AudioEndpointBuilder"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("Appinfo"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("AppXSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("CaptureService"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("cbdhsvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("ClipSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("CryptSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("DevicesFlowUserSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("Dhcp"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("DispBrokerDesktopSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("Dnscache"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("DoSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("DsmSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("gpsvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("InstallService"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("KeyIso"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("lfsvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("msiserver"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("NcbService"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("netprofm"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("NgcSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("NgcCtnrSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("nsi"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("ProfSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("sppsvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("StateRepository"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("TextInputManagementService"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("TrustedInstaller"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("UdkUserSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("WFDSConMgrSvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("WinHttpAutoProxySvc"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("Winmgmt"), null),
            ("Disabling failure actions", async () => ServicesHelper.DisableFailureActions("Wcmsvc"), null)
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