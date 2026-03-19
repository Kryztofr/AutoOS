using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using WinRT.Interop;
using AutoOS.Helpers.Store;
using AutoOS.Helpers.Registry;
using AutoOS.Helpers.Services;
using Microsoft.Win32;
using Windows.Storage;
using AutoOS.Helpers.TaskScheduler;

namespace AutoOS.Views.Installer.Stages;

public static class BrowsersStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool? Chrome = PreparingStage.Chrome;
        bool? Thorium = PreparingStage.Thorium;
        bool? Brave = PreparingStage.Brave;
        bool? Vivaldi = PreparingStage.Vivaldi;
        bool? Arc = PreparingStage.Arc;
        bool? Comet = PreparingStage.Comet;
        bool? Firefox = PreparingStage.Firefox;
        bool? Zen = PreparingStage.Zen;
        bool? uBlock = PreparingStage.uBlock;
        bool? SponsorBlock = PreparingStage.SponsorBlock;
        bool? ReturnYouTubeDislike = PreparingStage.ReturnYouTubeDislike;
        bool? Cookies = PreparingStage.Cookies;
        bool? DarkReader = PreparingStage.DarkReader;
        bool? Violentmonkey = PreparingStage.Violentmonkey;
        bool? Tampermonkey = PreparingStage.Tampermonkey;
        bool? Shazam = PreparingStage.Shazam;
        bool? iCloud = PreparingStage.iCloud;
        bool? Bitwarden = PreparingStage.Bitwarden;
        bool? OnePassword = PreparingStage.OnePassword;

        InstallPage.Status.Text = "Configuring Browsers...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        string edgeVersion = "";
        string chromeVersion = "";
        string chromeVersion2 = "";
        string thoriumVersion = "";
        string braveVersion = "";
        string vivaldiVersion = "";
        string arcVersion = "";
        string cometVersion = "";

        using HttpClient client = new();
        string firefoxVersion = JsonDocument.Parse(await client.GetStringAsync("https://product-details.mozilla.org/1.0/firefox_versions.json")).RootElement.GetProperty("LATEST_FIREFOX_VERSION").GetString();

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // optimize microsoft edge settings
            (@"Enabling ""Configure Do Not Track"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "ConfigureDoNotTrack", 1, RegistryValueKind.DWord), null),
            (@"Disabling ""Shopping in Microsoft Edge Enabled"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "EdgeShoppingAssistantEnabled", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Allow Microsoft content on the new tab page"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "NewTabPageContentEnabled", 0, RegistryValueKind.DWord), null),
            (@"Disbaling ""Allow user feedback"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "UserFeedbackAllowed", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Create Desktop Shortcut upon install default"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\EdgeUpdate", "CreateDesktopShortcutDefault", 0, RegistryValueKind.DWord), null),
            (@"Enabling ""Turn off tracking of app usage"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI", "DisableMFUTracking", 1, RegistryValueKind.DWord), null),

            // disable microsoft edge services
            ("Disabling Microsoft Edge services", async () => edgeVersion = FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")).ProductVersion, null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "", "Microsoft Edge", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "Localized Name", "Microsoft Edge", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "StubPath", $@"""C:\Program Files (x86)\Microsoft\Edge\Application\{edgeVersion}\Installer\setup.exe"" --configure-user-settings --verbose-logging --system-level --msedge --channel=stable", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "Version", "43,0,0,0", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", "IsInstalled", 1, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "ComponentID", "DOTNETFRAMEWORKS", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "DontAsk", 2, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "Enabled", 0, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "IsInstalled", 1, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "StubPath", @"C:\Windows\System32\Rundll32.exe C:\Windows\System32\mscories.dll,Install", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "ComponentID", "DOTNETFRAMEWORKS", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "DontAsk", 2, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "Enabled", 0, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "IsInstalled", 1, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}", "StubPath", @"C:\Windows\SysWOW64\Rundll32.exe C:\Windows\SysWOW64\mscories.dll,Install", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "", "IEToEdge BHO", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "NoExplorer", 1, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", "{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "", "IEToEdge BHO", RegistryValueKind.String), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "NoExplorer", 1, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", "{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdate", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => ServicesHelper.StopService("edgeupdate"), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdatem", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => ServicesHelper.StopService("edgeupdatem"), null),
            ("Disabling Microsoft Edge services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\MicrosoftEdgeElevationService", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling Microsoft Edge services", async () => ServicesHelper.StopService("MicrosoftEdgeElevationService"), null),
            ("Disabling Microsoft Edge services", async () => TaskSchedulerHelper.Toggle("MicrosoftEdgeUpdateTaskMachineCore", false), null),
            ("Disabling Microsoft Edge services", async () => TaskSchedulerHelper.Toggle("MicrosoftEdgeUpdateTaskMachineUA", false), null),

            // download google chrome
            ("Downloading Google Chrome", async () => await ProcessActions.RunDownload("http://dl.google.com/chrome/install/375.126/chrome_installer.exe", ApplicationData.Current.TemporaryFolder.Path, "ChromeSetup.exe"), () => Chrome == true),

            // install google chrome
            ("Installing Google Chrome", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "ChromeSetup.exe"), Arguments = "--silent --install --system-level --do-not-launch-chrome", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("ChromeSetup.exe")).DeleteAsync(); }, () => Chrome == true),
            ("Installing Google Chrome", async () => chromeVersion = FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "ChromeSetup.exe")).ProductVersion, () => Chrome == true),
            ("Installing Google Chrome", async () => chromeVersion2 = FileVersionInfo.GetVersionInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe").ProductVersion, () => Chrome == true),

            // pin google chrome to the taskbar
            ("Pinning Google Chrome to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Google Chrome.lnk"""), () => Chrome == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Chrome == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Chrome == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing ReturnYouTubeDislike Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'gebbhagfogifgggkldgodflihgfeippi' /f"), () => Chrome == true && ReturnYouTubeDislike == true),

            // install i still dont care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Chrome == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Chrome == true && DarkReader == true),
            
            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'jinjaccalgkegednnccohejagnlnfdag' /f"), () => Chrome == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'dhdgffkkebhmkfjojejmpbldmpobfkfo' /f"), () => Chrome == true && Tampermonkey == true),

            // install shazam extension
            ("Installing Shazam Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Chrome == true && Shazam == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Chrome == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Chrome == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Chrome == true && OnePassword == true),

            // log in to google chrome
            ("Please log in to your Google Chrome account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Google\Chrome\Application", "chrome.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => Chrome == true),

            // remove google chrome shortcut from the desktop
            ("Removing Google Chrome shortcut from the desktop", async () => File.Delete(@"C:\Users\Public\Desktop\Google Chrome.lnk"), () => Chrome == true),

            // disable google chrome services
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleChromeElevationService", "Start", 4, RegistryValueKind.DWord), () => Chrome == true),
            ("Disabling Google Chrome services", async () => ServicesHelper.StopService("GoogleChromeElevationService"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterInternalService{chromeVersion}", "Start", 4, RegistryValueKind.DWord), () => Chrome == true),
            ("Disabling Google Chrome services", async () => ServicesHelper.StopService($@"GoogleUpdaterInternalService{chromeVersion}"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterService{chromeVersion}", "Start", 4, RegistryValueKind.DWord), () => Chrome == true),
            ("Disabling Google Chrome services", async () => ServicesHelper.StopService($@"GoogleUpdaterService{chromeVersion}"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "", "Google Chrome", RegistryValueKind.String), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "Localized Name", "Google Chrome", RegistryValueKind.String), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{8A69D345-D564-463c-AFF1-A69D9E530F96}}", "StubPath", $@"""C:\Program Files\Google\Chrome\Application\{chromeVersion2}\Installer\chrmstp.exe"" --configure-user-settings --verbose-logging --system-level --channel=stable", RegistryValueKind.String), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "Version", "43,0,0,0", RegistryValueKind.String), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}", "IsInstalled", 1, RegistryValueKind.DWord), () => Chrome == true),
            ("Disabling Google Chrome services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{8A69D345-D564-463c-AFF1-A69D9E530F96}"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => TaskSchedulerHelper.Toggle("GoogleUpdaterTaskSystem", false), () => Chrome == true),

            // download thorium
            ("Downloading Thorium", async () => await ProcessActions.RunDownload("https://github.com/Alex313031/Thorium-Win/releases/download/M130.0.6723.174/thorium_SSE4_mini_installer.exe", ApplicationData.Current.TemporaryFolder.Path, "ThoriumSetup.exe"), () => Thorium == true),

            // install thorium
            ("Installing Thorium", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "ThoriumSetup.exe"), Arguments = "--silent --install --system-level --do-not-launch-chrome", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("ThoriumSetup.exe")).DeleteAsync(); }, () => Thorium == true),
            ("Installing Thorium", async () => thoriumVersion = FileVersionInfo.GetVersionInfo(@"C:\Program Files\Thorium\Application\thorium.exe").ProductVersion, () => Thorium == true),

            // disable thorium services
            ("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "", "Thorium", RegistryValueKind.String), () => Thorium == true),
            ("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "Localized Name", "Thorium", RegistryValueKind.String), () => Thorium == true),
            ("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}}", "StubPath", $@"""C:\Program Files\Thorium\Application\{thoriumVersion}\Installer\chrmstp.exe"" --configure-user-settings --verbose-logging --system-level", RegistryValueKind.String), () => Thorium == true),
            ("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "Version", "43,0,0,0", RegistryValueKind.String), () => Thorium == true),
            ("Disabling Thorium services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}", "IsInstalled", 1, RegistryValueKind.DWord), () => Thorium == true),
            ("Disabling Thorium services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}"), () => Thorium == true),

            // pin thorium to the taskbar
            ("Pinning Thorium to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Thorium.lnk"""), () => Thorium == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Thorium == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Thorium == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing ReturnYouTubeDislike Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'gebbhagfogifgggkldgodflihgfeippi' /f"), () => Thorium == true && ReturnYouTubeDislike == true),

            // install i still dont care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Thorium == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Thorium == true && DarkReader == true),
            
            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'jinjaccalgkegednnccohejagnlnfdag' /f"), () => Thorium == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'dhdgffkkebhmkfjojejmpbldmpobfkfo' /f"), () => Thorium == true && Tampermonkey == true),

            // install shazam extension
            ("Installing Shazam Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Thorium == true && Shazam == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Thorium == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Thorium == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Thorium == true && OnePassword == true),

            // log in to thorium
            ("Please log in to your Thorium account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Thorium\Application", "thorium.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => Thorium == true),

            // remove thorium shortcut from the desktop
            ("Removing Thorium shortcut from the desktop", async () => File.Delete(@"C:\Users\Public\Desktop\Thorium.lnk"), () => Thorium == true),

            // download brave
            ("Downloading Brave", async () => await ProcessActions.RunDownload("https://github.com/brave/brave-browser/releases/latest/download/BraveBrowserStandaloneSetup.exe", ApplicationData.Current.TemporaryFolder.Path, "BraveBrowserStandaloneSetup.exe"), () => Brave == true),

            // install brave
            ("Installing Brave", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "BraveBrowserStandaloneSetup.exe"), Arguments = "/silent /install", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("BraveBrowserStandaloneSetup.exe")).DeleteAsync(); }, () => Brave == true),
            ("Installing Brave", async () => braveVersion = FileVersionInfo.GetVersionInfo(@"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe").ProductVersion, () => Brave == true),

            // pin brave to the taskbar
            ("Pinning Brave to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Brave.lnk"""), () => Brave == true),

            // remove brave shortcut from the desktop
            ("Removing Brave shortcut from the desktop", async () => File.Delete(@"C:\Users\Public\Desktop\Brave.lnk"), () => Brave == true),

            // optimize brave settings
            //("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "initial_preferences"), @"C:\Program Files\BraveSoftware\Brave-Browser\Application\initial_preferences", true)), () => Brave == true),
            //("Optimizing Brave settings", async () => await Task.Run(() => Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data"))), () => Brave == true),
            //("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "Local State"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data", "Local State"), true)), () => Brave == true),

            // disable brave services
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\brave", "Start", 4, RegistryValueKind.DWord), () => Brave == true),
            ("Disabling Brave services", async () => ServicesHelper.StopService("brave"), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\BraveElevationService", "Start", 4, RegistryValueKind.DWord), () => Brave == true),
            ("Disabling Brave services", async () => ServicesHelper.StopService("BraveElevationService"), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\bravem", "Start", 4, RegistryValueKind.DWord), () => Brave == true),
            ("Disabling Brave services", async () => ServicesHelper.StopService("bravem"), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "", "Brave", RegistryValueKind.String), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "Localized Name", "Brave", RegistryValueKind.String), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}}", "StubPath", $@"""C:\Program Files\BraveSoftware\Brave-Browser\Application\{braveVersion}\Installer\chrmstp.exe"" --configure-user-settings --verbose-logging --system-level", RegistryValueKind.String), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "Version", "43,0,0,0", RegistryValueKind.String), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}", "IsInstalled", 1, RegistryValueKind.DWord), () => Brave == true),
            ("Disabling Brave services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"), () => Brave == true),
            ("Disabling Brave services", async () => TaskSchedulerHelper.Toggle("BraveSoftwareUpdateTaskMachine", false), () => Brave == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Brave == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Brave == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing Return YouTube Dislike Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'gebbhagfogifgggkldgodflihgfeippi' /f"), () => Brave == true && ReturnYouTubeDislike == true),

            // install i still dont care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Brave == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Brave == true && DarkReader == true),

            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'jinjaccalgkegednnccohejagnlnfdag' /f"), () => Brave == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'dhdgffkkebhmkfjojejmpbldmpobfkfo' /f"), () => Brave == true && Tampermonkey == true),

            // install shazam extension
            ("Installing Shazam Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Brave == true && Shazam == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Brave == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Brave == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\BraveSoftware\Brave\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Brave == true && OnePassword == true),

            // download vivaldi
            ("Downloading Vivaldi", async () => await ProcessActions.RunDownload("https://vivaldi.com/download/Vivaldi.x64.exe", ApplicationData.Current.TemporaryFolder.Path, "Vivaldi.x64.exe"), () => Vivaldi == true),

            // install vivaldi
            ("Installing Vivaldi", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Vivaldi.x64.exe"), Arguments = "--vivaldi-silent --do-not-launch-chrome --system-level", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Vivaldi.x64.exe")).DeleteAsync(); }, () => Vivaldi == true),
            ("Installing Vivaldi", async () => vivaldiVersion = FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files\Vivaldi\Application\vivaldi.exe")).ProductVersion, () => Vivaldi == true),

            // pin vivaldi to the taskbar
            ("Pinning Vivaldi to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Vivaldi.lnk"""), () => Vivaldi == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Vivaldi == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Vivaldi == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing ReturnYouTubeDislike Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'gebbhagfogifgggkldgodflihgfeippi' /f"), () => Vivaldi == true && ReturnYouTubeDislike == true),

            // install i still dont care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Vivaldi == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Vivaldi == true && DarkReader == true),
            
            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'jinjaccalgkegednnccohejagnlnfdag' /f"), () => Vivaldi == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'dhdgffkkebhmkfjojejmpbldmpobfkfo' /f"), () => Vivaldi == true && Tampermonkey == true),

            // install shazam extension
            ("Installing Shazam Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Vivaldi == true && Shazam == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Vivaldi == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Vivaldi == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Vivaldi\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Vivaldi == true && OnePassword == true),

            // remove vivaldi shortcut from the desktop
            ("Removing Vivaldi shortcut from the desktop", async () => File.Delete(@"C:\Users\Public\Desktop\Vivaldi.lnk"), () => Vivaldi == true),

            // disable vivaldi services
            ("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "", "Vivaldi", RegistryValueKind.String), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "Localized Name", "Vivaldi", RegistryValueKind.String), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{9C142C0C-124C-4467-B117-EBCC62801D7B}}", "StubPath", $@"""C:\Program Files\Vivaldi\Application\{vivaldiVersion}\Installer\chrmstp.exe"" --configure-user-settings --verbose-logging --system-level --vivaldi-install-dir=""C:\Program Files\Vivaldi\""", RegistryValueKind.String), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "Version", "43,0,0,0", RegistryValueKind.String), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}", "IsInstalled", 1, RegistryValueKind.DWord), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9C142C0C-124C-4467-B117-EBCC62801D7B}"), () => Vivaldi == true),

            // download arc dependency
            ("Downloading Arc Dependency", async () => await ProcessActions.RunDownload("https://releases.arc.net/windows/dependencies/x64/Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx", ApplicationData.Current.TemporaryFolder.Path, "Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx"), () => Arc == true),

            // install arc dependency
            ("Installing Arc Dependency", async () => { await Process.Start(new ProcessStartInfo { FileName = "powershell", Arguments = @$"-Command ""Add-AppxPackage -Path {Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx")}""", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx")).DeleteAsync(); }, () => Arc == true),

            // download arc
            ("Downloading Arc", async () => await ProcessActions.RunDownload("https://releases.arc.net/windows/prod/1.72.0.296/Arc.x64.msix", ApplicationData.Current.TemporaryFolder.Path, "Arc.x64.msix"), () => Arc == true),

            // install arc
            ("Installing Arc", async () => { await Process.Start(new ProcessStartInfo { FileName = "powershell", Arguments = $"-Command \"Add-AppxPackage -Path {Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Arc.x64.msix")}\"", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Arc.x64.msix")).DeleteAsync(); }, () => Arc == true),
            ("Installing Arc", async () => arcVersion = StoreHelper.GetVersion("TheBrowserCompany.Arc_ttt1ap7aakyb4"), () => Arc == true),

            // pin arc to the taskbar
            ("Pinning Arc to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path TheBrowserCompany.Arc_ttt1ap7aakyb4!Arc"), () => Arc == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Arc == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Arc == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing ReturnYouTubeDislike Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'gebbhagfogifgggkldgodflihgfeippi' /f"), () => Arc == true && ReturnYouTubeDislike == true),

            // install i still dont care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Arc == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Arc == true && DarkReader == true),
            
            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'jinjaccalgkegednnccohejagnlnfdag' /f"), () => Arc == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'dhdgffkkebhmkfjojejmpbldmpobfkfo' /f"), () => Arc == true && Tampermonkey == true),

            // install shazam extension
            ("Installing Shazam Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Arc == true && Shazam == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Arc == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Arc == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Arc == true && OnePassword == true),

            // log in
            ("Please log in to your Arc account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\TheBrowserCompany.Arc_" + arcVersion + @"_x64__ttt1ap7aakyb4", "Arc.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => Arc == true),

            // download comet
            ("Downloading Comet", async () => await ProcessActions.RunDownload("https://www.perplexity.ai/rest/browser/download?platform=win_x64&channel=stable", ApplicationData.Current.TemporaryFolder.Path, "Comet.exe"), () => Comet == true),

            // install comet
            ("Installing Comet", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Comet.exe"), Arguments = "-silent --do-not-launch-chrome --system-level", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Comet.exe")).DeleteAsync(); }, () => Comet == true),
            ("Installing Comet", async () => cometVersion = FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files\Perplexity\Comet\Application\comet.exe")).ProductVersion, () => Comet == true),

            // pin comet to the taskbar
            ("Pinning Comet to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Comet.lnk"""), () => Comet == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'cjpalhdlnbpafiamejdnhcphjbkeiagm' /f"), () => Comet == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mnjggcdmjocbbbhaepdhchncahnbgone' /f"), () => Comet == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing ReturnYouTubeDislike Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'gebbhagfogifgggkldgodflihgfeippi' /f"), () => Comet == true && ReturnYouTubeDislike == true),

            // install i still dont care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'edibdbjcniadpccecjdfdjjppcpchdlm' /f"), () => Comet == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'eimadpbcbfnmbkopoojfekhnkhdbieeh' /f"), () => Comet == true && DarkReader == true),
            
            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'jinjaccalgkegednnccohejagnlnfdag' /f"), () => Comet == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'dhdgffkkebhmkfjojejmpbldmpobfkfo' /f"), () => Comet == true && Tampermonkey == true),

            // install shazam extension
            ("Installing Shazam Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'mmioliijnhnoblpgimnlajmefafdfilb' /f"), () => Comet == true && Shazam == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'pejdijmoenmkgeppbflobdenhhabjlaj' /f"), () => Comet == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'nngceckbapebfimnlniiiahkandclblb' /f"), () => Comet == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await ProcessActions.RunPowerShell(@"$BaseKey = 'HKLM:\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist'; $Index = (Get-Item $BaseKey).Property | Sort-Object {[int]$_} | Select-Object -Last 1; $NewIndex = [int]$Index + 1; reg add 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Chromium\ExtensionInstallForcelist' /v $NewIndex /t REG_SZ /d 'aeblfdkhhhdcdjpifhhbdiojplfjncoa' /f"), () => Comet == true && OnePassword == true),

            // remove comet shortcut from the desktop
            ("Removing Comet shortcut from the desktop", async () => File.Delete(@"C:\Users\Public\Desktop\Comet.lnk"), () => Comet == true),

            // disable comet services
            ("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "", "Comet", RegistryValueKind.String), () => Comet == true),
            ("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "Localized Name", "Comet", RegistryValueKind.String), () => Comet == true),
            ("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}}", "StubPath", $@"""C:\Program Files\Perplexity\Comet\Application\{cometVersion}\Installer\chrmstp.exe"" --configure-user-settings --verbose-logging --system-level", RegistryValueKind.String), () => Comet == true),
            ("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "Version", "43,0,0,0", RegistryValueKind.String), () => Comet == true),
            ("Disabling Comet services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}", "IsInstalled", 1, RegistryValueKind.DWord), () => Comet == true),
            ("Disabling Comet services", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}"), () => Comet == true),

            // download firefox
            ("Downloading Firefox", async () => await ProcessActions.RunDownload($"https://releases.mozilla.org/pub/firefox/releases/{firefoxVersion}/win64/en-US/Firefox%20Setup%20{firefoxVersion}.exe", ApplicationData.Current.TemporaryFolder.Path, "FirefoxSetup.exe"), () => Firefox == true),

            // install firefox
            ("Installing Firefox", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "FirefoxSetup.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("FirefoxSetup.exe")).DeleteAsync(); }, () => Firefox == true),

            // pin firefox to the taskbar
            ("Pinning Firefox to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Firefox.lnk"""), () => Firefox == true),

            // disable firefox startup entry
            ("Disabling Firefox startup entry", async () => TaskSchedulerHelper.Toggle(@"\Mozilla\Firefox Default Browser Agent 308046B0AF4A39CB", false), () => Firefox == true),

            // optimize firefox settings
            ("Optimizing Firefox settings", async () => Directory.CreateDirectory(@"C:\Program Files\Mozilla Firefox\distribution"), () => Firefox == true),
            ("Optimizing Firefox settings", async () => File.WriteAllText(Path.Combine(@"C:\Program Files\Mozilla Firefox", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"firefox.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => Firefox == true),
            ("Optimizing Firefox settings", async () => File.WriteAllText(Path.Combine(@"C:\Program Files\Mozilla Firefox", "firefox.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"widget.windows.mica\", true);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);"), () => Firefox == true),
            ("Optimizing Firefox settings", async () => File.WriteAllText(Path.Combine(@"C:\Program Files\Mozilla Firefox", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => Firefox == true),

            // download arkenfox user.js
            ("Downloading Arkenfox user.js", async () => await ProcessActions.RunDownload("https://raw.githubusercontent.com/arkenfox/user.js/refs/heads/master/user.js", @"C:\Program Files\Mozilla Firefox", "user.js"), () => Firefox == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => Firefox == true && uBlock == true),
            ("Installing uBlock Origin Extension", async () => await Task.Delay(500), () => Firefox == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => Firefox == true && SponsorBlock == true),
            ("Installing SponsorBlock Extension", async () => await Task.Delay(500), () => Firefox == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => Firefox == true && ReturnYouTubeDislike == true),
            ("Installing Return YouTube Dislike Extension", async () => await Task.Delay(500), () => Firefox == true && ReturnYouTubeDislike == true),

            // install i still don't care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => Firefox == true && Cookies == true),
            ("Installing I still don't care about cookies Extension", async () => await Task.Delay(500), () => Firefox == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => Firefox == true && DarkReader == true),
            ("Installing Dark Reader Extension", async () => await Task.Delay(500), () => Firefox == true && DarkReader == true),

            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => Firefox == true && Violentmonkey == true),
            ("Installing Violentmonkey Extension", async () => await Task.Delay(500), () => Firefox == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => Firefox == true && Tampermonkey == true),
            ("Installing TampermonkeyExtension", async () => await Task.Delay(500), () => Firefox == true && Tampermonkey == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => Firefox == true && iCloud == true),
            ("Installing iCloud Passwords Extension", async () => await Task.Delay(500), () => Firefox == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => Firefox == true && Bitwarden == true),
            ("Installing Bitwarden Extension", async () => await Task.Delay(500), () => Firefox == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => Firefox == true && OnePassword == true),
            ("Installing 1Password Extension", async () => await Task.Delay(500), () => Firefox == true && OnePassword == true),

            // download zen
            ("Downloading Zen", async () => await ProcessActions.RunDownload("https://github.com/zen-browser/desktop/releases/latest/download/zen.installer.exe", ApplicationData.Current.TemporaryFolder.Path, "zen.installer.exe"), () => Zen == true),

            // install zen
            ("Installing Zen", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "zen.installer.exe"), Arguments = "/S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("zen.installer.exe")).DeleteAsync(); }, () => Zen == true),

            // pin zen to the taskbar
            ("Pinning Zen to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Zen.lnk"""), () => Zen == true),

            // disable zen startup entry
            ("Disabling Zen startup entry", async () => TaskSchedulerHelper.Toggle(@"\Mozilla\Zen Default Browser Agent F0DC299D809B9700", false), () => Zen == true),

            // optimize zen settings
            ("Optimizing Zen settings", async () => Directory.CreateDirectory(@"C:\Program Files\Zen Browser\distribution"), () => Zen == true),
            ("Optimizing Zen settings", async () => File.WriteAllText(Path.Combine(@"C:\Program Files\Zen Browser", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"zen.cfg\");\npref(\"general.config.obscure_value\", 0);"), () => Zen == true),
            ("Optimizing Zen settings", async () => File.WriteAllText(Path.Combine(@"C:\Program Files\Zen Browser", "zen.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"zen.view.use-single-toolbar\", false);\ndefaultPref(\"zen.theme.accent-color\", \"#2c34fb\");\ndefaultPref(\"zen.urlbar.behavior\", \"float\");\ndefaultPref(\"zen.view.grey-out-inactive-windows\", false);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);"), () => Zen == true),
            ("Optimizing Zen settings", async () => File.WriteAllText(Path.Combine(@"C:\Program Files\Zen Browser", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}"), () => Zen == true),

            // download arkenfox user.js
            ("Downloading Arkenfox user.js", async () => await ProcessActions.RunDownload("https://raw.githubusercontent.com/arkenfox/user.js/refs/heads/master/user.js", @"C:\Program Files\Zen Browser", "user.js"), () => Zen == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin"), () => Zen == true && uBlock == true),
            ("Installing uBlock Origin Extension", async () => await Task.Delay(500), () => Zen == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock"), () => Zen == true && SponsorBlock == true),
            ("Installing SponsorBlock Extension", async () => await Task.Delay(500), () => Zen == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing Return YouTube Dislike Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes"), () => Zen == true && ReturnYouTubeDislike == true),
            ("Installing Return YouTube Dislike Extension", async () => await Task.Delay(500), () => Zen == true && ReturnYouTubeDislike == true),

            // install i still don't care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies"), () => Zen == true && Cookies == true),
            ("Installing I still don't care about cookies Extension", async () => await Task.Delay(500), () => Zen == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/darkreader"), () => Zen == true && DarkReader == true),
            ("Installing Dark Reader Extension", async () => await Task.Delay(500), () => Zen == true && DarkReader == true),

            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey"), () => Zen == true && Violentmonkey == true),
            ("Installing Violentmonkey Extension", async () => await Task.Delay(500), () => Zen == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey"), () => Zen == true && Tampermonkey == true),
            ("Installing TampermonkeyExtension", async () => await Task.Delay(500), () => Zen == true && Tampermonkey == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords"), () => Zen == true && iCloud == true),
            ("Installing iCloud Passwords Extension", async () => await Task.Delay(500), () => Zen == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager"), () => Zen == true && Bitwarden == true),
            ("Installing Bitwarden Extension", async () => await Task.Delay(500), () => Zen == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager"), () => Zen == true && OnePassword == true),
            ("Installing 1Password Extension", async () => await Task.Delay(500), () => Zen == true && OnePassword == true),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        double incrementPerTitle = filteredActions.Select(a => a.Title).Distinct().Count() > 0 ? stagePercentage / (double)filteredActions.Select(a => a.Title).Distinct().Count() : 0;

        List<Func<Task>> currentGroup = [];

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

    private static void UpdatePolicies(string policiesPath, string extensionUrl)
    {
        var json = File.ReadAllText(policiesPath);
        var root = JsonNode.Parse(json)?.AsObject();
        if (root != null && root.TryGetPropertyValue("policies", out var policiesNode))
        {
            var policies = policiesNode?.AsObject() ?? new JsonObject();
            JsonNode extensionNode = JsonValue.Create(extensionUrl)!;
            if (!policies.ContainsKey("Extensions"))
            {
                policies["Extensions"] = new JsonObject { ["Install"] = new JsonArray(extensionNode) };
            }
            else
            {
                var extensions = policies["Extensions"]?.AsObject();
                if (extensions != null)
                {
                    if (!extensions.TryGetPropertyValue("Install", out var installNode))
                    {
                        extensions["Install"] = new JsonArray(JsonValue.Create(extensionUrl));
                    }
                    else
                    {
                        var installArray = installNode?.AsArray();
                        if (installArray != null)
                        {
                            bool alreadyExists = false;
                            foreach (var item in installArray)
                            {
                                if (item?.ToString() == extensionUrl)
                                {
                                    alreadyExists = true;
                                    break;
                                }
                            }
                            if (!alreadyExists)
                            {
                                installArray.Add(extensionNode);
                            }
                        }
                    }
                }
            }
            root["policies"] = policies;
            File.WriteAllText(policiesPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
