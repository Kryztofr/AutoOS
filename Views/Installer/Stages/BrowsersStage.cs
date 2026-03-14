using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using WinRT.Interop;
using AutoOS.Helpers.Store;

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
            (@"Enabling ""Configure Do Not Track"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ConfigureDoNotTrack /t REG_DWORD /d 1 /f"), null),
            (@"Disabling ""Shopping in Microsoft Edge Enabled"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeShoppingAssistantEnabled /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Allow Microsoft content on the new tab page"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageContentEnabled /t REG_DWORD /d 0 /f"), null),
            (@"Disbaling ""Allow user feedback"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v UserFeedbackAllowed /t REG_DWORD /d 0 /f"), null),
            (@"Disabling ""Create Desktop Shortcut upon install default"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\EdgeUpdate"" /v CreateDesktopShortcutDefault /t REG_DWORD /d 0 /f"), null),
            (@"Enabling ""Turn off tracking of app usage"" policy", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableMFUTracking /t REG_DWORD /d 1 /f"), null),

            // disable microsoft edge services
            ("Disabling Microsoft Edge services", async () => edgeVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")).ProductVersion), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v """" /t REG_SZ /d ""Microsoft Edge"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""Localized Name"" /t REG_SZ /d ""Microsoft Edge"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{9459C573-B17A-45AE-9F64-1857B5D58CEE}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\Program Files (x86)\Microsoft\Edge\Application\{edgeVersion}\Installer\setup.exe\"" --configure-user-settings --verbose-logging --system-level --msedge --channel=stable"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""ComponentID"" /t REG_SZ /d ""DOTNETFRAMEWORKS"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""DontAsk"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""Enabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""StubPath"" /t REG_SZ /d ""C:\Windows\System32\Rundll32.exe C:\Windows\System32\mscories.dll,Install"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""ComponentID"" /t REG_SZ /d ""DOTNETFRAMEWORKS"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""DontAsk"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""Enabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""StubPath"" /t REG_SZ /d ""C:\Windows\SysWOW64\Rundll32.exe C:\Windows\SysWOW64\mscories.dll,Install"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v """" /t REG_SZ /d ""IEToEdge BHO"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v ""NoExplorer"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v """" /t REG_SZ /d ""IEToEdge BHO"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v ""NoExplorer"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /f"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdate"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop edgeupdate"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdatem"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop edgeupdatem"), null),
            ("Disabling Microsoft Edge services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\MicrosoftEdgeElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop MicrosoftEdgeElevationService"), null),

            // download google chrome
            ("Downloading Google Chrome", async () => await ProcessActions.RunDownload("http://dl.google.com/chrome/install/375.126/chrome_installer.exe", Path.GetTempPath(), "ChromeSetup.exe"), () => Chrome == true),

            // install google chrome
            ("Installing Google Chrome", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\ChromeSetup.exe"" --silent --install --system-level --do-not-launch-chrome"), () => Chrome == true),
            ("Installing Google Chrome", async () => chromeVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\ChromeSetup.exe")).ProductVersion), () => Chrome == true),
            ("Installing Google Chrome", async () => chromeVersion2 = await Task.Run(() => FileVersionInfo.GetVersionInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe").ProductVersion), () => Chrome == true),

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
            ("Please log in to your Google Chrome account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Google\Chrome\Application", "chrome.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync()), () => Chrome == true),

            // remove google chrome shortcut from the desktop
            ("Removing Google Chrome shortcut from the desktop", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Google Chrome.lnk"""), () => Chrome == true),

            // disable google chrome services
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleChromeElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop GoogleChromeElevationService"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterInternalService{chromeVersion}"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop GoogleUpdaterInternalService{chromeVersion}"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GoogleUpdaterService{chromeVersion}"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop GoogleUpdaterService{chromeVersion}"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v """" /t REG_SZ /d ""Google Chrome"" /f"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v ""Localized Name"" /t REG_SZ /d ""Google Chrome"" /f"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{8A69D345-D564-463c-AFF1-A69D9E530F96}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\\Program Files\\Google\\Chrome\\Application\\{chromeVersion2}\\Installer\\chrmstp.exe\"" --configure-user-settings --verbose-logging --system-level --channel=stable"" /f"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{8A69D345-D564-463c-AFF1-A69D9E530F96}"" /f"), () => Chrome == true),
            ("Disabling Google Chrome services", async () => await ProcessActions.RunPowerShell(@"Get-ScheduledTask | Where-Object {$_.TaskName -like 'GoogleUpdaterTaskSystem*'} | Disable-ScheduledTask"), () => Chrome == true),

            // download thorium
            ("Downloading Thorium", async () => await ProcessActions.RunDownload("https://github.com/Alex313031/Thorium-Win/releases/download/M130.0.6723.174/thorium_SSE4_mini_installer.exe", Path.GetTempPath(), "ThoriumSetup.exe"), () => Thorium == true),

            // install thorium
            ("Installing Thorium", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\ThoriumSetup.exe"" --silent --install --system-level --do-not-launch-chrome"), () => Thorium == true),
            ("Installing Thorium", async () => thoriumVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(@"C:\Program Files\Thorium\Application\thorium.exe").ProductVersion), () => Thorium == true),

            // disable thorium services
            ("Disabling Thorium services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}"" /v """" /t REG_SZ /d ""Thorium"" /f"), () => Thorium == true),
            ("Disabling Thorium services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}"" /v ""Localized Name"" /t REG_SZ /d ""Thorium"" /f"), () => Chrome == true),
            ("Disabling Thorium services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\\Program Files\\Thorium\\Application\\{thoriumVersion}\\Installer\\chrmstp.exe\"" --configure-user-settings --verbose-logging --system-level /f"), () => Thorium == true),
            ("Disabling Thorium services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), () => Thorium == true),
            ("Disabling Thorium services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), () => Thorium == true),
            ("Disabling Thorium services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{7D2B3E1D-D096-4594-9D8F-A6667F12E0AC}"" /f"), () => Thorium == true),

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
            ("Please log in to your Thorium account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Thorium\Application", "thorium.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync()), () => Thorium == true),

            // remove thorium shortcut from the desktop
            ("Removing Thorium shortcut from the desktop", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Thorium.lnk"""), () => Thorium == true),

            // download brave
            ("Downloading Brave", async () => await ProcessActions.RunDownload("https://github.com/brave/brave-browser/releases/latest/download/BraveBrowserStandaloneSetup.exe", Path.GetTempPath(), "BraveBrowserStandaloneSetup.exe"), () => Brave == true),

            // install brave
            ("Installing Brave", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\BraveBrowserStandaloneSetup.exe"" /silent /install"), () => Brave == true),
            ("Installing Brave", async () => braveVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(@"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe").ProductVersion), () => Brave == true),

            // pin brave to the taskbar
            ("Pinning Brave to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Brave.lnk"""), () => Brave == true),

            // remove brave shortcut from the desktop
            ("Removing Brave shortcut from the desktop", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Brave.lnk"""), () => Brave == true),

            // optimize brave settings
            //("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "initial_preferences"), @"C:\Program Files\BraveSoftware\Brave-Browser\Application\initial_preferences", true)), () => Brave == true),
            //("Optimizing Brave settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data"""), () => Brave == true),
            //("Optimizing Brave settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "Local State"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data", "Local State"), true)), () => Brave == true),

            // disable brave services
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\brave"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop brave"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\BraveElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop BraveElevationService"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\bravem"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop bravem"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v """" /t REG_SZ /d ""Brave"" /f"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v ""Localized Name"" /t REG_SZ /d ""Brave"" /f"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\{braveVersion}\\Installer\\chrmstp.exe\"" --configure-user-settings --verbose-logging --system-level"" /f"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{AFE6A462-C574-4B8A-AF43-4CC60DF4563B}"" /f"), () => Brave == true),
            ("Disabling Brave services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""tokens=1 delims=,"" %A in ('schtasks /query /fo csv ^| findstr BraveSoftwareUpdateTaskMachine') do schtasks /change /tn %A /disable"), () => Brave == true),

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
            ("Downloading Vivaldi", async () => await ProcessActions.RunDownload("https://vivaldi.com/download/Vivaldi.x64.exe", Path.GetTempPath(), "Vivaldi.x64.exe"), () => Vivaldi == true),

            // install vivaldi
            ("Installing Vivaldi", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\Vivaldi.x64"" --vivaldi-silent --do-not-launch-chrome --system-level"), () => Vivaldi == true),
            ("Installing Vivaldi", async () => vivaldiVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files\Vivaldi\Application\vivaldi.exe")).ProductVersion), () => Vivaldi == true),

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
            ("Removing Vivaldi shortcut from the desktop", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Vivaldi.lnk"""), () => Vivaldi == true),

            // disable vivaldi services
            ("Disabling Vivaldi services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}"" /v """" /t REG_SZ /d ""Vivaldi"" /f"), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}"" /v ""Localized Name"" /t REG_SZ /d ""Vivaldi"" /f"), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{9C142C0C-124C-4467-B117-EBCC62801D7B}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\Program Files\Vivaldi\Application\{vivaldiVersion}\Installer\chrmstp.exe\"" --configure-user-settings --verbose-logging --system-level --vivaldi-install-dir=\""C:\Program Files\Vivaldi\"""" /f"), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9C142C0C-124C-4467-B117-EBCC62801D7B}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), () => Vivaldi == true),
            ("Disabling Vivaldi services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9C142C0C-124C-4467-B117-EBCC62801D7B}"" /f"), () => Vivaldi == true),

            // download arc dependency
            ("Downloading Arc Dependency", async () => await ProcessActions.RunDownload("https://releases.arc.net/windows/dependencies/x64/Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx", Path.GetTempPath(), "Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx"), () => Arc == true),

            // install arc dependency
            ("Installing Arc Dependency", async () => await ProcessActions.RunNsudo("CurrentUser", @"powershell -Command ""Add-AppxPackage -Path $env:TEMP\Microsoft.VCLibs.x64.14.00.Desktop.14.0.33728.0.appx"""), () => Arc == true),

            // download arc
            ("Downloading Arc", async () => await ProcessActions.RunDownload("https://releases.arc.net/windows/prod/1.72.0.296/Arc.x64.msix", Path.GetTempPath(), "Arc.x64.msix"), () => Arc == true),

            // install arc
            ("Installing Arc", async () => await ProcessActions.RunNsudo("CurrentUser", @"powershell -Command ""Add-AppxPackage -Path $env:TEMP\Arc.x64.msix"""), () => Arc == true),
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
            ("Please log in to your Arc account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\TheBrowserCompany.Arc_" + arcVersion + @"_x64__ttt1ap7aakyb4", "Arc.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => Arc == true),

            // download comet
            ("Downloading Comet", async () => await ProcessActions.RunDownload("https://www.perplexity.ai/rest/browser/download?platform=win_x64&channel=stable", Path.GetTempPath(), "Comet.exe"), () => Comet == true),

            // install comet
            ("Installing Comet", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\Comet.exe"" -silent --do-not-launch-chrome --system-level"), () => Comet == true),
            ("Installing Comet", async () => cometVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files\Perplexity\Comet\Application\comet.exe")).ProductVersion), () => Comet == true),

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
            ("Removing Comet shortcut from the desktop", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Comet.lnk"""), () => Comet == true),

            // disable comet services
            ("Disabling Comet services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}"" /v """" /t REG_SZ /d ""Comet"" /f"), () => Comet == true),
            ("Disabling Comet services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}"" /v ""Localized Name"" /t REG_SZ /d ""Comet"" /f"), () => Comet == true),
            ("Disabling Comet services", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\Program Files\Perplexity\Comet\Application\{cometVersion}Installer\chrmstp.exe\""  --configure-user-settings --verbose-logging --system-level /f"), () => Comet == true),
            ("Disabling Comet services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), () => Comet == true),
            ("Disabling Comet services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), () => Comet == true),
            ("Disabling Comet services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{1F7C13D9-45E8-47E9-A2B5-6B2EF21B91F4}"" /f"), () => Comet == true),

            // download firefox
            ("Downloading Firefox", async () => await ProcessActions.RunDownload($"https://releases.mozilla.org/pub/firefox/releases/{firefoxVersion}/win64/en-US/Firefox%20Setup%20{firefoxVersion}.exe", Path.GetTempPath(), "FirefoxSetup.exe"), () => Firefox == true),

            // install firefox
            ("Installing Firefox", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\FirefoxSetup.exe"" /S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true"), () => Firefox == true),

            // pin firefox to the taskbar
            ("Pinning Firefox to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Firefox.lnk"""), () => Firefox == true),

            // disable firefox startup entry
            ("Disabling Firefox startup entry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"SCHTASKS /Change /TN ""\Mozilla\Firefox Default Browser Agent 308046B0AF4A39CB"" /Disable"), () => Firefox == true),

            // optimize firefox settings
            ("Optimizing Firefox settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""C:\Program Files\Mozilla Firefox\distribution"""), () => Firefox == true),
            ("Optimizing Firefox settings", async () => await Task.Run(() => File.WriteAllText(Path.Combine(@"C:\Program Files\Mozilla Firefox", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"firefox.cfg\");\npref(\"general.config.obscure_value\", 0);")), () => Firefox == true),
            ("Optimizing Firefox settings", async () => await Task.Run(() => File.WriteAllText(Path.Combine(@"C:\Program Files\Mozilla Firefox", "firefox.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"widget.windows.mica\", true);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);")), () => Firefox == true),
            ("Optimizing Firefox settings", async () => await Task.Run(() => File.WriteAllText(Path.Combine(@"C:\Program Files\Mozilla Firefox", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}")), () => Firefox == true),

            // download arkenfox user.js
            ("Downloading Arkenfox user.js", async () => await ProcessActions.RunDownload("https://raw.githubusercontent.com/arkenfox/user.js/refs/heads/master/user.js", @"C:\Program Files\Mozilla Firefox", "user.js"), () => Firefox == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin")), () => Firefox == true && uBlock == true),
            ("Installing uBlock Origin Extension", async () => await Task.Delay(500), () => Firefox == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock")), () => Firefox == true && SponsorBlock == true),
            ("Installing SponsorBlock Extension", async () => await Task.Delay(500), () => Firefox == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing Return YouTube Dislike Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes")), () => Firefox == true && ReturnYouTubeDislike == true),
            ("Installing Return YouTube Dislike Extension", async () => await Task.Delay(500), () => Firefox == true && ReturnYouTubeDislike == true),

            // install i still don't care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies")), () => Firefox == true && Cookies == true),
            ("Installing I still don't care about cookies Extension", async () => await Task.Delay(500), () => Firefox == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/darkreader")), () => Firefox == true && DarkReader == true),
            ("Installing Dark Reader Extension", async () => await Task.Delay(500), () => Firefox == true && DarkReader == true),

            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey")), () => Firefox == true && Violentmonkey == true),
            ("Installing Violentmonkey Extension", async () => await Task.Delay(500), () => Firefox == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey")), () => Firefox == true && Tampermonkey == true),
            ("Installing TampermonkeyExtension", async () => await Task.Delay(500), () => Firefox == true && Tampermonkey == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords")), () => Firefox == true && iCloud == true),
            ("Installing iCloud Passwords Extension", async () => await Task.Delay(500), () => Firefox == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager")), () => Firefox == true && Bitwarden == true),
            ("Installing Bitwarden Extension", async () => await Task.Delay(500), () => Firefox == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Mozilla Firefox\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager")), () => Firefox == true && OnePassword == true),
            ("Installing 1Password Extension", async () => await Task.Delay(500), () => Firefox == true && OnePassword == true),

            // download zen
            ("Downloading Zen", async () => await ProcessActions.RunDownload("https://github.com/zen-browser/desktop/releases/latest/download/zen.installer.exe", Path.GetTempPath(), "zen.installer.exe"), () => Zen == true),

            // install zen
            ("Installing Zen", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\zen.installer.exe"" /S /MaintenanceService=false /DesktopShortcut=false /StartMenuShortcut=true"), () => Zen == true),

            // pin zen to the taskbar
            ("Pinning Zen to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Zen.lnk"""), () => Zen == true),

            // disable zen startup entry
            ("Disabling Zen startup entry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"SCHTASKS /Change /TN ""\Mozilla\Zen Default Browser Agent F0DC299D809B9700"" /Disable"), () => Zen == true),

            // optimize zen settings
            ("Optimizing Zen settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""C:\Program Files\Zen Browser\distribution"""), () => Zen == true),
            ("Optimizing Zen settings", async () => await Task.Run(() => File.WriteAllText(Path.Combine(@"C:\Program Files\Zen Browser", "defaults", "pref", "autoconfig.js"), "pref(\"general.config.filename\", \"zen.cfg\");\npref(\"general.config.obscure_value\", 0);")), () => Zen == true),
            ("Optimizing Zen settings", async () => await Task.Run(() => File.WriteAllText(Path.Combine(@"C:\Program Files\Zen Browser", "zen.cfg"), "defaultPref(\"app.shield.optoutstudies.enabled\", false);\ndefaultPref(\"browser.search.serpEventTelemetryCategorization.enabled\", false);\ndefaultPref(\"dom.security.unexpected_system_load_telemetry_enabled\", false);\ndefaultPref(\"identity.fxaccounts.telemetry.clientAssociationPing.enabled\", false);\ndefaultPref(\"network.trr.confirmation_telemetry_enabled\", false);\ndefaultPref(\"nimbus.telemetry.targetingContextEnabled\", false);\ndefaultPref(\"reader.parse-on-load.enabled\", false);\ndefaultPref(\"telemetry.fog.init_on_shutdown\", false);\ndefaultPref(\"default-browser-agent.enabled\", false);\ndefaultPref(\"zen.view.use-single-toolbar\", false);\ndefaultPref(\"zen.theme.accent-color\", \"#2c34fb\");\ndefaultPref(\"zen.urlbar.behavior\", \"float\");\ndefaultPref(\"zen.view.grey-out-inactive-windows\", false);\ndefaultPref(\"widget.windows.mica.popups\", 1);\ndefaultPref(\"widget.windows.mica.toplevel-backdrop\", 0);")), () => Zen == true),
            ("Optimizing Zen settings", async () => await Task.Run(() => File.WriteAllText(Path.Combine(@"C:\Program Files\Zen Browser", "distribution", "policies.json"), "{\r\n  \"policies\": {}\r\n}")), () => Zen == true),

            // download arkenfox user.js
            ("Downloading Arkenfox user.js", async () => await ProcessActions.RunDownload("https://raw.githubusercontent.com/arkenfox/user.js/refs/heads/master/user.js", @"C:\Program Files\Zen Browser", "user.js"), () => Zen == true),

            // install ublock origin extension
            ("Installing uBlock Origin Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin")), () => Zen == true && uBlock == true),
            ("Installing uBlock Origin Extension", async () => await Task.Delay(500), () => Zen == true && uBlock == true),

            // install sponsorblock extension
            ("Installing SponsorBlock Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/sponsorblock")), () => Zen == true && SponsorBlock == true),
            ("Installing SponsorBlock Extension", async () => await Task.Delay(500), () => Zen == true && SponsorBlock == true),

            // install return youtube dislike extension
            ("Installing Return YouTube Dislike Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/return-youtube-dislikes")), () => Zen == true && ReturnYouTubeDislike == true),
            ("Installing Return YouTube Dislike Extension", async () => await Task.Delay(500), () => Zen == true && ReturnYouTubeDislike == true),

            // install i still don't care about cookies extension
            ("Installing I still don't care about cookies Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/istilldontcareaboutcookies")), () => Zen == true && Cookies == true),
            ("Installing I still don't care about cookies Extension", async () => await Task.Delay(500), () => Zen == true && Cookies == true),

            // install dark reader extension
            ("Installing Dark Reader Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/darkreader")), () => Zen == true && DarkReader == true),
            ("Installing Dark Reader Extension", async () => await Task.Delay(500), () => Zen == true && DarkReader == true),

            // install violentmonkey extension
            ("Installing Violentmonkey Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/violentmonkey")), () => Zen == true && Violentmonkey == true),
            ("Installing Violentmonkey Extension", async () => await Task.Delay(500), () => Zen == true && Violentmonkey == true),

            // install tampermonkey extension
            ("Installing Tampermonkey Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/tampermonkey")), () => Zen == true && Tampermonkey == true),
            ("Installing TampermonkeyExtension", async () => await Task.Delay(500), () => Zen == true && Tampermonkey == true),

            // install icloud passwords extension
            ("Installing iCloud Passwords Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/icloud-passwords")), () => Zen == true && iCloud == true),
            ("Installing iCloud Passwords Extension", async () => await Task.Delay(500), () => Zen == true && iCloud == true),

            // install bitwarden extension
            ("Installing Bitwarden Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/bitwarden-password-manager")), () => Zen == true && Bitwarden == true),
            ("Installing Bitwarden Extension", async () => await Task.Delay(500), () => Zen == true && Bitwarden == true),

            // install 1password extension
            ("Installing 1Password Extension", async () => await Task.Run(() => UpdatePolicies(@"C:\Program Files\Zen Browser\distribution\policies.json", "https://addons.mozilla.org/firefox/downloads/latest/1password-x-password-manager")), () => Zen == true && OnePassword == true),
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