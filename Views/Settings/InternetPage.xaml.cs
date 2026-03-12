using System.Diagnostics;
using Microsoft.Win32;

namespace AutoOS.Views.Settings;

public sealed partial class InternetPage : Page
{
    private bool initialWIFIState = false;
    private bool isInitializingWIFIState = true;
    private bool isInitializingWOLState = true;

    public InternetPage()
    {
        InitializeComponent();
        GetWIFIState();
        GetWOLState();
    }

    private void GetWIFIState()
    {
        // declare services and drivers
        var groups = new[]
        {
            (new[] { "WlanSvc", "Dhcp", "Wcmsvc" }, 2),
            (new[] { "NlaSvc", "WinHttpAutoProxySvc", "Netwtw10", "Netwtw14" }, 3),
            (new[] { "tdx", "vwififlt"}, 1)
        };

        // check if values match
        foreach (var group in groups)
        {
            foreach (var service in group.Item1)
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}");
                if (key == null) continue;

                var startValue = key.GetValue("Start");
                if (startValue == null || (int)startValue != group.Item2)
                {
                    isInitializingWIFIState = false;
                    return;
                }
            }
        }

        initialWIFIState = true;
        WiFi.IsOn = true;
        isInitializingWIFIState = false;
    }

    private async void WiFi_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWIFIState) return;

        // disable hittestvisible to avoid double-clicking
        WiFi.IsHitTestVisible = false;

        // remove infobar
        WiFiInfo.Children.Clear();

        // add infobar
        WiFiInfo.Children.Add(new InfoBar
        {
            Title = WiFi.IsOn ? "Enabling Wi-Fi..." : "Disabling Wi-Fi...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -4, 4, 12)
        });

        // declare services and drivers
        var groups = new[]
        {
            (new[] { "WlanSvc", "Wcmsvc" }, 2),
            (new[] { "NlaSvc", "WinHttpAutoProxySvc", "Netwtw10", "Netwtw14" }, 3),
            (new[] { "tdx", "vwififlt"}, 1)
        };

        // set start values
        foreach (var group in groups)
        {
            foreach (var service in group.Item1)
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true);
                if (key == null) continue;

                Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", WiFi.IsOn ? group.Item2 : 4);
            }
        }

        // delay
        await Task.Delay(500);

        // re-enable hittestvisible
        WiFi.IsHitTestVisible = true;

        // remove infobar
        WiFiInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = WiFi.IsOn ? "Successfully enabled Wi-Fi." : "Successfully disabled Wi-Fi.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(4, -4, 4, 12)
        };
        WiFiInfo.Children.Add(infoBar);

        // add restart button if needed
        if (WiFi.IsOn != initialWIFIState)
        {
            infoBar.Title += " A restart is required to apply the change.";
            infoBar.ActionButton = new Button
            {
                Content = "Restart",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            ((Button)infoBar.ActionButton).Click += (s, args) =>
            Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true });
        }
        else
        {
            // delay
            await Task.Delay(2000);

            // remove infobar
            WiFiInfo.Children.Clear();
        }
    }

    private async void GetWOLState()
    {
        // hide toggle switch
        WOL.Visibility = Visibility.Collapsed;

        // check state
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $@"-ExecutionPolicy Bypass -File ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "wol.ps1")}""",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            }
        };
        process.Start();
        WOL.IsOn = (await process.StandardOutput.ReadToEndAsync()).Contains("ENABLED");

        // hide progress ring
        wolProgress.Visibility = Visibility.Collapsed;

        // show toggle
        WOL.Visibility = Visibility.Visible;

        isInitializingWOLState = false;
    }

    private async void WOL_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingWOLState) return;

        // disable hittestvisible to avoid double-clicking
        WOL.IsHitTestVisible = false;

        // remove infobar
        EthernetInfo.Children.Clear();

        // add infobar
        EthernetInfo.Children.Add(new InfoBar
        {
            Title = WOL.IsOn ? "Enabling Wake-on-LAN (WOL)..." : "Disabling Wake-on-LAN (WOL)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // toggle wol
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", WOL.IsOn ? "wol-enable.ps1" : "wol-disable.ps1")}\"",
                CreateNoWindow = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        // re-enable hittestvisible
        WOL.IsHitTestVisible = true;

        // remove infobar
        EthernetInfo.Children.Clear();

        // add infobar
        EthernetInfo.Children.Add(new InfoBar
        {
            Title = WOL.IsOn ? "Successfully enabled Wake-on-LAN (WOL)" : "Successfully disabled Wake-on-LAN (WOL)",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        EthernetInfo.Children.Clear();
    }
}