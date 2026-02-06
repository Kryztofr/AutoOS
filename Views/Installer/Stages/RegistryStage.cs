using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class RegistryStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);

        InstallPage.Status.Text = "Configuring Registry...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // privacy & security -> recommendations & offers
            (@"Disabling ""Personalized offers""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy"" /v ""TailoredExperiencesWithDiagnosticDataEnabled"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Personalized offers""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CPSS\DevicePolicy\TailoredExperiencesWithDiagnosticDataEnabled"" /v DefaultValue /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow websites to access my language list""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\International\User Profile"" /v ""HttpAcceptLanguageOptOut"" /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Improve Start and search results""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""Start_TrackProgs"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Show notifications in Settings""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SystemSettings\AccountNotifications"" /v EnableAccountNotifications /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Recommendations and offers in Settings""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338393Enabled /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Recommendations and offers in Settings""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353694Enabled /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Recommendations and offers in Settings""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353696Enabled /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Advertising ID""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\AdvertisingInfo"" /v Value /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Advertising ID""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Advertising ID""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Advertising ID""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"" /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f"), null),
            
            // privacy & security -> speech
            (@"Disabling ""Online speech recognition""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy"" /v ""HasAccepted"" /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> inking & typing personalization
            (@"Disabling ""Custom inking and typing dictionary""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\InkingAndTypingPersonalization"" /v Value /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Personalization\Settings"" /v ""AcceptedPrivacyPolicy"" /t REG_DWORD /d 0 /f"), null),
            
            // these two might be deprecated
            (@"Disabling ""Custom inking and typing dictionary""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitTextCollection"" /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitInkCollection"" /t REG_DWORD /d 1 /f"), null),
            
            (@"Disabling ""Custom inking and typing dictionary""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization\TrainedDataStore"" /v ""HarvestContacts"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Custom inking and typing dictionary""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CPSS\DevicePolicy\InkingAndTypingPersonalization"" /t DefaultValue /d 0 /f"), null),
            (@"Disabling ""Improve inking and typing recognition"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput"" /v AllowLinguisticDataCollection /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> diagnostics & feedback
            (@"Disabling ""Send optional diagnostic data""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack"" /v ""ShowedToastAtLevel"" /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Improve inking and typing""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\ImproveInkingAndTyping"" /v Value /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Improve inking and typing""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Input\TIPC"" /t Enabled /d 0 /f"), null),
            (@"Disabling ""Turn on the Diagnostic Data Viewer (uses up to 1GB of hard drive space)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack\EventTranscriptKey"" /v EnableEventTranscript /t REG_DWORD /d 0 /f"), null),
            (@"Setting ""Feedback frequency"" to never", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules"" /v ""NumberOfSIUFInPeriod"" /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> activity history
            (@"Disabling ""Enables Activity Feed"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow publishing of User Activities"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v PublishUserActivities /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow upload of User Activities"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v UploadUserActivities /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> voice activation
            (@"Disabling ""Let apps access voice activation services""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps"" /v ""AgentActivationEnabled"" /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> notifications
            (@"Disabling ""Notification access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> account info
            (@"Disabling ""Account info access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userAccountInformation"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> contacts
            (@"Disabling ""Contacts access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\contacts"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> calendar
            (@"Disabling ""Calendar access""",  async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appointments"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> phone calls
            (@"Disabling ""Phone call access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCall"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> call history
            (@"Disabling ""Call history access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCallHistory"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> email
            (@"Disabling ""Email access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\email"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> tasks
            (@"Disabling ""Tasks access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userDataTasks"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> messaging
            (@"Disabling ""Messaging access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\chat"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> radios
            (@"Disabling ""Radio control access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\radios"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> other devices
            (@"Disabling ""Communicate with unpaired devices""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\bluetoothSync"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> app diagnostics
            (@"Disabling ""App diagnostics access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> documents
            (@"Disabling ""Documents library access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> download
            (@"Disabling ""Downloads folder access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\downloadsFolder"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> music
            (@"Disabling ""Allow access to music libraries on this device""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\musicLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> pictures
            (@"Disabling ""Pictures library access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> videos
            (@"Disabling ""Videos library access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> file system
            (@"Disabling ""File system access""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // apps -> advanced app settings
            (@"Setting ""Share across devices"" to ""Off""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP"" /v ""RomeSdkChannelUserAuthzPolicy"" /t REG_DWORD /d 0 /f"), null),
            (@"Setting ""Share across devices"" to ""Off""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP"" /v ""CdpSessionUserAuthzPolicy"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Archive apps""", async () => await ProcessActions.RunPowerShell(@"New-Item -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\InstallService\Stubification\$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"" -Force | Out-Null; New-ItemProperty -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\InstallService\Stubification\$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"" -Name EnableAppOffloading -PropertyType DWord -Value 0 -Force"), null),

            // apps -> resume
            ("Disabling resume feature", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CrossDeviceResume\Configuration"" /v ""IsResumeAllowed"" /t REG_DWORD /d 0 /f"), null),

            // system -> clipboard
            (@"Enabling ""Clipboard history""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Clipboard"" /v EnableClipboardHistory /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Allow Clipboard synchronization across devices"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f"), null),

            // bluetooth & devices -> autoplay
            (@"Disabling ""Use AutoPlay for all media and devices""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers"" /v DisableAutoplay /t REG_DWORD /d 1 /f"), null),

            // time & language -> typing -> typing insights
            (@"Disabling ""Typing insights""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\input\Settings"" /v InsightsEnabled /t REG_DWORD /d 0 /f"), null),
                        
            // keyboard properties -> speed -> character repeat
            (@"Setting ""Repeat delay"" to ""Short""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Keyboard"" /v KeyboardDelay /t REG_SZ /d ""0"" /f"), null),

            // set desktop mode default over tablet mode
            ("Setting desktop mode as default over tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v SignInMode /t REG_DWORD /d 1 /f"), null),

            // disable tablet mode
            ("Disabling tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v TabletMode /t REG_DWORD /d 0 /f"), null),

            // disable tablet mode prompts and always switch
            ("Disabling tablet mode prompts and always switch", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v ConvertibleSlateModePromptPreference /t REG_DWORD /d 2 /f"), null),

            // enable taskbar when in tablet mode
            ("Enabling taskbar when in tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAppsVisibleInTabletMode /t REG_DWORD /d 1 /f"), null),

            // disable automatic hiding of the taskbar in tablet mode
            ("Disabling automatic hiding of the taskbar in tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAutoHideInTabletMode /t REG_DWORD /d 0 /f"), null),

            // disable automatic folder type discovery
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags"" /f"), null),
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU"" /f"), null),
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell"" /v FolderType /t REG_SZ /d ""NotSpecified"" /f"), null),

            // disable jpeg wallpaper compression
            ("Disabling JPEG wallpaper compression", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v JPEGImportQuality /t REG_DWORD /d 100 /f"), null),

            // disable tracking recent files
            ("Disabling tracking of recent files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_TrackDocs /t REG_DWORD /d 0 /f"), null),

            // explorer -> options -> privacy
            (@"Disabling ""Show recently used files""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer"" /v ShowRecent /t REG_DWORD /d 0 /f"), null),
            
            // explorer -> options -> privacy
            (@"Disabling ""Show frequently used folders""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer"" /v ShowFrequent /t REG_DWORD /d 0 /f"), null),
            
            // explorer -> options -> privacy
            (@"Disabling ""Show files from Office.com""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer"" /v ShowCloudFilesInQuickAccess /t REG_DWORD /d 0 /f"), null),

            // enable "clear history of recently opened documents on exit" policy
            (@"Enabling ""Clear history of recently opened documents on exit"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v ClearRecentDocsOnExit /t REG_DWORD /d 1 /f"), null),

            // disable recent office files
            (@"Enabling ""Turn off account-based insights, recent, favorite, and recommended files in File Explorer"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v DisableGraphRecentItems /t REG_DWORD /d 1 /f"), null),

            // enable "do not track shell shortcuts during roaming" policy
            (@"Enabling ""Do not track Shell shortcuts during roaming"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v LinkResolveIgnoreLinkInfo /t REG_DWORD /d 1 /f"), null),
            
            // enable "do not use tracking-based method when resolving shell shortcuts" policy
            (@"Enabling ""Do not use the tracking-based method when resolving shell shortcuts"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoResolveTrack /t REG_DWORD /d 1 /f"), null),

            // enable "more details" in file dialogs
            (@"Enabling ""More details"" in file dialogs", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager"" /v EnthusiastMode /t REG_DWORD /d 1 /f"), null),

            // disable "show extracted files when complete"
            (@"Disabling ""Show extracted files when complete""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\ExtractionWizard"" /v ShowFiles /t REG_DWORD /d 0 /f"), null),

            // remove "gallery" tab from file explorer
            (@"Removing ""Gallery"" tab from file explorer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"" /f"), null),

            // set ""mouse hover time" to 150ms
            (@"Setting ""Mouse hover time""to 150ms", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Mouse"" /v MouseHoverTime /t REG_SZ /d 150 /f"), null),

            // set "Menu show delay" to 150ms
            (@"Setting ""Mouse hover time"" to 150ms", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v MenuShowDelay /t REG_SZ /d 150 /f"), null),

            // disable telemetry
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v CLOUDSDK_CORE_DISABLE_PROMPTS /t REG_SZ /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v DOTNET_TRY_CLI_TELEMETRY_OPTOUT /t REG_SZ /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v DOTNET_CLI_TELEMETRY_OPTOUT /t REG_SZ /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v DOCKER_CLI_TELEMETRY_OPTOUT /t REG_SZ /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v npm_config_loglevel /t REG_SZ /d silent /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v POWERSHELL_TELEMETRY_OPTOUT /t REG_SZ /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v VS_TELEMETRY_OPT_OUT /t REG_SZ /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DiagTrack"" /v Start /t REG_DWORD /d 4 /f & sc stop DiagTrack"), null),
            (@"Enabling ""Limit Diagnostic Log Collection"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v LimitDiagnosticLogCollection /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Limit Dump Collection"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v LimitDumpCollection /t REG_DWORD /d 1 /f"), null),
            (@"Setting ""Limit optional diagnostic data for Desktop Analytics"" policy to ""Disable Desktop Analytics collection""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v LimitEnhancedDiagnosticDataWindowsAnalytics /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Do not show feedback notifications"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Allow device name to be sent in Windows diagnostic data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowDeviceNameInTelemetry /t REG_DWORD /d 0 /f"), null),
            (@"Setting ""Configure Authenticated Proxy usage for the Connected User Experience and Telemetry service"" policy to ""Disable Authenticated Proxy usage""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableEnterpriseAuthProxy /t REG_DWORD /d 1 /f"), null),
            (@"Setting ""Configure collection of browsing data for Desktop Analytics"" policy to ""Do not allow sending intranet or internet history""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\DataCollection"" /v MicrosoftEdgeDataOptIn /t REG_DWORD /d 0 /f"), null),
            (@"Setting ""Configure collection of browsing data for Desktop Analytics"" policy to ""Do not allow sending intranet or internet history""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v MicrosoftEdgeDataOptIn /t REG_DWORD /d 0 /f"), null),
            (@"Setting ""Configure diagnostic data opt-in change notifications"" policy to ""Disable diagnostic data change notifications""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableTelemetryOptInChangeNotification /t REG_DWORD /d 1 /f"), null),
            (@"Setting ""Configure diagnostic data opt-in settings user interface"" policy to ""Disable diagnostic data opt-in settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableTelemetryOptInSettingsUx /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Configure Watson events"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Reporting"" /v ""DisableGenericRePorts"" /t REG_DWORD /d 1 /f"), null),

            // disable windows error reporting
            (@"Enabling ""Disable Windows Error Reporting"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Disable Windows Error Reporting"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not send additional data"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\Windows Error Reporting"" /v LoggingDisabled /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Disable logging"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v LoggingDisabled /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not send additional data"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\Windows Error Reporting"" /v DontSendAdditionalData /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not send additional data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v DontSendAdditionalData /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Prevent display of the user interface for critical errors"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v DontShowUI /t REG_DWORD /d 1 /f"), null),
            (@"Setting ""Customize consent settings"" policy to 0", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Error Reporting\Consent"" /v 0 /t REG_SZ /d """" /f"), null),
            (@"Setting ""Customize consent settings"" policy to 0", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting\Consent"" /v 0 /t REG_SZ /d """" /f"), null),
            (@"Disabling ""Ignore custom consent settings"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Windows Error Reporting\Consent"" /v DefaultOverrideBehavior /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Ignore custom consent settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\Consent"" /v DefaultOverrideBehavior /t REG_DWORD /d 0 /f"), null),

            // disable customer experience improvement program
            ("Disabling Customer Experience Improvement Program (CEIP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQMClient\Windows"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off Windows Customer Experience Improvement Program"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SQMClient\Windows"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Microsoft Customer Experience Improvement Program (CEIP)"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\AppV\CEIP"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            ("Disabling Customer Experience Improvement Program (CEIP) for Visual Studio 2025", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VSCommon\18.0\SQM"" /v OptIn /t REG_DWORD /d 0 /f"), null),

            // disable messages to cloud services
            (@"Disabling ""Allow Message Service Cloud Sync"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Messaging"" /v AllowMessageSync /t REG_DWORD /d 0 /f"), null),

            // disable "allow online tips" policy
            (@"Disabling ""Allow Online Tips"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v AllowOnlineTips /t REG_DWORD /d 0 /f"), null),

            // enable "allow microsoft accounts to be optional" policy
            (@"Enabling ""Allow Microsoft accounts to be optional"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System"" /v MSAOptional /t REG_DWORD /d 1 /f"), null),

            // disable program compatibility assistant
            (@"Enabling ""Turn off Program Compatibility Assistant"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\AppCompat"" /v DisablePCA /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Program Compatibility Assistant"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisablePCA /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Steps Recorder"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableUAR /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Inventory Collector"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableInventory /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Application Telemetry"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v AITEnable /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off SwitchBack Compatibility Engine"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v SbEnable /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off Application Compatibility Engine"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableEngine /t REG_DWORD /d 1 /f"), null),

            // disable "allow remote assistance connections to this computer"
            (@"Disabling ""Allow Remote Assistance connection to this computer""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance"" /v fAllowToGetHelp /t REG_DWORD /d 0 /f"), null),

            // disable "fault tolerant heap" (fth)
            (@"Disabling ""Fault Tolerant Heap"" (FTH)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\FTH"" /v Enabled /t REG_DWORD /d 0 /f"), null),

            // disable settings sync
            (@"Enabling ""Do not sync"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync app settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableApplicationSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync app settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableApplicationSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync passwords"" policy""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableCredentialsSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync passwords"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableCredentialsSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync personalize"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisablePersonalizationSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync personalize"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisablePersonalizationSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync Apps"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableAppSyncSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync Apps"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableAppSyncSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync other Windows settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWindowsSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync other Windows settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWindowsSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync desktop personalization"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableDesktopThemeSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync desktop personalization"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableDesktopThemeSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync browser settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWebBrowserSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync browser settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWebBrowserSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync on metered connections"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSyncOnPaidNetwork /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync start settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableStartLayoutSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync start settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableStartLayoutSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync accessibility settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableAccessibilitySettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync accessibility settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableAccessibilitySettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Do not sync language preferences settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableLanguageSettingSync /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Do not sync language preferences settings"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableLanguageSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Accessibility"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Personalization"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\BrowserSettings"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Credentials"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Windows"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync"" /v SyncPolicy /t REG_DWORD /d 5 /f"), null),

            // set "set the default behavior for autorun" policy to "do not execute any autorun commands"
            (@"Setting ""Set the default behavior for AutoRun"" policy to ""Do not execute any autorun commands""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoAutorun /t REG_DWORD /d 1 /f"), null),
            
            // set "turn off autoplay" policy to "all drives"
            (@"Setting ""Turn off Autoplay"" policy to ""All drives""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f"), null),

            // reserve 10% of CPU resources to low-priority tasks
            ("Reserving 10% of CPU resources to low-priority tasks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 10 /f"), null),

            // set "win32priorityseparation" to 0x18/24
            (@"Setting ""Win32PrioritySeparation"" to 0x18/24", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 24 /f"), null),

            // setting "let apps run in the background" policy to "force deny"
            (@"Setting ""Let Windows apps run in the background"" policy to ""Force Deny""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"" /v LetAppsRunInBackground /t REG_DWORD /d 2 /f"), null),

            // disable autorun entries
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot"" /v ""AlternateShell"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot\AutorunsDisabled"" /v ""AlternateShell"" /t REG_SZ /d ""cmd.exe"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunPowerShell(@"Get-ScheduledTask | Where-Object {$_.TaskName -like 'UnlockStartLayout*'} | Unregister-ScheduledTask -Confirm:$false"), null),
            
            // enable windows spotlight
            ("Enabling Windows Spotlight", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\CloudContent"" /v ""DisableCloudOptimizedContent"" /f"), null),
            
            // disable search indexing
            ("Disabling Search Indexing", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch"" /v Start /t REG_DWORD /d 4 /f & sc stop WSearch"), null),

            // disable game bar presence writer
            ("Disabling GameBar Presence Writer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter"" /v ActivationType /t REG_DWORD /d 0 /f"), null)
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

            TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
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