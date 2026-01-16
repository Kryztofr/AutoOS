using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class NetworkStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool? WOL = PreparingStage.WOL;
        bool? Wifi = PreparingStage.Wifi;
        bool? TxIntDelay = PreparingStage.TxIntDelay;

        InstallPage.Status.Text = "Configuring Network Adapters...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable protocols
            ("Disabling unnecessary protocols", async () => await ProcessActions.RunPowerShell(@"& { Get-NetAdapterBinding | Where-Object { $_.Enabled -eq $true -and $_.ComponentID -in 'ms_msclient','ms_server','ms_implat','ms_lldp','ms_lltdio','ms_rspndr' } | ForEach-Object { Disable-NetAdapterBinding -Name $_.InterfaceAlias -ComponentID $_.ComponentID } }"), null),

            // advanced tcp/ip settings -> wins
            (@"Setting NetBIOS setting to ""Disable NetBIOS over TCP/IP""", async () => await ProcessActions.RunPowerShell(@"Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces' | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name 'NetbiosOptions' -Value 2 -Type DWord -Force }"), null),

            // advanced tcp/ip settings -> wins
            (@"Disabling ""Enable LMHOSTS lookup""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT\Parameters"" /v ""EnableLMHOSTS"" /t REG_DWORD /d 0 /f"), null),

            // adjust ethernet adapter advanced settings
            ("Adjusting Ethernet adapter advanced settings", async () => await ProcessActions.RunPowerShellScript("ethernet.ps1", ""), null),

            // check connection
            ("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), null),

            // adjust wifi adapter advanved settings
            ("Adjusting Wi-Fi adapter advanced settings", async () => await ProcessActions.RunPowerShellScript("wifi.ps1", ""), null),

            // check connection
            ("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), () => Wifi == true),

            // disable power management settings
            ("Disabling power management settings", async () => await ProcessActions.RunPowerShellScript("networkpowermanagement.ps1", ""), null),

            // enabling wake-on-lan
            ("Enabling Wake-On-Lan (WOL)", async () => await ProcessActions.RunPowerShellScript("wol.ps1", ""), () => WOL == true),

             // set txintdelay to 0
            ("Setting TxIntDelay to 0", async () => await ProcessActions.RunPowerShellScript("txintdelay.ps1", ""), () => TxIntDelay == true),

            // set "congestion control provider" to "bbr2"
            (@"Setting ""Congestion Control Provider"" to ""BBR2""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set supplemental internet congestionprovider=bbr2"), null),
            
            // disable loopback large mtu
            (@"Disabling ""Loopback Large Mtu"" for IPv4", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ipv4 set gl loopbacklargemtu=disable"), null),
            (@"Disabling ""Loopback Large Mtu"" for IPv6", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ipv6 set gl loopbacklargemtu=disable"), null),

            // disable "receive side scaling" (rss)
            (@"Disabling ""Receive Side Scaling"" (RSS)", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -ReceiveSideScaling Disabled"), null),
            
            // disable "packet coalescing filter"
            (@"Disabling ""Packet Coalescing Filter""", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -PacketCoalescingFilter Disabled"), null),

            // log advanced network settings
            ("Logging advanced network settings", async () => await ProcessActions.LogAdvancedNetworkSettings(), null),

            // disable wifi services and drivers
            ("Disabling Wi-Fi services and drivers", async () => await ProcessActions.DisableWiFiServicesAndDrivers(), () => Wifi == false),
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