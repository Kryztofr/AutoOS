using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

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
            ("Grouping services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"powershell -ExecutionPolicy Bypass -file ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "groupservices.ps1")}"""), null),  

            // disable failure actions
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"" /v ""InactivityShutdownDelay"" /t REG_DWORD /d 4294967295 /f"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure AudioEndpointBuilder reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag AudioEndpointBuilder 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure Appinfo reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag Appinfo 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure AppXSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag AppXSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure CaptureService reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag CaptureService 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure cbdhsvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag cbdhsvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure ClipSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag ClipSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure CryptSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag CryptSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure DevicesFlowUserSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag DevicesFlowUserSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure Dhcp reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag Dhcp 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure DispBrokerDesktopSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag DispBrokerDesktopSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure Dnscache reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag Dnscache 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure DoSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag DoSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure DsmSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag DsmSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure gpsvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag gpsvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure InstallService reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag InstallService 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure KeyIso reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag KeyIso 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure lfsvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag lfsvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure msiserver reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag msiserver 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure NcbService reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag NcbService 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure netprofm reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag netprofm 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure NgcSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag NgcSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure NgcCtnrSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag NgcCtnrSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure nsi reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag nsi 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure ProfSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag ProfSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure sppsvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag sppsvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure StateRepository reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag StateRepository 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure TextInputManagementService reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag TextInputManagementService 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure TrustedInstaller reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag TrustedInstaller 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure UdkUserSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failu^^reflag UdkUserSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure WFDSConMgrSvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag WFDSConMgrSvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure WinHttpAutoProxySvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag WinHttpAutoProxySvc 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure Winmgmt reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag Winmgmt 0"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failure Wcmsvc reset=0 actions=//"), null),
            ("Disabling failure actions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc failureflag Wcmsvc 0"), null)
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