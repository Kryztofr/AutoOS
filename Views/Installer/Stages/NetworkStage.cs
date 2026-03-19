using AutoOS.Helpers.Device;
using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Microsoft.Win32;
using AutoOS.Helpers.Registry;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class NetworkStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool Wifi = PreparingStage.Wifi;
        bool TxIntDelay = PreparingStage.TxIntDelay;

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
            (@"Disabling ""Enable LMHOSTS lookup""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT\Parameters", "EnableLMHOSTS", 0, RegistryValueKind.DWord), null),

            // adjust ethernet adapter advanced settings
            ("Adjusting Ethernet adapter advanced settings", async () => await ProcessActions.RunPowerShellScript("ethernet.ps1", ""), null),

            // check connection
            ("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), null),

            // adjust wifi adapter advanved settings
            ("Adjusting Wi-Fi adapter advanced settings", async () => await ProcessActions.RunPowerShellScript("wifi.ps1", ""), () => Wifi == true),

            // check connection
            ("Waiting for internet connection to reestablish", async () => await ProcessActions.RunConnectionCheck(), () => Wifi == true),

            // disable power management settings
            ("Disabling power management settings", async () => await ProcessActions.RunPowerShellScript("networkpowermanagement.ps1", ""), null),

            // set txintdelay to 0
            ("Setting TxIntDelay to 0", async () => DeviceHelper.GetDevices(DeviceType.NIC).Where(d => Registry.LocalMachine.OpenSubKey(d.RegistryPath).GetValue("TxIntDelay") != null).ToList().ForEach(d => Registry.LocalMachine.OpenSubKey(d.RegistryPath, true).SetValue("TxIntDelay", 0, RegistryValueKind.DWord)), () => TxIntDelay == true),

            // set "congestion control provider" to "bbr2"
            (@"Setting ""Congestion Control Provider"" to ""BBR2""", async () => await Process.Start(new ProcessStartInfo { FileName = "netsh", Arguments = "int tcp set supplemental internet congestionprovider=bbr2", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
            
            // disable loopback large mtu
            (@"Disabling ""Loopback Large Mtu"" for IPv4", async () => await Process.Start(new ProcessStartInfo { FileName = "netsh", Arguments = "int ipv4 set gl loopbacklargemtu=disable", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
            (@"Disabling ""Loopback Large Mtu"" for IPv6", async () => await Process.Start(new ProcessStartInfo { FileName = "netsh", Arguments = "int ipv6 set gl loopbacklargemtu=disable", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),

            // disable "receive side scaling" (rss)
            (@"Disabling ""Receive Side Scaling"" (RSS)", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -ReceiveSideScaling Disabled"), null),
            
            // disable "packet coalescing filter"
            (@"Disabling ""Packet Coalescing Filter""", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -PacketCoalescingFilter Disabled"), null)
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
