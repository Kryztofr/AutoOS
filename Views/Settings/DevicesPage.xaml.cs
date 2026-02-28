using AutoOS.Helpers.Device;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.Storage;

namespace AutoOS.Views.Settings;

public sealed partial class DevicesPage : Page
{
    private bool initialBluetoothState = false;
    private bool isInitializingBluetoothState = true;
    private bool isInitializingHIDState = true;
    private bool isInitializingIMODState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public DevicesPage()
    {
        InitializeComponent();
        GetBluetoothState();
        GetHIDState();
        GetIMODState();

        // copy chiptool to localstate because of permissions
        string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Chiptool");
        string destinationPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "Chiptool");

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);

            foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));

            foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(sourcePath, destinationPath), overwrite: true);
        }
    }

    private void GetBluetoothState()
    {
        // declare services and drivers
        var groups = new[]
        {
            (new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DeviceAssociationService", "DevicesFlowUserSvc", "DsmSvc", "NcbService", "WFDSConMgrSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" }, 3),
            (new[] { "SystemEventsBroker" }, 2)
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
                    isInitializingBluetoothState = false;
                    return;
                }
            }
        }

        initialBluetoothState = true;
        Bluetooth.IsOn = true;
        isInitializingBluetoothState = false;
    }

    private async void Bluetooth_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingBluetoothState) return;

        // disable hittestvisible to avoid double-clicking
        Bluetooth.IsHitTestVisible = false;

        // remove infobar
        BluetoothInfo.Children.Clear();

        // add infobar
        BluetoothInfo.Children.Add(new InfoBar
        {
            Title = Bluetooth.IsOn ? "Enabling Bluetooth..." : "Disabling Bluetooth...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -4, 4, 12)
        });

        // declare services and drivers
        var groups = new[]
        {
            (new[] { "BluetoothUserService", "BTAGService", "BthAvctpSvc", "bthserv", "DsmSvc", "BthA2dp", "BthEnum", "BthHFAud", "BthHFEnum", "BthLEEnum", "BTHMODEM", "BthMini", "BthPan", "BTHPORT", "BTHUSB", "HidBth", "Microsoft_Bluetooth_AvrcpTransport", "RFCOMM", "ibtusb" }, 3),
        };

        // set start values
        foreach (var group in groups)
        {
            foreach (var service in group.Item1)
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{service}", writable: true);
                if (key == null) continue;

                Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "Start", Bluetooth.IsOn ? group.Item2 : 4);
            }
        }

        // delay
        await Task.Delay(500);

        // re-enable hittestvisible
        Bluetooth.IsHitTestVisible = true;

        // remove infobar
        BluetoothInfo.Children.Clear();

        // add infobar
        var infoBar = new InfoBar
        {
            Title = Bluetooth.IsOn ? "Successfully enabled Bluetooth." : "Successfully disabled Bluetooth.",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(4, -4, 4, 12)
        };
        BluetoothInfo.Children.Add(infoBar);

        // add restart button
        if (Bluetooth.IsOn != initialBluetoothState)
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
            BluetoothInfo.Children.Clear();
        }
    }

    private void GetHIDState()
    {
        var devices = DeviceHelper.GetDevices(DeviceType.HID);
        HID.IsOn = devices.Any(device => device.State == DeviceState.Enabled && (device.DeviceDescription?.Contains("HID-compliant") ?? false));

        isInitializingHIDState = false;
    }

    private async void HID_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingHIDState) return;

        // disable hittestvisible to avoid double-clicking
        HID.IsHitTestVisible = false;

        // remove infobar
        DevicesInfo.Children.Clear();

        // add infobar
        DevicesInfo.Children.Add(new InfoBar
        {
            Title = HID.IsOn ? "Enabling Human Interface Devices (HID)..." : "Disabling Human Interface Devices (HID)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -28, 4, 36)
        });

        localSettings.Values["HumanInterfaceDevices"] = HID.IsOn ? 1 : 0;

        // toggle hid devices
        bool isOn = HID.IsOn;

        var devices = DeviceHelper.GetDevices(DeviceType.HID);

        foreach (var device in devices)
        {
            if (device.DeviceDescription.Contains("HID-compliant"))
            {
                await Task.Run(() => DeviceHelper.SetDeviceState(device, isOn));
            }
        }

        // delay
        await Task.Delay(500);

        // re-enable hittestvisible
        HID.IsHitTestVisible = true;

        // remove infobar
        DevicesInfo.Children.Clear();

        // add infobar
        DevicesInfo.Children.Add(new InfoBar
        {
            Title = HID.IsOn ? "Successfully enabled Human Interface Devices (HID)." : "Successfully disabled Human Interface Devices (HID).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(4, -28, 4, 36)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        DevicesInfo.Children.Clear();
    }

    private async void GetIMODState()
    {
        // hide toggle switch
        IMOD.Visibility = Visibility.Collapsed;

        // check state
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = @$"-ExecutionPolicy Bypass -Command ""& '{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "imod.ps1")}' -status '{Path.Combine(PathHelper.GetAppDataFolderPath(), "Chiptool", "chiptool.exe")}'""",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();

        if (output.Contains("ENABLED"))
        {
            localSettings.Values["XhciInterruptModeration"] = 1;
            IMOD.IsOn = true;
        }
        else if (output.Contains("FAILED"))
        {
            // resort to setting
            if ((int?)localSettings.Values["XhciInterruptModeration"] == 1)
            {
                IMOD.IsOn = true;
            }

            IMOD.IsEnabled = false;

            // hide progress ring
            imodProgress.Visibility = Visibility.Collapsed;

            // show toggle
            IMOD.Visibility = Visibility.Visible;

            // add infobar
            DevicesInfo.Children.Add(new InfoBar
            {
                Title = "Failed to check XHCI Interrupt Moderation (IMOD) status.",
                IsClosable = false,
                IsOpen = true,
                Severity = InfoBarSeverity.Error,
                Margin = new Thickness(4, -28, 4, 36)
            });
        }

        // hide progress ring
        imodProgress.Visibility = Visibility.Collapsed;

        // show toggle
        IMOD.Visibility = Visibility.Visible;

        isInitializingIMODState = false;
    }

    private async void IMOD_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingIMODState) return;

        // disable hittestvisible to avoid double-clicking
        IMOD.IsHitTestVisible = false;

        // remove infobar
        DevicesInfo.Children.Clear();

        // add infobar
        DevicesInfo.Children.Add(new InfoBar
        {
            Title = IMOD.IsOn ? "Enabling XHCI Interrupt Moderation (IMOD)..." : "Disabling XHCI Interrupt Moderation (IMOD)...",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -28, 4, 36)
        });

        // toggle imod
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = @$"-ExecutionPolicy Bypass -Command ""& '{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "imod.ps1")}' {(IMOD.IsOn ? "-enable" : "-disable")} '{Path.Combine(PathHelper.GetAppDataFolderPath(), "Chiptool", "chiptool.exe")}'""",
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        // toggle setting
        localSettings.Values["XhciInterruptModeration"] = IMOD.IsOn ? 1 : 0;

        // re-enable hittestvisible
        IMOD.IsHitTestVisible = true;

        // remove infobar
        DevicesInfo.Children.Clear();

        // add infobar
        DevicesInfo.Children.Add(new InfoBar
        {
            Title = IMOD.IsOn ? "Successfully enabled XHCI Interrupt Moderation (IMOD)." : "Successfully disabled XHCI Interrupt Moderation (IMOD).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Success,
            Margin = new Thickness(4, -28, 4, 36)
        });

        // delay
        await Task.Delay(2000);

        // remove infobar
        DevicesInfo.Children.Clear();
    }
}