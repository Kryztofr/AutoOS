using AutoOS.Views.Installer.Actions;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Storage;

namespace AutoOS.Views.Installer
{
    public sealed partial class HomeLandingPage : Page
    {
        [LibraryImport("user32.dll")]
        private static partial void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public HomeLandingPage()
        {
            InitializeComponent();
            Loaded += HomeLandingPage_Loaded;
        }

        private async void HomeLandingPage_Loaded(object sender, RoutedEventArgs e)
        {
            #if !DEBUG
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key == null) return;

                if (key.GetValue("InstallDate") is int unixSeconds)
                {
                    var installDate = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
                    if ((DateTime.Now - installDate).TotalDays > 2)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Fresh Windows Required",
                            Content = "AutoOS currently only on fresh installations of Windows.\nPlease follow the Getting Started guide in the README on GitHub.",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = App.MainWindow.Content.XamlRoot
                        };
                        await dialog.ShowAsync();
                        Application.Current.Exit();
                    }
                }

                string buildStr = key.GetValue("CurrentBuild")?.ToString() ?? "";
                string ubrStr = key.GetValue("UBR")?.ToString() ?? "";
                if (int.TryParse(buildStr, out int build) && int.TryParse(ubrStr, out int ubr))
                {
                    if (build != 22631 || (build == 22631 && ubr < 6494))
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Unsupported Windows Version",
                            Content = $"AutoOS is currently only supported on new versions of Windows 23H2. \nPlease download it from the Getting Started guide in the README on GitHub.",
                            CloseButtonText = "OK",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = App.MainWindow.Content.XamlRoot
                        };
                        await dialog.ShowAsync();
                        Application.Current.Exit();
                    }
                }
            #endif

            // enable app access to location
            await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v ""Value"" /t REG_SZ /d ""Allow"" /f");
            await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v ""Value"" /t REG_SZ /d ""Allow"" /f");
            await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\activity"" /v ""Value"" /t REG_SZ /d ""Allow"" /f");
            await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\activity"" /v ""Value"" /t REG_SZ /d ""Allow"" /f");
            await ProcessActions.RunNsudo("CurrentUser", @"reg add HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location\AutoOS_xxtketq8p23nt /v Value /t REG_SZ /d Allow /f");

            // switch keyboard layout
            if (!(localSettings.Values["HasChangedLayout"] as bool? == true))
            {
                keybd_event(0x5B, 0, 0x0001, UIntPtr.Zero);
                keybd_event(0x20, 0, 0x0001, UIntPtr.Zero);
                keybd_event(0x20, 0, 0x0002, UIntPtr.Zero);
                keybd_event(0x5B, 0, 0x0002, UIntPtr.Zero);
                localSettings.Values["HasChangedLayout"] = true;
            }
        }
    }
}