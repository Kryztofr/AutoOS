using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class RegistryStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool? Desktop = PreparingStage.Desktop;
        bool? SSD = PreparingStage.SSD;
        bool? Rename = PreparingStage.Rename;

        InstallPage.Status.Text = "Configuring Registry...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        string edgeVersion = "";

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
            (@"Setting ""Feedback frequency"" to never", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules"" /v ""NumberOfSIUFInPeriod"" /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> activity history
            (@"Disabling ""Enables Activity Feed"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow publishing of User Activities"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v PublishUserActivities /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow upload of User Activities"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v UploadUserActivities /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> voice activation
            ("Disabling app access to voice activation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps"" /v ""AgentActivationOnLockScreenEnabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling app access to voice activation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps"" /v ""AgentActivationEnabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling app access to voice activation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps"" /v ""AgentActivationLastUsed"" /t REG_DWORD /d 0 /f"), null),

            // privacy & security -> notifications
            ("Disabling app access to notifications", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to notifications", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> account info
            ("Disabling app access to account info", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"" /v ""LetAppsAccessAccountInfo"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling app access to account info", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userAccountInformation"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to account info", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userAccountInformation"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> contacts
            ("Disabling app access to contacts", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\contacts"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to contacts", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\contacts"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> calendar
            ("Disabling app access to calendar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appointments"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to calendar", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appointments"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> phone calls
            ("Disabling app access to phone calls", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCall"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to phone calls", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCall"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> call history
            ("Disabling app access to call history", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCallHistory"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to call history", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCallHistory"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> email
            ("Disabling app access to email", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\email"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to email", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\email"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> tasks
            ("Disabling app access to tasks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userDataTasks"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to tasks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userDataTasks"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> messaging
            ("Disabling app access to messaging", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\chat"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to messaging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\chat"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> radios
            ("Disabling app access to radios", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\radios"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to radios", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\radios"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> other devices
            ("Disabling app access to other devices", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\bluetoothSync"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to other devices", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\bluetoothSync"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> app diagnostics
            ("Disabling app access to app diagnostics", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to app diagnostics", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> documents
            ("Disabling app access to documents", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to documents", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> download
            ("Disabling app access to downloads", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\downloadsFolder"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to downloads", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\downloadsFolder"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> music
            ("Disabling app access to music", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\musicLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to music", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\musicLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> pictures
            ("Disabling app access to pictures", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to pictures", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> videos
            ("Disabling app access to videos", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to videos", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // privacy & security -> file system
            ("Disabling app access to file system", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to file system", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // apps -> advanced app settings
            ("Disabling sharing across devices", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP"" /v ""RomeSdkChannelUserAuthzPolicy"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling sharing across devices", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP"" /v ""CdpSessionUserAuthzPolicy"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Archive apps""", async () => await ProcessActions.RunPowerShell(@"New-Item -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\InstallService\Stubification\$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"" -Force | Out-Null; New-ItemProperty -Path ""HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\InstallService\Stubification\$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"" -Name EnableAppOffloading -PropertyType DWord -Value 0 -Force"), null),

            // apps -> resume
            //("Disabling resume feature", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CrossDeviceResume\Configuration"" /v ""IsResumeAllowed"" /t REG_DWORD /d 0 /f"), null),

            // disable "sign-in and lock last interactive user automatically after a restart" policy
            (@"Disabling ""Sign-in and lock last interactive user automatically after a restart"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v DisableAutomaticRestartSignOn /t REG_DWORD /d 1 /f"), null),

            // disable automatic maintenance
            ("Disabling automatic maintenance", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance"" /v MaintenanceDisabled /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Configure Scheduled Maintenance Behavior"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\ScheduledDiagnostics"" /v EnabledExecution /t REG_DWORD /d 0 /f"), null),

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

            // disable fault tolerant heap
            ("Disabling Fault Tolerant Heap (FTH)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\FTH"" /v Enabled /t REG_DWORD /d 0 /f"), null),

            // disable customer experience improvement program
            ("Disabling Customer Experience Improvement Program (CEIP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQMClient\Windows"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off Windows Customer Experience Improvement Program"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SQMClient\Windows"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Microsoft Customer Experience Improvement Program (CEIP)"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\AppV\CEIP"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            ("Disabling Customer Experience Improvement Program (CEIP) for Visual Studio 2025", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VSCommon\18.0\SQM"" /v OptIn /t REG_DWORD /d 0 /f"), null),

            // disable messages to cloud services
            (@"Disabling ""Allow Message Service Cloud Sync"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Messaging"" /v AllowMessageSync /t REG_DWORD /d 0 /f"), null),

            // set desktop mode default over tablet mode
            ("Setting desktop mode as default over tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v SignInMode /t REG_DWORD /d 1 /f"), null),

            // disable tablet mode
            ("Disabling tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v TabletMode /t REG_DWORD /d 0 /f"), null),

            // disable tablet mode prompts and always switch
            ("Disabling tablet mode prompts and always switch", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v ConvertibleSlateModePromptPreference /t REG_DWORD /d 2 /f"), null),

            // enable taskbar in tablet mode
            ("Enabling taskbar when in tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAppsVisibleInTabletMode /t REG_DWORD /d 1 /f"), null),

            // disable automatic hiding of the taskbar in tablet mode
            ("Disabling automatic hiding of the taskbar in tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAutoHideInTabletMode /t REG_DWORD /d 0 /f"), null),

            // enable windows spotlight
            ("Enabling Windows Spotlight", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\CloudContent"" /v ""DisableCloudOptimizedContent"" /f"), null),

            // disable program compatibility assistant
            (@"Enabling ""Turn off Program Compatibility Assistant"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\AppCompat"" /v DisablePCA /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Program Compatibility Assistant"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisablePCA /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Steps Recorder"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableUAR /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Inventory Collector"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableInventory /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Turn off Application Telemetry"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v AITEnable /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off SwitchBack Compatibility Engine"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v SbEnable /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off Application Compatibility Engine"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableEngine /t REG_DWORD /d 1 /f"), null),

            // disable game bar
            ("Disabling GameBar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AppCaptureEnabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling GameBar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\System\GameConfigStore"" /v ""GameDVR_Enabled"" /t REG_DWORD /d 0 /f"), null),

            // disable game bar presence writer
            ("Disabling the GameBar Presence Writer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter"" /v ActivationType /t REG_DWORD /d 0 /f"), null),

            // configure presentation modes
            ("Configuring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_HonorUserFSEBehaviorMode"" /t REG_DWORD /d 1 /f"), null),
            ("Configuring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_DXGIHonorFSEWindowsCompatible"" /t REG_DWORD /d 1 /f"), null),
            ("Configuring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_FSEBehavior"" /t REG_DWORD /d 2 /f"), null),
            ("Confiugring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm"" /v ""OverlayTestMode"" /t REG_DWORD /d 5 /f"), null),

            // disable remote assistance
            ("Disabling remote assistance", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance"" /v fAllowToGetHelp /t REG_DWORD /d 0 /f"), null),

            // disable search indexing
            ("Disabling search indexing", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch"" /v Start /t REG_DWORD /d 4 /f & sc stop WSearch"), null),

            // disable automatic folder type discovery
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags"" /f"), null),
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU"" /f"), null),
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell"" /v FolderType /t REG_SZ /d ""NotSpecified"" /f"), null),

            // disable jpeg wallpaper compression
            ("Disabling JPEG wallpaper compression", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v JPEGImportQuality /t REG_DWORD /d 100 /f"), null),

            // disable tracking recent files
            ("Disabling tracking of recent files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_TrackDocs /t REG_DWORD /d 0 /f"), null),

            // disable show frequently used folders
            ("Disabling show frequently used folders", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer"" /v ShowFrequent /t REG_DWORD /d 0 /f"), null),

            // enable clear recent files on exit
            ("Enabling clear recent files on exit", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v ClearRecentDocsOnExit /t REG_DWORD /d 1 /f"), null),

            // disable recent office files
            ("Disabling recent office files", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v DisableGraphRecentItems /t REG_DWORD /d 1 /f"), null),

            // disable shortcut text creation
            ("Disabling shortcut text creation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v link /t REG_BINARY /d 00000000 /f"), null),

            // disable shortcut tracking
            ("Disabling shortcut tracking", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v LinkResolveIgnoreLinkInfo /t REG_DWORD /d 1 /f"), null),
            ("Disabling shortcut tracking", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoResolveTrack /t REG_DWORD /d 1 /f"), null),

            // enable show more details in copy dialog
            ("Enabling show more details in copy dialog", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager"" /v EnthusiastMode /t REG_DWORD /d 1 /f"), null),

            // disable show extracted files when complete
            ("Disable showing extracted files when complete", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\ExtractionWizard"" /v ShowFiles /t REG_DWORD /d 0 /f"), null),

            // hide gallery from file explorer
            ("Hiding gallery from file explorer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"" /f"), null),

            // disable autoplay
            ("Disabling autoplay", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v NoAutoplayfornonVolume /t REG_DWORD /d 1 /f"), null),
            ("Disabling autoplay", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers"" /v DisableAutoplay /t REG_DWORD /d 1 /f"), null),

            // disable autorun from disks
            (@"Setting ""Set the default behavior for AutoRun"" policy to ""Do not execute any autorun commands""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoAutorun /t REG_DWORD /d 1 /f"), null),
            (@"Setting ""Turn off Autoplay"" policy to ""All drives""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f"), null),
            (@"Setting ""Turn off Autoplay"" policy to ""All drives""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f"), null),

            // reduce mouse hover time on tooltips
            ("Reducing mouse hover time on tooltips", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Mouse"" /v MouseHoverTime /t REG_SZ /d 150 /f"), null),

            // reduce delay when opening submenus
            ("Reducing delay when opening submenus", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v MenuShowDelay /t REG_SZ /d 150 /f"), null),

            // disable save your work prompt
            ("Disabling save your work prompt", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v AutoEndTasks /t REG_SZ /d 1 /f"), null),
            ("Disabling save your work prompt", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v AutoEndTasks /t REG_SZ /d 1 /f"), null),

            // reduce delay to end tasks on shutdown screen
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v HungAppTimeout /t REG_SZ /d 2000 /f"), null),
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v HungAppTimeout /t REG_SZ /d 2000 /f"), null),
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f"), null),
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f"), null),

            // reduce delay for low-level hooks
            ("Reducing delay for low-level hooks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v LowLevelHooksTimeout /t REG_SZ /d 1000 /f"), null),
            ("Reducing delay for low-level hooks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v LowLevelHooksTimeout /t REG_SZ /d 1000 /f"), null),

            // reduce delay to end services on shutdown
            ("Reducing delay to end services on shutdown", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control"" /v WaitToKillServiceTimeout /t REG_SZ /d 1500 /f"), null),

            // disable telemetry
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx POWERSHELL_TELEMETRY_OPTOUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx DOTNET_TRY_CLI_TELEMETRY_OPTOUT 1   "), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx CLOUDSDK_CORE_DISABLE_PROMPTS 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx DOTNET_CLI_TELEMETRY_OPTOUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx DOCKER_CLI_TELEMETRY_OPTOUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx VS_TELEMETRY_OPT_OUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx npm_config_loglevel silent   "), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v POWERSHELL_TELEMETRY_OPTOUT /t REG_SZ /d 1 /f"), null),
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

            // disable "allow diagnostic data" policy
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\DataCollection"" /v ""AllowTelemetry"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v MaxTelemetryAllowed /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v MaxTelemetryAllowed /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\System\AllowTelemetry"" /v value /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow Diagnostic Data"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CPSS\DevicePolicy\AllowTelemetry"" /t DefaultValue /d 0 /f"), null),

            // disable diagnostic data viewer
            ("Disabling Diagnostic Data Viewer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack\EventTranscriptKey"" /v EnableEventTranscript /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Disable diagnostic data viewer"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableDiagnosticDataViewer /t REG_DWORD /d 1 /f"), null),

            // disable "allow online tips" policy
            (@"Disabling ""Allow Online Tips"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v AllowOnlineTips /t REG_DWORD /d 0 /f"), null),

            // disable wifi telemetry
            (@"Disabling ""Allow Windows to automatically connect to suggested open hotspots, to networks shared by contacts, and to hotspots offering paid services"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config"" /v AutoConnectAllowedOEM /t REG_DWORD /d 0 /f"), null),
            ("Disabling WiFi telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting"" /v value /t REG_DWORD /d 0 /f"), null),
            ("Disabling WiFi telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots"" /v value /t REG_DWORD /d 0 /f"), null),

            // enable "allow microsoft accounts to be optional" policy
            (@"Enabling ""Allow Microsoft accounts to be optional"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System"" /v MSAOptional /t REG_DWORD /d 1 /f"), null),

            // optimize microsoft edge settings
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v UserFeedbackAllowed /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutofillCreditCardEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeCollectionsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ShowMicrosoftRewards /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeShoppingAssistantEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PersonalizationReportingEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ShowRecommendationsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v WebWidgetAllowed /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v MathSolverEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v InAppSupportEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageSearchBox /t REG_SZ /d ""redirect"" /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v TyposquattingCheckerEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SiteSafetyServicesEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AlternateErrorPagesEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutofillAddressEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ConfigureDoNotTrack /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PaymentMethodQueryEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PromotionalTabsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v VisualSearchEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutoImportAtFirstRun /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BlockExternalExtensions /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v FamilySafetySettingsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordGeneratorEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordManagerEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordMonitorAllowed /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ResolveNavigationErrorsUseWebService /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeFollowEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AllowGamesMenu /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BlockThirdPartyCookies /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v DefaultNotificationsSetting /t REG_DWORD /d 2 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v DefaultGeolocationSetting /t REG_DWORD /d 2 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AccessibilityImageLabelsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v QuickSearchShowMiniMenu /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v HubsSidebarEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v MicrosoftEdgeInsiderPromotionEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v WindowOcclusionEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SpotlightExperiencesAndRecommendationsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageAppLauncherEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageContentEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AddressBarMicrosoftSearchInBingProviderEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SearchInSidebarEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\EdgeUpdate"" /v CreateDesktopShortcutDefault /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off tracking of app usage"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableMFUTracking /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Disable help tips"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableHelpSticker /t REG_DWORD /d 1 /f"), null),
            (@"Enabling ""Disable help tips"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableHelpSticker /t REG_DWORD /d 1 /f"), null),

            // disable typing accesibility settings
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableSpellchecking /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableSpellchecking /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnablePredictionSpaceInsertion /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnablePredictionSpaceInsertion /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableDoubleTapSpace /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableDoubleTapSpace /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v InsightsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v InsightsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v MultilingualEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v MultilingualEnabled /t REG_DWORD /d 0 /f"), null),

            // disable typing insights
            ("Disabling typing insights", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\input\Settings"" /v InsightsEnabled /t REG_DWORD /d 0 /f"), null),

            // increase keyboard repeat rate
            ("Increasing keyboard repeat rate", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Keyboard"" /v InitialKeyboardIndicators /t REG_SZ /d ""0"" /f"), null),
            ("Increasing keyboard repeat rate", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Keyboard"" /v KeyboardDelay /t REG_SZ /d ""0"" /f"), null),
            ("Increasing keyboard repeat rate", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Keyboard"" /v KeyboardSpeed /t REG_SZ /d ""31"" /f"), null),

            // enable clipboard history
            ("Enabling clipboard history", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Clipboard"" /v EnableClipboardHistory /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Allow Clipboard synchronization across devices"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f"), null),

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

            // disable disk space checks
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoLowDiskSpaceChecks /t REG_DWORD /d 1 /f"), null),
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager"" /v BootExecute /t REG_MULTI_SZ /d ""autocheck autochk /k:C*"" /f"), null),
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Dfrg\BootOptimizeFunction"" /v Enable /t REG_SZ /d ""N"" /f"), null),
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\OptimalLayout"" /v EnableAutoLayout /t REG_DWORD /d 0 /f"), null),

            // reserve 10% of CPU resources to low-priority tasks instead of 20%
            ("Reserving 10% of CPU resources to low-priority tasks instead of 20%", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 10 /f"), null),

            // set "win32priorityseparation" to 0x28/40
            (@"Setting ""Win32PrioritySeparation"" to 0x28/40", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 40 /f"), null),

            // setting "let apps run in the background" policy to "force deny"
            (@"Setting ""Let Windows apps run in the background"" policy to ""Force Deny""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"" /v LetAppsRunInBackground /t REG_DWORD /d 2 /f"), null),

            // disable autorun entries
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot"" /v ""AlternateShell"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot\AutorunsDisabled"" /v ""AlternateShell"" /t REG_SZ /d ""cmd.exe"" /f"), null),

            ("Disabling Autorun entries", async () => edgeVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")).ProductVersion), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v """" /t REG_SZ /d ""Microsoft Edge"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""Localized Name"" /t REG_SZ /d ""Microsoft Edge"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{9459C573-B17A-45AE-9F64-1857B5D58CEE}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\Program Files (x86)\Microsoft\Edge\Application\{edgeVersion}\Installer\setup.exe\"" --configure-user-settings --verbose-logging --system-level --msedge --channel=stable"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""ComponentID"" /t REG_SZ /d ""DOTNETFRAMEWORKS"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""DontAsk"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""Enabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""StubPath"" /t REG_SZ /d ""C:\Windows\System32\Rundll32.exe C:\Windows\System32\mscories.dll,Install"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""ComponentID"" /t REG_SZ /d ""DOTNETFRAMEWORKS"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""DontAsk"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""Enabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""StubPath"" /t REG_SZ /d ""C:\Windows\SysWOW64\Rundll32.exe C:\Windows\SysWOW64\mscories.dll,Install"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v """" /t REG_SZ /d ""IEToEdge BHO"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v ""NoExplorer"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v """" /t REG_SZ /d ""IEToEdge BHO"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v ""NoExplorer"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunPowerShell(@"Get-ScheduledTask | Where-Object {$_.TaskName -like 'UnlockStartLayout*'} | Unregister-ScheduledTask -Confirm:$false"), null),

            // disable potentially unwanted windows programs
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\AggregatorHost.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BCILauncher.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BGAUpsell.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BingChatInstaller.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DeviceCensus.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FeatureLoader.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),

            // disable unnecessary services
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DPS"" /v Start /t REG_DWORD /d 4 /f & sc stop DPS"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\diagnosticshub.standardcollector.service"" /v Start /t REG_DWORD /d 4 /f & sc stop diagnosticshub.standardcollector.service"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OneSyncSvc"" /v Start /t REG_DWORD /d 4 /f & sc stop OneSyncSvc"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PcaSvc"" /v Start /t REG_DWORD /d 4 /f & sc stop PcaSvc"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiServiceHost"" /v Start /t REG_DWORD /d 4 /f & sc stop WdiServiceHost"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiSystemHost"" /v Start /t REG_DWORD /d 4 /f & sc stop WdiSystemHost"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Wecsvc"" /v Start /t REG_DWORD /d 4 /f & sc stop Wecsvc"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WerSvc"" /v Start /t REG_DWORD /d 4 /f & sc stop WerSvc"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wisvc"" /v Start /t REG_DWORD /d 4 /f & sc stop wisvc"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\SysMain"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop SysMain"), () => SSD == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdate"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop edgeupdate"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdatem"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop edgeupdatem"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\FontCache3.0.0.0"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop FontCache3.0.0.0"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\MicrosoftEdgeElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop MicrosoftEdgeElevationService"), null),
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