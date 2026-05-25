using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using AutoOS.Core.Helpers.TaskScheduler;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class RegistryStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        return new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // system -> notifications
            (@"Disabling ""Suggest ways to get the most out of Windows and finish setting up this device""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement", "ScoobeSystemSettingEnabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Get tips and suggestions when using Windows""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord), null),

            // personalization -> start
            (@"Disabling ""Show websites from your browsing history""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_RecoPersonalizedSites", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Show recommedations for tips, shortcuts, new apps and more""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_IrisRecommendations", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Show account-related notifications""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_AccountNotifications", 0, RegistryValueKind.DWord), null),

            // privacy & security -> recommendations & offers
            (@"Disabling ""Let apps show me personalized ads by using my advertising ID""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Let apps show me personalized ads by using my advertising ID""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\AdvertisingInfo", "Value", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Let websites show me locally relevant content by accessing my language list""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Let Windows improve Start and search results by tracking app launches""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Show me suggested content in the Settings app""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Show me suggested content in the Settings app""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-353694Enabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Show me suggested content in the Settings app""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-353696Enabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Show notifications in the Settings app""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SystemSettings\AccountNotifications", "EnableAccountNotifications", 0, RegistryValueKind.DWord), null),
            
            // privacy & security -> speech
            (@"Disabling ""Online speech recognition""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy", "HasAccepted", 0, RegistryValueKind.DWord), null),

            // privacy & security -> inking & typing personalization
            (@"Disabling ""Custom inking and typing dictionary""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\InkingAndTypingPersonalization", "Value", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization\TrainedDataStore", "HarvestContacts", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CPSS\DevicePolicy\InkingAndTypingPersonalization", "DefaultValue", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Improve inking and typing recognition"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput", "AllowLinguisticDataCollection", 0, RegistryValueKind.DWord), null),

            // privacy & security -> diagnostics & feedback
            (@"Disabling ""Send optional diagnostic data""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack", "ShowedToastAtLevel", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Improve inking and typing""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\ImproveInkingAndTyping", "Value", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Improve inking and typing""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Input\TIPC", "Enabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Tailored experiences""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Turn on the Diagnostic Data Viewer (uses up to 1GB of hard drive space)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack\EventTranscriptKey", "EnableEventTranscript", 0, RegistryValueKind.DWord), null),
            (@"Setting ""Feedback frequency"" to never", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord), null),

            // privacy & security -> activity history (no longer in settings app)
            (@"Disabling ""Enables Activity Feed"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Allow publishing of User Activities"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Allow upload of User Activities"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0, RegistryValueKind.DWord), null),

            // privacy & security -> voice activation
            (@"Disabling ""Let apps access voice activation services""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps", "AgentActivationEnabled", 0, RegistryValueKind.DWord), null),

            // apps -> advanced app settings
            (@"Setting ""Share across devices"" to ""Off""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP", "RomeSdkChannelUserAuthzPolicy", 0, RegistryValueKind.DWord), null),
            (@"Setting ""Share across devices"" to ""Off""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP", "CdpSessionUserAuthzPolicy", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Archive apps""", async () => await ProcessActions.RunPowerShell(@"New-Item -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\InstallService\Stubification\$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"" -Force | Out-Null; New-ItemProperty -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\InstallService\Stubification\$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"" -Name EnableAppOffloading -PropertyType DWord -Value 0 -Force"), null),

            // apps -> resume
            ("Disabling resume feature", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CrossDeviceResume\Configuration", "IsResumeAllowed", 0, RegistryValueKind.DWord), null),

            // system -> clipboard
            (@"Enabling ""Clipboard history""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Clipboard", "EnableClipboardHistory", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Allow Clipboard synchronization across devices"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", 0, RegistryValueKind.DWord), null),

            // bluetooth & devices -> autoplay
            (@"Disabling ""Use AutoPlay for all media and devices""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers", "DisableAutoplay", 1, RegistryValueKind.DWord), null),

            // time & language -> typing -> typing insights
            (@"Disabling ""Typing insights""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\input\Settings", "InsightsEnabled", 0, RegistryValueKind.DWord), null),
                        
            // keyboard properties -> speed -> character repeat
            (@"Setting ""Repeat delay"" to ""Short""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay", "0", RegistryValueKind.String), null),

            // set desktop mode default over tablet mode
            ("Setting desktop mode as default over tablet mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell", "SignInMode", 1, RegistryValueKind.DWord), null),

            // disable tablet mode
            ("Disabling tablet mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell", "TabletMode", 0, RegistryValueKind.DWord), null),

            // disable tablet mode prompts and always switch
            ("Disabling tablet mode prompts and always switch", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell", "ConvertibleSlateModePromptPreference", 2, RegistryValueKind.DWord), null),

            // enable taskbar when in tablet mode
            ("Enabling taskbar when in tablet mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAppsVisibleInTabletMode", 1, RegistryValueKind.DWord), null),

            // disable automatic hiding of the taskbar in tablet mode
            ("Disabling automatic hiding of the taskbar in tablet mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAutoHideInTabletMode", 0, RegistryValueKind.DWord), null),

            // disable automatic folder type discovery
            ("Disabling automatic folder type discovery", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags"), null),
            ("Disabling automatic folder type discovery", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU"), null),
            ("Disabling automatic folder type discovery", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell", "FolderType", "NotSpecified", RegistryValueKind.String), null),

            // disable jpeg wallpaper compression
            ("Disabling JPEG wallpaper compression", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\Desktop", "JPEGImportQuality", 100, RegistryValueKind.DWord), null),

            // explorer -> options -> privacy
            (@"Disabling ""Show recently used files""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowRecent", 0, RegistryValueKind.DWord), null),
            
            // explorer -> options -> privacy
            (@"Disabling ""Show frequently used folders""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowFrequent", 0, RegistryValueKind.DWord), null),
            
            // explorer -> options -> privacy
            (@"Disabling ""Show files from Office.com""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer", "ShowCloudFilesInQuickAccess", 0, RegistryValueKind.DWord), null),

            // enable "clear history of recently opened documents on exit" policy
            (@"Enabling ""Clear history of recently opened documents on exit"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "ClearRecentDocsOnExit", 1, RegistryValueKind.DWord), null),

            // enable "do not track shell shortcuts during roaming" policy
            (@"Enabling ""Do not track Shell shortcuts during roaming"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "LinkResolveIgnoreLinkInfo", 1, RegistryValueKind.DWord), null),
            
            // enable "do not use tracking-based method when resolving shell shortcuts" policy
            (@"Enabling ""Do not use the tracking-based method when resolving shell shortcuts"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoResolveTrack", 1, RegistryValueKind.DWord), null),

            // enable "more details" in file dialogs
            (@"Enabling ""More details"" in file dialogs", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager", "EnthusiastMode", 1, RegistryValueKind.DWord), null),

            // disable "show extracted files when complete"
            (@"Disabling ""Show extracted files when complete""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\ExtractionWizard", "ShowFiles", 0, RegistryValueKind.DWord), null),

            // remove "gallery" tab from file explorer
            (@"Removing ""Gallery"" tab from file explorer", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"), null),

            // set ""mouse hover time" to 150ms
            (@"Setting ""Mouse hover time""to 150ms", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseHoverTime", "150", RegistryValueKind.String), null),

            // set "Menu show delay" to 150ms
            (@"Setting ""Mouse hover time"" to 150ms", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\Desktop", "MenuShowDelay", "150", RegistryValueKind.String), null),

            // disable telemetry
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "CLOUDSDK_CORE_DISABLE_PROMPTS", "1", RegistryValueKind.String), null),
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "DOTNET_TRY_CLI_TELEMETRY_OPTOUT", "1", RegistryValueKind.String), null),
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "DOTNET_CLI_TELEMETRY_OPTOUT", "1", RegistryValueKind.String), null),
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "DOCKER_CLI_TELEMETRY_OPTOUT", "1", RegistryValueKind.String), null),
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "npm_config_loglevel", "silent", RegistryValueKind.String), null),
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "POWERSHELL_TELEMETRY_OPTOUT", "1", RegistryValueKind.String), null),
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "VS_TELEMETRY_OPT_OUT", "1", RegistryValueKind.String), null),
            ("Disabling telemetry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DiagTrack", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling telemetry", async () => ServicesHelper.StopService("DiagTrack"), null),
            (@"Enabling ""Limit Diagnostic Log Collection"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "LimitDiagnosticLogCollection", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Limit Dump Collection"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "LimitDumpCollection", 1, RegistryValueKind.DWord), null),
            (@"Setting ""Limit optional diagnostic data for Desktop Analytics"" policy to ""Disable Desktop Analytics collection""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "LimitEnhancedDiagnosticDataWindowsAnalytics", 0, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not show feedback notifications"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Allow device name to be sent in Windows diagnostic data"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowDeviceNameInTelemetry", 0, RegistryValueKind.DWord), null),
            (@"Setting ""Configure Authenticated Proxy usage for the Connected User Experience and Telemetry service"" policy to ""Disable Authenticated Proxy usage""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DisableEnterpriseAuthProxy", 1, RegistryValueKind.DWord), null),
            (@"Setting ""Configure collection of browsing data for Desktop Analytics"" policy to ""Do not allow sending intranet or internet history""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\DataCollection", "MicrosoftEdgeDataOptIn", 0, RegistryValueKind.DWord), null),
            (@"Setting ""Configure collection of browsing data for Desktop Analytics"" policy to ""Do not allow sending intranet or internet history""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "MicrosoftEdgeDataOptIn", 0, RegistryValueKind.DWord), null),
            (@"Setting ""Configure diagnostic data opt-in change notifications"" policy to ""Disable diagnostic data change notifications""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DisableTelemetryOptInChangeNotification", 1, RegistryValueKind.DWord), null),
            (@"Setting ""Configure diagnostic data opt-in settings user interface"" policy to ""Disable diagnostic data opt-in settings""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DisableTelemetryOptInSettingsUx", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Configure Watson events"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Reporting", "DisableGenericRePorts", 1, RegistryValueKind.DWord), null),

            // disable windows error reporting
            (@"Enabling ""Disable Windows Error Reporting"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Disable Windows Error Reporting"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not send additional data"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\Windows Error Reporting", "LoggingDisabled", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Disable logging"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "LoggingDisabled", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not send additional data"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\Windows Error Reporting", "DontSendAdditionalData", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not send additional data"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "DontSendAdditionalData", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Prevent display of the user interface for critical errors"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "DontShowUI", 1, RegistryValueKind.DWord), null),
            (@"Setting ""Customize consent settings"" policy to 0", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Error Reporting\Consent", "0", string.Empty, RegistryValueKind.String), null),
            (@"Setting ""Customize consent settings"" policy to 0", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting\Consent", "0", string.Empty, RegistryValueKind.String), null),
            (@"Disabling ""Ignore custom consent settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Error Reporting\Consent", "DefaultOverrideBehavior", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Ignore custom consent settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\Consent", "DefaultOverrideBehavior", 0, RegistryValueKind.DWord), null),

            // disable customer experience improvement program
            ("Disabling Customer Experience Improvement Program (CEIP)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQMClient\Windows", "CEIPEnable", 0, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off Windows Customer Experience Improvement Program"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Microsoft Customer Experience Improvement Program (CEIP)"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\AppV\CEIP", "CEIPEnable", 0, RegistryValueKind.DWord), null),
            ("Disabling Customer Experience Improvement Program (CEIP) for Visual Studio 2025", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VSCommon\18.0\SQM", "OptIn", 0, RegistryValueKind.DWord), null),

            // disable messages to cloud services
            (@"Disabling ""Allow Message Service Cloud Sync"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Messaging", "AllowMessageSync", 0, RegistryValueKind.DWord), null),

            // disable "allow online tips" policy
            (@"Disabling ""Allow Online Tips"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "AllowOnlineTips", 0, RegistryValueKind.DWord), null),

            // enable "allow microsoft accounts to be optional" policy
            (@"Enabling ""Allow Microsoft accounts to be optional"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System", "MSAOptional", 1, RegistryValueKind.DWord), null),

            // disable program compatibility assistant
            (@"Enabling ""Turn off Program Compatibility Assistant"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisablePCA", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off Program Compatibility Assistant"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat", "DisablePCA", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off Steps Recorder"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat", "DisableUAR", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off Inventory Collector"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat", "DisableInventory", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off Application Telemetry"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat", "AITEnable", 0, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off SwitchBack Compatibility Engine"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat", "SbEnable", 0, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off Application Compatibility Engine"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat", "DisableEngine", 1, RegistryValueKind.DWord), null),

            // disable "allow remote assistance connections to this computer"
            (@"Disabling ""Allow Remote Assistance connection to this computer""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 0, RegistryValueKind.DWord), null),

            // disable "fault tolerant heap" (fth)
            (@"Disabling ""Fault Tolerant Heap"" (FTH)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\FTH", "Enabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Fault Tolerant Heap"" (FTH)", async () => await Process.Start(new ProcessStartInfo { FileName = "rundll32.exe", Arguments = "fthsvc.dll,FthSysprepSpecialize" , CreateNoWindow = true })!.WaitForExitAsync(), null),

            // disable settings sync
            (@"Enabling ""Do not sync"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync app settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableApplicationSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync app settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableApplicationSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync passwords"" policy""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableCredentialsSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync passwords"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableCredentialsSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync personalize"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisablePersonalizationSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync personalize"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisablePersonalizationSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync Apps"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableAppSyncSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync Apps"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableAppSyncSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync other Windows settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableWindowsSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync other Windows settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableWindowsSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync desktop personalization"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableDesktopThemeSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync desktop personalization"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableDesktopThemeSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync browser settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableWebBrowserSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync browser settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableWebBrowserSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync on metered connections"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSyncOnPaidNetwork", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync start settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableStartLayoutSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync start settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableStartLayoutSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync accessibility settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableAccessibilitySettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync accessibility settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableAccessibilitySettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync language preferences settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableLanguageSettingSync", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Do not sync language preferences settings"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableLanguageSettingSyncUserOverride", 1, RegistryValueKind.DWord), null),
            ("Disabling settings sync", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Accessibility", "Enabled", 0, RegistryValueKind.DWord), null),
            ("Disabling settings sync", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Personalization", "Enabled", 0, RegistryValueKind.DWord), null),
            ("Disabling settings sync", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\BrowserSettings", "Enabled", 0, RegistryValueKind.DWord), null),
            ("Disabling settings sync", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Credentials", "Enabled", 0, RegistryValueKind.DWord), null),
            ("Disabling settings sync", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Windows", "Enabled", 0, RegistryValueKind.DWord), null),
            ("Disabling settings sync", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync", "SyncPolicy", 5, RegistryValueKind.DWord), null),

            // set "set the default behavior for autorun" policy to "do not execute any autorun commands"
            (@"Setting ""Set the default behavior for AutoRun"" policy to ""Do not execute any autorun commands""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoAutorun", 1, RegistryValueKind.DWord), null),
            
            // set "turn off autoplay" policy to "all drives"
            (@"Setting ""Turn off Autoplay"" policy to ""All drives""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 255, RegistryValueKind.DWord), null),

            // reserve 10% of CPU resources to low-priority tasks
            ("Reserving 10% of CPU resources to low-priority tasks", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 10, RegistryValueKind.DWord), null),
            
            // set "win32priorityseparation" to 0x18/24
            (@"Setting ""Win32PrioritySeparation"" to 0x18/24", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 24, RegistryValueKind.DWord), null),

            // setting "let apps run in the background" policy to "force deny"
            (@"Setting ""Let Windows apps run in the background"" policy to ""Force Deny""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2, RegistryValueKind.DWord), null),

            // disable autorun entries
            ("Disabling Autorun entries", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot", "AlternateShell"), null),
            ("Disabling Autorun entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot\AutorunsDisabled", "AlternateShell", "cmd.exe", RegistryValueKind.String), null),
            ("Disabling Autorun entries", async () => TaskSchedulerHelper.Unregister("UnlockStartLayout"), null),

            // enable windows spotlight
            ("Enabling Windows Spotlight", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\CloudContent", "DisableCloudOptimizedContent"), null),
            
            // disable search indexing
            ("Disabling Search Indexing", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling Search Indexing", async () => ServicesHelper.StopService("WSearch"), null),

            // disable game bar presence writer
            ("Disabling GameBar Presence Writer", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter", "ActivationType", 0, RegistryValueKind.DWord), null),

            // enable scroll wheel for alt tab
            (@"Enabling Scroll Wheel for Alt Tab", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Ingan121\ClassicWindowSwitcher", "ScrollWheelBehavior", 1, RegistryValueKind.DWord), null)
        };
    }
}

