using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Store;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using WinRT.Interop;
using AutoOS.Helpers.Processes;
using AutoOS.Helpers.Games;
using AutoOS.Helpers.Registry;
using AutoOS.Helpers.Services;
using Microsoft.Win32;
using AutoOS.Helpers.TaskScheduler;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class ApplicationStage
{
    public static bool Fortnite;
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        string ScheduleMode = PreparingStage.ScheduleMode;
        string LightTime = PreparingStage.LightTime;
        string DarkTime = PreparingStage.DarkTime;

        bool iCloud = PreparingStage.iCloud;
        bool Bitwarden = PreparingStage.Bitwarden;
        bool OnePassword = PreparingStage.OnePassword;
        bool AlwaysShowTrayIcons = PreparingStage.AlwaysShowTrayIcons;

        bool Word = PreparingStage.Word;
        bool Excel = PreparingStage.Excel;
        bool PowerPoint = PreparingStage.PowerPoint;
        bool OneNote = PreparingStage.OneNote;
        bool Teams = PreparingStage.Teams;
        bool Outlook = PreparingStage.Outlook;
        bool OneDrive = PreparingStage.OneDrive;

        bool VisualStudio = PreparingStage.VisualStudio;
        bool VisualStudioCode = PreparingStage.VisualStudioCode;
        bool Git = PreparingStage.Git;
        bool Python = PreparingStage.Python;
        bool Nodejs = PreparingStage.Nodejs;
        bool Trello = PreparingStage.Trello;

        bool AppleMusic = PreparingStage.AppleMusic;
        bool Tidal = PreparingStage.Tidal;
        bool Qobuz = PreparingStage.Qobuz;
        bool AmazonMusic = PreparingStage.AmazonMusic;
        bool DeezerMusic = PreparingStage.DeezerMusic;
        bool Spotify = PreparingStage.Spotify;
        bool WhatsApp = PreparingStage.WhatsApp;
        bool Discord = PreparingStage.Discord;
        bool EpicGames = PreparingStage.EpicGames;
        bool EpicGamesAccount = PreparingStage.EpicGamesAccount;
        bool EpicGamesGames = PreparingStage.EpicGamesGames;
        bool Steam = PreparingStage.Steam;
        bool SteamGames = PreparingStage.SteamGames;
        bool RiotClient = PreparingStage.RiotClient;
        bool EA = PreparingStage.EA;
        bool UbisoftConnect = PreparingStage.UbisoftConnect;
        bool BattleNet = PreparingStage.BattleNet;
        bool MinecraftLauncher = PreparingStage.MinecraftLauncher;
        bool RockstarGamesLauncher = PreparingStage.RockstarGamesLauncher;
        bool FiveM = PreparingStage.FiveM;
        bool FACEIT = PreparingStage.FACEIT;

        InstallPage.Status.Text = "Configuring Applications...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        string icloudVersion = "";
        string bitwardenVersion = "";
        string onePasswordVersion = "";
        string trelloVersion = "";
        string dolbyAccessVersion = "";
        string appleMusicVersion = "";
        string tidalVersion = "";
        string amazonMusicVersion = "";
        string deezerMusicVersion = "";
        string spotifyVersion = "";
        string discordVersion = "";
        string whatsAppVersion = "";
        string rockstarGamesLauncherVersion = "";

        string scheduleMode = ScheduleMode switch
        {
            "Sunset to sunrise" => "LocationService",
            "Custom hours" => "CustomHours",
            _ => ScheduleMode
        };

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // optimize notepad settings
            ("Optimizing Notepad settings", async () => await ProcessActions.RunPowerShellScript("notepad.ps1", ""), null),

            // download heif image extension
            ("Downloading HEIF Image Extension", async () => await StoreHelper.Download("Microsoft.HEIFImageExtension_8wekyb3d8bbwe"), null),

            // install heif image extension
            ("Installing HEIF Image Extension", async () => await StoreHelper.Install("Microsoft.HEIFImageExtension_8wekyb3d8bbwe"), null),

            // download mpeg-2 video extension
            ("Downloading MPEG-2 Video Extension", async () => await StoreHelper.Download("Microsoft.MPEG2VideoExtension_8wekyb3d8bbwe"), null),

            // install mpeg-2 video extension
            ("Installing MPEG-2 Video Extension", async () => await StoreHelper.Install("Microsoft.MPEG2VideoExtension_8wekyb3d8bbwe"), null),

            // download av1 video extension
            ("Downloading AV1 Video Extension", async () => await StoreHelper.Download("Microsoft.AV1VideoExtension_8wekyb3d8bbwe"), null),

            // install av1 video extension
            ("Installing AV1 Video Extension", async () => await StoreHelper.Install("Microsoft.AV1VideoExtension_8wekyb3d8bbwe"), null),

            // download avc encoder video extension
            ("Downloading AVC Encoder Video Extension", async () => await StoreHelper.Download("Microsoft.AVCEncoderVideoExtension_8wekyb3d8bbwe"), null),

            // install avc encoder video extension
            ("Installing AVC Encoder Video Extension", async () => await StoreHelper.Install("Microsoft.AVCEncoderVideoExtension_8wekyb3d8bbwe"), null),

            // download dolby vision extension
            ("Downloading Dolby Vision Extension", async () => await StoreHelper.Download("DolbyLaboratories.DolbyVisionAccess_rz1tebttyb220"), null),

            // install dolby vision extension
            ("Installing Dolby Vision Extension", async () => await StoreHelper.Install("DolbyLaboratories.DolbyVisionAccess_rz1tebttyb220"), null),

            // download movies & tv
            ("Downloading Movies & TV", async () => await StoreHelper.Download("Microsoft.ZuneVideo_8wekyb3d8bbwe"), null),

            // install movies & tv
            ("Installing Movies & TV", async () => await StoreHelper.Install("Microsoft.ZuneVideo_8wekyb3d8bbwe"), null),

            // download icloud
            ("Downloading iCloud", async () => await StoreHelper.Download("AppleInc.iCloud_nzyj5cx40ttqa"), () => iCloud == true),

            // install icloud
            ("Installing iCloud", async () => await StoreHelper.Install("AppleInc.iCloud_nzyj5cx40ttqa"), () => iCloud == true),
            ("Installing iCloud", async () => icloudVersion = StoreHelper.GetVersion("AppleInc.iCloud_nzyj5cx40ttqa"), () => iCloud == true),

            // log in to icloud
            ("Please log in to your iCloud account", async () => await Task.Delay(1000), () => iCloud == true),
            ("Please log in to your iCloud account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.iCloud_" + icloudVersion + "_x64__nzyj5cx40ttqa", "iCloud", "iCloudHome.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => iCloud == true),

            // disable icloud startup entries
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudHomeStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudDriveStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudCKKSStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudPhotosStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudPhotoStreamsStartupTask", "State", 1, RegistryValueKind.DWord), () => iCloud == true),

            // download bitwarden
            ("Downloading Bitwarden", async () => await StoreHelper.Download("8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy"), () => Bitwarden == true),

            // install bitwarden
            ("Installing Bitwarden", async () => await StoreHelper.Install("8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy"), () => Bitwarden == true),
            ("Installing Bitwarden", async () => bitwardenVersion = StoreHelper.GetVersion("8bitSolutionsLLC.bitwardendesktop_h4e712dmw3xyy"), () => Bitwarden == true),

            // log in to bitwarden
            ("Please log in to your Bitwarden account", async () => await Task.Delay(1000), () => Bitwarden == true),
            ("Please log in to your Bitwarden account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\8bitSolutionsLLC.bitwardendesktop_" + bitwardenVersion + "_x64__h4e712dmw3xyy", "app", "Bitwarden.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => Bitwarden == true),

            // download 1password
            ("Downloading 1Password", async () => await StoreHelper.Download("DC5C6510.2032887045529_2v019pwa6amcg"), () => OnePassword == true),

            // install 1password
            ("Installing 1Password", async () => await StoreHelper.Install("DC5C6510.2032887045529_2v019pwa6amcg"), () => OnePassword == true),
            ("Installing 1Password", async () => onePasswordVersion = StoreHelper.GetVersion("DC5C6510.2032887045529_2v019pwa6amcg"), () => OnePassword == true),

            // log in to 1password
            ("Please log in to your 1Password account", async () => await Task.Delay(1000), () => OnePassword == true),
            ("Please log in to your 1Password account", async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "settings", "settings.json"); Directory.CreateDirectory(Path.GetDirectoryName(path) !); await File.WriteAllTextAsync(path, "{ \"version\": 1, \"updates.updateChannel\": \"PRODUCTION\", \"authTags\": {}, \"app.keepInTray\": false }"); }, () => OnePassword == true),
            ("Please log in to your 1Password account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\DC5C6510.2032887045529_" + onePasswordVersion + "_x64__2v019pwa6amcg", "1Password.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => OnePassword == true),

            // download nanazip
            ("Downloading NanaZip", async () => await StoreHelper.Download("40174MouriNaruto.NanaZip_8672y6p4v2rg0"), null),

            // install nanazip
            ("Installing NanaZip", async () => await StoreHelper.Install("40174MouriNaruto.NanaZip_8672y6p4v2rg0"), null),

            //// download files
            //("Downloading Files", async () => await ProcessActions.RunDownload("https://files.community/appinstallers/Files.stable.appinstaller", ApplicationData.Current.TemporaryFolder.Path, "Files.stable.appinstaller"), null),

            //// install files
            //("Installing Files", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -AppInstallerFile ""$env:TEMP\Files.stable.appinstaller"""), null),
            //("Installing Files", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/u2hcpijo21p8i0u6lj6qm/Files.zip?rlkey=e5pq2cbj4sevh5lf5jfmvv5hc&st=8o8frer3&dl=0", ApplicationData.Current.TemporaryFolder.Path, "Files.zip"), null),
            //("Installing Files", async () => await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Files.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Files_1y0xx7n9077q4", "LocalState")), null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command", "", @"""%LOCALAPPDATA%\Files\Files.App.Launcher.exe"" ""%1""", RegistryValueKind.ExpandString), null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command", "DelegateExecute", "2", RegistryValueKind.String), null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\explore\command", "", @"""%LOCALAPPDATA%\Files\Files.App.Launcher.exe"" ""%1""", RegistryValueKind.ExpandString), null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Folder\shell\explore\command", "DelegateExecute", "2", RegistryValueKind.String), null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}\shell\opennewwindow\command", "", @"""%LOCALAPPDATA%\Files\Files.App.Launcher.exe""", RegistryValueKind.ExpandString), null),
            //("Installing Files", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}\shell\opennewwindow\command", "DelegateExecute", "2", RegistryValueKind.String), null),

            // download everything
            ("Downloading Everything", async () => await ProcessActions.RunDownload("https://www.voidtools.com/Everything-1.5.0.1404a.x64-Setup.exe", ApplicationData.Current.TemporaryFolder.Path, "Everything.exe"), null),
            
            // install everything
            ("Installing Everything", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Everything.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Everything.exe")).DeleteAsync(); }, null),
            ("Installing Everything", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything")), null),
            ("Installing Everything", async () => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "Everything-1.5a.ini"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything", "Everything-1.5a.ini"), true), null),
            ("Installing Everything", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Everything 1.5a\Everything.exe", WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-install-run-on-system-startup"})!.WaitForExitAsync(), null),
            ("Installing Everything", async () => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Everything 1.5a\Everything.exe", WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-startup" }), null),

            // remove everything desktop shortcut 
            ("Removing Everything desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Everything 1.5a.lnk")), null),

            // download windhawk
            ("Downloading Windhawk", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/1id8mn7onjmyvd5ky1hka/Windhawk.zip?rlkey=0y77dk0u8crd34gnhszpt4nqc&st=391uel11&dl=0", ApplicationData.Current.TemporaryFolder.Path, "Windhawk.zip"), null),

            // install windhawk
            ("Installing Windhawk", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Windhawk.zip"), @"C:\Program Files\Windhawk"); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Windhawk.zip")).DeleteAsync(); }, null),
            ("Installing Windhawk", async () => await Task.Run(() => Directory.Move(@"C:\Program Files\Windhawk\Windhawk", @"C:\ProgramData\Windhawk")), null),
            ("Installing Windhawk", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @$"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "windhawk.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
            //("Installing Windhawk", async () => await RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "LightThemePath", LightThemePath, RegistryValueKind.String), null),
            //("Installing Windhawk", async () => await RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "DarkThemePath", DarkThemePath, RegistryValueKind.String), null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher", "Disabled", 1, RegistryValueKind.DWord), () => ScheduleMode == "Always Light" || ScheduleMode == "Always Dark"),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "ScheduleMode", scheduleMode, RegistryValueKind.String), null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "CustomLight", LightTime, RegistryValueKind.String), null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings", "CustomDark", DarkTime, RegistryValueKind.String), null),
            ("Installing Windhawk", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\taskbar-notification-icons-show-all", "Disabled", 1, RegistryValueKind.DWord), () => AlwaysShowTrayIcons == false),
            ("Installing Windhawk", async () => ServicesHelper.CreateService("Windhawk", @"""C:\Program Files\Windhawk\windhawk.exe"" -service"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs\Windhawk.lnk'));$sc.TargetPath='C:\Program Files\Windhawk\windhawk.exe';$sc.Save()"), null),
            ("Installing Windhawk", async () => ServicesHelper.StartService("Windhawk"), null),
            
            // download startallback
            ("Downloading StartAllBack", async () => await ProcessActions.RunDownload("https://www.startallback.com/download.php", ApplicationData.Current.TemporaryFolder.Path, "StartAllBackSetup.exe"), null),

            // install startallback
            ("Installing StartAllBack", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @$"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "startallback.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
            ("Installing StartAllBack", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "StartAllBackSetup.exe"), Arguments = "/silent /allusers" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("StartAllBackSetup.exe")).DeleteAsync(); }, null),
            ("Installing StartAllBack", async () => TaskSchedulerHelper.Toggle(@"StartAllBack Update", false), null),

            // activate startallback
            ("Activating StartAllBack", async () => await ProcessActions.PatchStartAllBack(), null),
            ("Activating StartAllBack", async () => await Task.Delay(2000), null),

            // download process explorer
            ("Downloading Process Explorer", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/a8l16rp3cpcvkkryavix1/procexp64.exe?rlkey=5fec8mcmkfcxlum9a95o1xn3t&st=mjkrpc1f&dl=0", ApplicationData.Current.TemporaryFolder.Path, "procexp64.exe"), null),
            //("Downloading Process Explorer", async () => await ProcessActions.RunDownload("https://download.sysinternals.com/files/ProcessExplorer.zip", ApplicationData.Current.TemporaryFolder.Path, "ProcessExplorer.zip"), null),

            // install process explorer
            //("Installing Process Explorer", async () => await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "ProcessExplorer.zip"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "ProcessExplorer")), null),
            //("Installing Process Explorer", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "ProcessExplorer", "procexp64.exe"), @"C:\Windows\procexp64.exe", true), null),
			("Installing Process Explorer", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer")), null),
            ("Installing Process Explorer", async () => { File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "procexp64.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Process Explorer", "procexp64.exe"), true); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("procexp64.exe")).DeleteAsync(); }, null),
            ("Installing Process Explorer", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:ProgramData, 'Microsoft\Windows\Start Menu\Programs\Process Explorer.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:ProgramFiles, 'Process Explorer\procexp64.exe'); $Shortcut.Save()"), null),
            ("Installing Process Explorer", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "processexplorer.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
            ("Installing Process Explorer", async () => await Task.Delay(500), null),

            // download office
            ("Downloading Office", async () => await ProcessActions.RunDownload("https://officecdn.microsoft.com/pr/wsus/setup.exe", ApplicationData.Current.TemporaryFolder.Path, "setup.exe"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

            // install office
            ("Installing Office", async () => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "configuration.xml"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml"), true), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Word").Remove(); doc.Save(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); }, () => Word == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Excel").Remove(); doc.Save(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); }, () => Excel == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "PowerPoint").Remove(); doc.Save(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); }, () => PowerPoint == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OneNote").Remove(); doc.Save(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); }, () => OneNote == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Teams").Remove(); doc.Save(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); }, () => Teams == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OutlookForWindows").Remove(); doc.Save(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); }, () => Outlook == true),
            ("Installing Office", async () => { var doc = XDocument.Load(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OneDrive").Remove(); doc.Save(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")); }, () => OneDrive == true),
            ("Installing Office", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "setup.exe"), Arguments = $@"/configure ""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "configuration.xml")}""" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("setup.exe")).DeleteAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("configuration.xml")).DeleteAsync(); }, () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

            // disable office startup entries
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Filter\AutorunsDisabled\text/xml\CLSID", "", "{807583E5-5146-11D5-A672-00B0D022E945}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Filter\text/xml"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb-roaming.16\CLSID", "", "{83C25742-A9F7-49FB-9138-434302C88D07}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\mso-minsb-roaming.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16\CLSID", "", "{42089D2D-912D-4018-9087-2B87803E93FB}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\osf-roaming.16\CLSID", "", "{42089D2D-912D-4018-9087-2B87803E93FB}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\osf-roaming.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\osf.16\CLSID", "", "{5504BE45-A83B-4808-900A-3A5C36E7F77A}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\osf.16"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "", "Lync Click to Call BHO", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "NoExplorer", "1", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "", "Lync Click to Call", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "MenuText", "Lync Click to Call", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "Icon", @"C:\Program Files\Microsoft Office\root\VFS\ProgramFilesX86\Microsoft Office\Office16\lync.exe,1", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "HotIcon", @"C:\Program Files\Microsoft Office\root\VFS\ProgramFilesX86\Microsoft Office\Office16\lync.exe,1", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "CLSID", "{1FBA04EE-3024-11d2-8F1F-0000F87ABD16}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "ClsidExtension", "{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "Default Visible", "Yes", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}", "ButtonText", "Lync Click to Call", RegistryValueKind.String), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Actions Server", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Automatic Updates 2.0", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Background Push Maintenance", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office ClickToRun Service Monitor", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Feature Updates", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Feature Updates Logon", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Office\Office Performance Monitor", false), () => Word || Excel || PowerPoint || OneNote || Teams || Outlook || OneDrive),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16\CLSID", "", "{42089D2D-912D-4018-9087-2B87803E93FB}", RegistryValueKind.String), () => OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.DeleteKey(RegistryHelper.Identity.TrustedInstaller, @"HKEY_CLASSES_ROOT\PROTOCOLS\Handler\mso-minsb.16"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle("OneDrive Per-Machine Standalone Update Task", false), () => OneDrive == true),
            ("Disabling Office startup entries", async () => TaskSchedulerHelper.Toggle("OneDrive Reporting Task", false), () => OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FileSyncHelper", "Start", 4, RegistryValueKind.DWord), () => OneDrive == true),
            ("Disabling Office startup entries", async () => ServicesHelper.StopService("FileSyncHelper"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OneDrive Updater Service", "Start", 4, RegistryValueKind.DWord), () => OneDrive == true),
            ("Disabling Office startup entries", async () => ServicesHelper.StopService("OneDrive Updater Service"), () => OneDrive == true),

            // disable office telemetry
            ("Disabling Office telemetry", async () => await ProcessActions.RunPowerShellScript("disableofficetelemetry.ps1", ""), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

            // download visual studio
            ("Downloading Visual Studio", async () => await ProcessActions.RunDownload("https://aka.ms/vs/stable/vs_community.exe", ApplicationData.Current.TemporaryFolder.Path, "vs_Community.exe"), () => VisualStudio == true),

            // install visual studio
            ("Installing Visual Studio", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "vs_Community.exe"), Arguments = "--quiet --wait" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("vs_Community.exe")).DeleteAsync(); }, () => VisualStudio == true),

            // optimize visual studio
            ("Optimizing Visual Studio", async () => { while (Process.GetProcessesByName("VSNgenRunner").Length == 1) await Task.Delay(500); }, () => VisualStudio == true),

            // pin visual studio to the taskbar
            ("Pinning Visual Studio to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Visual Studio.lnk"""), () => VisualStudio == true),

            // download mica visual studio
            ("Downloading Mica Visual Studio", async () => await ProcessActions.RunDownload("https://github.com/Tech5G5G/Mica-Visual-Studio/releases/latest/download/MicaVisualStudio.vsix", ApplicationData.Current.TemporaryFolder.Path, "MicaVisualStudio.vsix"), () => VisualStudio == true),

            // install mica visual studio
            ("Installing Mica Visual Studio", async () => { await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe", Arguments = $"/quiet /admin {Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "MicaVisualStudio.vsix")}" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("MicaVisualStudio.vsix")).DeleteAsync(); }, () => VisualStudio == true),

            // download xaml styler
            ("Downloading XAML Styler", async () => await ProcessActions.RunDownload("https://marketplace.visualstudio.com/_apis/public/gallery/publishers/TeamXavalon/vsextensions/XAMLStyler2022/3.2501.8/vspackage", ApplicationData.Current.TemporaryFolder.Path, "XamlStyler.Extension.Windows.VS2022.vsix"), () => VisualStudio == true),

            // install xaml styler
            ("Installing XAML Styler", async () => { await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe", Arguments = $"/quiet /admin {Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "XamlStyler.Extension.Windows.VS2022.vsix")}" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("XamlStyler.Extension.Windows.VS2022.vsix")).DeleteAsync(); }, () => VisualStudio == true),

            // download visual studio code
            ("Downloading Visual Studio Code", async () => await ProcessActions.RunDownload("https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user", ApplicationData.Current.TemporaryFolder.Path, "VSCodeUserSetup-x64.exe"), () => VisualStudioCode == true),

            // install visual studio code
            ("Installing Visual Studio Code", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VSCodeUserSetup-x64.exe"), Arguments = "/VERYSILENT /NORESTART /MERGETASKS=!runcode" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("VSCodeUserSetup-x64.exe")).DeleteAsync(); }, () => VisualStudioCode ==  true),

            // pin visual studio code to the taskbar
            ("Pinning Visual Studio Code to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Visual Studio Code\Visual Studio Code.lnk")}"""), () => VisualStudioCode == true),

            // download git
            ("Downloading Git", async () => await ProcessActions.RunDownload("https://github.com/git-for-windows/git/releases/download/v2.53.0.windows.1/Git-2.53.0-64-bit.exe", ApplicationData.Current.TemporaryFolder.Path, "Git64-bit.exe"), () => Git == true),

            // install git
            ("Installing Git", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Git64-bit.exe"), Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOICONS /COMPONENTS=GitLFS,GitGUI,GitCore" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Git64-bit.exe")).DeleteAsync(); }, () => Git ==  true),

            // download python
            ("Downloading Python", async () => await ProcessActions.RunDownload("https://www.python.org/ftp/python/3.14.2/python-3.14.2-amd64.exe", ApplicationData.Current.TemporaryFolder.Path, "python-3.14.2-amd64.exe"), () => Python == true),

            // install python
            ("Installing Python", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "python-3.14.2-amd64.exe"), Arguments = "/quiet InstallAllUsers=1 PrependPath=1" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("python-3.14.2-amd64.exe")).DeleteAsync(); }, () => Python == true),

            // download nodejs
            ("Downloading Node.js", async () => await ProcessActions.RunDownload("https://nodejs.org/dist/v24.12.0/node-v24.12.0-x64.msi", ApplicationData.Current.TemporaryFolder.Path, "node-v24.12.0-x64.msi"), () => Nodejs == true),

            // install nodejs
            ("Installing Node.js", async () => { await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "node-v24.12.0-x64.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("node-v24.12.0-x64.msi")).DeleteAsync(); }, () => Nodejs ==  true),

            // download trello
            ("Downloading Trello", async () => await StoreHelper.Download("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa"), () => Trello == true),

            // install trello
            ("Installing Trello", async () => await StoreHelper.Install("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa"), () => Trello == true),
            ("Installing Trello", async () => trelloVersion = StoreHelper.GetVersion("45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa"), () => Trello == true),

            // pin trello to the taskbar
            ("Pinning Trello to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path 45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa!trello"), () => Trello == true),

            // log in to trello
            ("Please log in to your Trello account", async () => await Task.Delay(1000), () => Trello == true),
            ("Please log in to your Trello account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\45273LiamForsyth.PawsforTrello_" + trelloVersion + @"_x64__7pb5ddty8z1pa\app", "Trello.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Trello == true),

            // download dolby access
            ("Downloading Dolby Access", async () => await StoreHelper.Download("DolbyLaboratories.DolbyAccess_rz1tebttyb220", 1), () => AppleMusic == true),

            // install dolby access
            ("Installing Dolby Access", async () => await StoreHelper.Install("DolbyLaboratories.DolbyAccess_rz1tebttyb220"), () => AppleMusic == true),
            ("Installing Dolby Access", async () => dolbyAccessVersion = StoreHelper.GetVersion("DolbyLaboratories.DolbyAccess_rz1tebttyb220"), () => AppleMusic == true),

            // log in to dolby access
            ("Please log in to your Dolby Access account", async () => await Task.Delay(1000), () => AppleMusic == true),
            ("Please log in to your Dolby Access account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\DolbyLaboratories.DolbyAccess_" + dolbyAccessVersion + "_x64__rz1tebttyb220", "DolbyAccess.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => AppleMusic == true),

            // download apple music
            ("Downloading Apple Music", async () => await StoreHelper.Download("AppleInc.AppleMusicWin_nzyj5cx40ttqa"), () => AppleMusic == true),

            // install apple music
            ("Installing Apple Music", async () => await StoreHelper.Install("AppleInc.AppleMusicWin_nzyj5cx40ttqa"), () => AppleMusic == true),
            ("Installing Apple Music", async () => appleMusicVersion = StoreHelper.GetVersion("AppleInc.AppleMusicWin_nzyj5cx40ttqa"), () => AppleMusic == true),
            
            // enable "keep miniplayer on top of all other windows"
            (@"Enabling ""Keep Miniplayer on top of all other windows""", async () => await ProcessActions.RunPowerShellScript("applemusic.ps1", ""), () => AppleMusic == true),

            // pin apple music to the taskbar
            ("Pinning Apple Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path AppleInc.AppleMusicWin_nzyj5cx40ttqa!App"), () => AppleMusic == true),

            // log in to apple music
            ("Please log in to your Apple Music account", async () => await Task.Delay(1000), () => AppleMusic == true),
            ("Please log in to your Apple Music account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.AppleMusicWin_" + appleMusicVersion + "_x64__nzyj5cx40ttqa", "AppleMusic.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => AppleMusic == true),

            // download tidal
            ("Downloading TIDAL", async () => await StoreHelper.Download("WiMPMusic.27241E05630EA_kn85bz84x7te4"), () => Tidal == true),

            // install tidal
            ("Installing TIDAL", async () => await StoreHelper.Install("WiMPMusic.27241E05630EA_kn85bz84x7te4"), () => Tidal == true),
            ("Installing TIDAL", async () => tidalVersion = StoreHelper.GetVersion("WiMPMusic.27241E05630EA_kn85bz84x7te4"), () => Tidal == true),

            // pin tidal to the taskbar
            ("Pinning TIDAL to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path WiMPMusic.27241E05630EA_kn85bz84x7te4!TIDAL"), () => Tidal == true),

            // log in to tidal
            ("Please log in to your TIDAL account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\WiMPMusic.27241E05630EA_" + tidalVersion + @"_x86__kn85bz84x7te4\app", "TIDAL.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Tidal == true),

            // download qobuz
            ("Downloading Qobuz", async () => await ProcessActions.RunDownload("https://desktop.qobuz.com/releases/win32/x64/windows7_8_10/8.1.0-b019/Qobuz_Installer.exe", ApplicationData.Current.TemporaryFolder.Path, "Qobuz_Installer.exe"), () => Qobuz == true),

            // install qobuz
            ("Installing Qobuz", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Qobuz_Installer.exe"), Arguments = "-s" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Qobuz_Installer.exe")).DeleteAsync(); }, () => Qobuz == true),

            // pin qobuz to the taskbar
            ("Pinning Qobuz to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Qobuz\Qobuz.lnk")}"""), () => Qobuz == true),

            // log in to qobuz
            ("Please log in to your Qobuz account", async () => await Task.Delay(1000), () => Qobuz == true),
            ("Please log in to your Qobuz account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Qobuz", "Qobuz.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync(), () => Qobuz == true),
            ("Please log in to your Qobuz account", async () => { while (Process.GetProcessesByName("Qobuz").Length != 0) await Task.Delay(500); }, () => Qobuz == true),

            // download amazon music
            ("Downloading Amazon Music", async () => await StoreHelper.Download("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0"), () => AmazonMusic == true),

            // install amazon music
            ("Installing Amazon Music", async () => await StoreHelper.Install("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0"), () => AmazonMusic == true),
            ("Installing Amazon Music", async () => amazonMusicVersion = StoreHelper.GetVersion("AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0"), () => AmazonMusic == true),

            // pin amazon music to the taskbar
            ("Pinning Amazon Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0!AmazonMobileLLC.AmazonMusic"), () => AmazonMusic == true),

            // log in to amazon music
            ("Please log in to your Amazon Music account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AmazonMobileLLC.AmazonMusic_" + amazonMusicVersion + "_x86__kc6t79cpj4tp0", "Amazon Music.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => AmazonMusic == true),

            // download deezer music
            ("Downloading Deezer Music", async () => await StoreHelper.Download("Deezer.62021768415AF_q7m17pa7q8kj0"), () => DeezerMusic == true),

            // install deezer music
            ("Installing Deezer Music", async () => await StoreHelper.Install("Deezer.62021768415AF_q7m17pa7q8kj0"), () => DeezerMusic == true),
            ("Installing Deezer Music", async () => deezerMusicVersion = StoreHelper.GetVersion("Deezer.62021768415AF_q7m17pa7q8kj0"), () => DeezerMusic == true),

            // pin deezer music to the taskbar
            ("Pinning Deezer Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path Deezer.62021768415AF_q7m17pa7q8kj0!Deezer.Music"), () => DeezerMusic == true),

            // log in to deezer music
            ("Please log in to your Deezer Music account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\Deezer.62021768415AF_" + deezerMusicVersion + @"_x86__q7m17pa7q8kj0\app", "Deezer.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => DeezerMusic == true),

            // download spotify
            ("Downloading Spotify", async () => await ProcessActions.RunDownload("https://download.scdn.co/SpotifyFullSetupX64.exe", ApplicationData.Current.TemporaryFolder.Path, "SpotifyFullSetupX64.exe"), () => Spotify == true),

            // install spotify
            ("Installing Spotify", async () => spotifyVersion = FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "SpotifyFullSetupX64.exe")).ProductVersion, () => Spotify == true),
            ("Installing Spotify", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "SpotifyFullSetupX64.exe"), Arguments = $"/extract \"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify")}\"" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("SpotifyFullSetupX64.exe")).DeleteAsync(); }, () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "DisplayIcon", @"%AppData%\Spotify\Spotify.exe", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "DisplayName", "Spotify", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "DisplayVersion", spotifyVersion, RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "InstallLocation", @"%AppData%\Spotify", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "NoModify", 1, RegistryValueKind.DWord), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "NoRepair", 1, RegistryValueKind.DWord), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "Publisher", "Spotify AB", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "UninstallString", @"%AppData%\Spotify\Spotify.exe /uninstall", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "URLInfoAbout", "https://www.spotify.com", RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify", "Version", spotifyVersion, RegistryValueKind.String), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Spotify.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:APPDATA, 'Spotify\Spotify.exe'); $Shortcut.Save()"), () => Spotify == true),

            // pin spotify to the taskbar
            ("Pinning Spotify to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Spotify.lnk")}"""), () => Spotify == true),

            // disable spotify hardware acceleration
            ("Disabling Spotify hardware acceleration", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "prefs"), "ui.hardware_acceleration=false"), () => Spotify == true),

            // download spotx
            ("Downloading SpotX", async () => await ProcessActions.RunDownload("https://raw.githubusercontent.com/SpotX-Official/SpotX/main/run.ps1", ApplicationData.Current.TemporaryFolder.Path, "run.ps1"), () => Spotify == true),

            // install spotx
            ("Installing SpotX", async () => await ProcessActions.RunPowerShell($@"& $env:TEMP\run.ps1 -new_theme -adsections_off -podcasts_off -block_update_off -version {spotifyVersion}-1234"), () => Spotify == true),

            // log in to spotify
            ("Please log in to your Spotify account", async () => await Task.Delay(1000), () => Spotify == true),
            ("Please log in to your Spotify account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Spotify == true),
            
            // remove spotify desktop shortcut
            ("Removing Spotify desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Spotify.lnk")), () => Spotify == true),

            // disable spotify startup entry
            ("Disabling Spotify startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Spotify", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Spotify == true),

            // download discord
            ("Downloading Discord", async () => await ProcessActions.RunDownload("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64", ApplicationData.Current.TemporaryFolder.Path, "DiscordSetup.exe"), () => Discord == true),

            // install discord
            ("Installing Discord", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "DiscordSetup.exe"), Arguments = "/silent" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("DiscordSetup.exe")).DeleteAsync(); }, () => Discord == true),
            ("Installing Discord", async () => discordVersion = FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "DiscordSetup.exe")).ProductVersion, () => Discord == true),
            ("Installing Discord", async () => File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "installer.db"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "installer.db"), true), () => Discord == true),

            // pin discord to the taskbar
            ("Pinning Discord to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Discord Inc\Discord.lnk")}"""), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Discord.lnk")), () => Discord == true),

            // disable discord startup entry
            ("Disabling Discord startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Discord", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Discord == true),

            // optimize discord settings
            ("Optimizing Discord settings", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "settings.json"), "{\"enableHardwareAcceleration\": false, \"OPEN_ON_STARTUP\": false, \"MINIMIZE_TO_TRAY\": false, \"debugLogging\": false}"), () => Discord == true),

            // download vencord
            ("Downloading Vencord", async () => await ProcessActions.RunDownload("https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", ApplicationData.Current.TemporaryFolder.Path, "VencordInstallerCli.exe"), () => Discord == true),

            // install vencord
            ("Installing Vencord", async () => { await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VencordInstallerCli.exe")}"" -install -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("VencordInstallerCli.exe")).DeleteAsync(); }, () => Discord == true),

            // import vencord settings
            ("Importing Vencord settings", async () => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings")), () => Discord == true),
            ("Importing Vencord settings", async () => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true), () => Discord == true),
            ("Importing Vencord settings", async () => await Task.Delay(500), () => Discord == true),

            // log in to discord
            ("Please log in to your Discord account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "Discord.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Discord.lnk")), () => Discord == true),

            // debloat discord
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_cloudsync-1"), true); } catch { } }, () => Discord == true),
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_dispatch-1"), true); } catch { } }, () => Discord == true),
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_erlpack-1"), true); } catch { } }, () => Discord == true),
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_game_utils-1"), true); } catch { } }, () => Discord == true),
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_overlay2-1"), true); } catch { } }, () => Discord == true),
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_rpc-1"), true); } catch { } }, () => Discord == true),
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_spellcheck-1"), true); } catch { } }, () => Discord == true),
            ("Debloating Discord", async () => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_zstd-1"), true); } catch { } }, () => Discord == true),

            // download whatsapp
            ("Downloading WhatsApp", async () => await StoreHelper.Download("5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), () => WhatsApp == true),

            // install whatsapp
            ("Installing WhatsApp", async () => await StoreHelper.Install("5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), () => WhatsApp == true),
            ("Installing WhatsApp", async () => whatsAppVersion = StoreHelper.GetVersion("5319275A.WhatsAppDesktop_cv1g1gvanyjgm"), () => WhatsApp == true),

            // disable "minimize to system tray"
			(@"Disabling ""Minimize to system tray""", async () => await ProcessActions.RunPowerShellScript("whatsapp.ps1", ""), () => WhatsApp == true),

            // pin whatsapp to the taskbar
            ("Pinning WhatsApp to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path 5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App"), () => WhatsApp == true),

            // log in to whatsapp
            ("Please log in to your WhatsApp account", async () => await Task.Delay(1000), () => WhatsApp == true),
            ("Please log in to your WhatsApp account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\5319275A.WhatsAppDesktop_" + whatsAppVersion + "_x64__cv1g1gvanyjgm", "WhatsApp.Root.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync(), () => WhatsApp == true),

            // disable whatsapp startup entry
            ("Disabling WhatsApp startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\5319275A.WhatsAppDesktop_cv1g1gvanyjgm\2defd21c-0b9e-4e4e-873a-2a68c47d7da5", "State", 1, RegistryValueKind.DWord), () => WhatsApp == true),

            // download epic games launcher
            ("Downloading Epic Games Launcher", async () => await ProcessActions.RunDownload("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", ApplicationData.Current.TemporaryFolder.Path, "EpicGamesLauncherInstaller.msi"), () => EpicGames == true),

            // install epic games launcher
            ("Installing Epic Games Launcher", async () => { await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "EpicGamesLauncherInstaller.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("EpicGamesLauncherInstaller.msi")).DeleteAsync(); }, () => EpicGames == true),

            // remove epic games launcher desktop shortcut
            ("Removing Epic Games Launcher desktop shortcut", async () => File.Delete(@"C:\Users\Public\Desktop\Epic Games Launcher.lnk"), () => EpicGames == true),

            // update epic games launcher
            ("Updating Epic Games Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe")}) !.WaitForExitAsync(), () => EpicGames == true),
            ("Updating Epic Games Launcher", async () => { while (true) { foreach (var proc in Process.GetProcessesByName("EpicGamesLauncher")) { if (ProcessesHelper.GetCommandLine(proc).Contains("-AllowSoftwareRendering -SaveToUserDir -Messaging", StringComparison.OrdinalIgnoreCase)) { proc.Kill(); return; } } await Task.Delay(100); } }, () => EpicGames == true),
            
            // import epic games launcher account
            ("Importing Epic Games Launcher Account", async () => await EpicGamesHelper.RunImportEpicGamesLauncherAccount(), () => EpicGames == true && EpicGamesAccount == true),

            // import epic games launcher games
            ("Importing Epic Games Launcher Games", async () => await EpicGamesHelper.RunImportEpicGamesLauncherGames(), () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => Fortnite = File.Exists(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat") && (JsonNode.Parse(await File.ReadAllTextAsync(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat"))?["InstallationList"] is JsonArray installations) && installations.Any(entry => entry?["AppName"]?.ToString() == "Fortnite") , () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => await Task.Delay(1000), () => EpicGames == true && EpicGamesGames == true),

            // log in to epic games launcher account
            ("Please log in to your Epic Games Launcher account", async () => await EpicGamesHelper.EpicGamesLogin(), () => EpicGames == true && EpicGamesAccount == false),

            // disable epic games startup entries
            ("Disabling Epic Games startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicOnlineServices", "Start", 4, RegistryValueKind.DWord), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => ServicesHelper.StopService("EpicOnlineServices"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicGamesUpdater", "Start", 4, RegistryValueKind.DWord), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => ServicesHelper.StopService("EpicGamesUpdater"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "EpicGamesLauncher", new byte[] { 0x01 }, RegistryValueKind.Binary), () => EpicGames == true),
        
            // download steam
            ("Downloading Steam", async () => await ProcessActions.RunDownload("https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", ApplicationData.Current.TemporaryFolder.Path, "SteamSetup.exe"), () => Steam == true),

            // install steam
            ("Installing Steam", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "SteamSetup.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("SteamSetup.exe")).DeleteAsync(); }, () => Steam == true),

            // update steam
            ("Updating Steam", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Steam\Steam.exe") , WindowStyle = ProcessWindowStyle.Hidden }) !.WaitForExitAsync(), () => Steam == true),
            ("Updating Steam", async () => { while (Process.GetProcessesByName("steamwebhelper").Length == 0) await Task.Delay(500); }, () => Steam == true),

            // log in to steam
            ("Please log in to your Steam account", async () => await SteamHelper.SteamLogin(), () => Steam == true),

            // import steam games
            ("Importing Steam Games", async () => await SteamHelper.RunImportSteamGames(), () => Steam == true && SteamGames == true),

            // remove steam desktop shortcut
            ("Removing Steam desktop shortcut", async () => File.Delete(@"C:\Users\Public\Desktop\Steam.lnk"), () => Steam == true),

            // disable steam startup entry
            ("Disabling Steam startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Steam", new byte[] { 0x01 }, RegistryValueKind.Binary), () => Steam == true),

            // download riot client
            ("Downloading Riot Client", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/lhjc10gc9i31bptzw6ism/Riot-Games.zip?rlkey=07n3ek47oaus1olu86u08yw04&st=t0vspqv4&dl=0", ApplicationData.Current.TemporaryFolder.Path, "Riot Games.zip"), () => RiotClient == true),

            // install riot client
            ("Installing Riot Client", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Riot Games.zip"), @"C:\"); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Riot Games.zip")).DeleteAsync(); }, () => RiotClient == true),

            // log in to riot client
            ("Please log in to your Riot account", async () => { Process.Start(new ProcessStartInfo { FileName = @"C:\Riot Games\Riot Client\RiotClientServices.exe", WindowStyle = ProcessWindowStyle.Maximized }); while (Process.GetProcessesByName("RiotClientCrashHandler").Length == 0 || Process.GetProcessesByName("Riot Client").Length == 0) await Task.Delay(500); while (Process.GetProcessesByName("Riot Client").Length > 0) await Task.Delay(500); }, () => RiotClient == true),

            // disable riot client startup entry
            ("Disabling Riot Client startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "RiotClient", new byte[] { 0x01 }, RegistryValueKind.Binary), () => RiotClient == true),

            // download ea
            ("Downloading EA", async () => await ProcessActions.RunDownload("https://origin-a.akamaihd.net/EA-Desktop-Client-Download/installer-releases/EAappInstaller.exe", ApplicationData.Current.TemporaryFolder.Path, "EAappInstaller.exe"), () => EA == true),

            // install ea
            ("Installing EA", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "EAappInstaller.exe"), Arguments = "/s", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("EAappInstaller.exe")).DeleteAsync(); }, () => EA == true),

            // log in to ea
            ("Please log in to your EA account", async () => { Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EADesktop.exe", WindowStyle = ProcessWindowStyle.Maximized }); while (Process.GetProcessesByName("EADesktop").Length > 0) await Task.Delay(500); }, () => EA == true),

            // remove ea desktop shortcut
            ("Removing EA desktop shortcut", async () => File.Delete(@"C:\Users\Public\Desktop\EA.lnk"), () => EA == true),

            // download ubisoft connect
            ("Downloading Ubisoft Connect", async () => await ProcessActions.RunDownload("https://static3.cdn.ubi.com/orbit/launcher_installer/UbisoftConnectInstaller.exe", ApplicationData.Current.TemporaryFolder.Path, "UbisoftConnectInstaller.exe"), () => UbisoftConnect == true),

            // install ubisoft connect
            ("Installing Ubisoft Connect", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "UbisoftConnectInstaller.exe"), Arguments = "/S" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("UbisoftConnectInstaller.exe")).DeleteAsync(); }, () => UbisoftConnect == true),

            // log in to ubisoft connect
            ("Please log in to your Ubisoft Connect account", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\upc.exe") , WindowStyle = ProcessWindowStyle.Hidden }) !.WaitForExitAsync(), () => UbisoftConnect == true),

            // remove ubisoft connect desktop shortcut 
            ("Removing Ubisoft Connect desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Ubisoft Connect.lnk")), () => UbisoftConnect == true),

            // disable ubisoft connect startup entries
            ("Disabling Ubisoft Connect startup entries", async () => TaskSchedulerHelper.Toggle(@"\Ubisoft\Ubisoft Connect Background Update", false), () => UbisoftConnect == true),
            ("Disabling Ubisoft Connect startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\UpcElevationService", "Start", 4, RegistryValueKind.DWord), () => UbisoftConnect == true),
            ("Disabling Ubisoft Connect startup entries", async () => ServicesHelper.StopService("UpcElevationService"), () => UbisoftConnect == true),

            // download battle.net
            ("Downloading Battle.Net", async () => await ProcessActions.RunDownload("https://downloader.battle.net//download/getInstallerForGame?os=win&gameProgram=BATTLENET_APP&version=Live", ApplicationData.Current.TemporaryFolder.Path, "Battle.net-Setup.exe"), () => BattleNet == true),

            // install battle.net
            ("Installing Battle.Net", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Battle.net-Setup.exe"), Arguments = @"--lang=enUS --installpath=""C:\Program Files (x86)\Battle.net""" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Battle.net-Setup.exe")).DeleteAsync(); }, () => BattleNet == true),

            // log in to battle.net
            ("Please log in to your Battle.Net account", async () => { while (Process.GetProcessesByName("Battle.net").Length >= 1) await Task.Delay(500); }, () => BattleNet == true),

            // disable battle.net startup entries
            ("Disabling Battle.Net startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\battlenet_helpersvc", "Start", 4, RegistryValueKind.DWord), () => BattleNet == true),
            ("Disabling Battle.Net startup entries", async () => ServicesHelper.StopService("battlenet_helpersvc"), () => BattleNet == true),
            ("Disabling Battle.Net startup entries", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Battle.Net", new byte[] { 0x01 }, RegistryValueKind.Binary), () => BattleNet == true),

            // remove battle.net desktop shortcut
            ("Removing Battle.Net desktop shortcut", async () => File.Delete(@"C:\Users\Public\Desktop\Battle.net.lnk"), () => BattleNet == true),

            // download minecraft launcher
            ("Downloading Minecraft Launcher", async () => await ProcessActions.RunDownload("https://launcher.mojang.com/download/MinecraftInstaller.msi", ApplicationData.Current.TemporaryFolder.Path, "MinecraftInstaller.msi"), () => MinecraftLauncher == true),

            // install minecraft launcher
            ("Installing Minecraft Launcher", async () => { await Process.Start(new ProcessStartInfo { FileName = "msiexec.exe", Arguments = $@"/i ""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "MinecraftInstaller.msi")}"" /qn" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("MinecraftInstaller.msi")).DeleteAsync(); }, () => MinecraftLauncher == true),

            // update minecraft launcher
            ("Updating Minecraft Launcher", async () => { Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe" , WindowStyle = ProcessWindowStyle.Hidden }); while (Process.GetProcessesByName("MinecraftLauncher").Length == 1) await Task.Delay(500); while (Process.GetProcessesByName("MinecraftLauncher").Length == 0) await Task.Delay(500); while (Process.GetProcessesByName("MinecraftLauncher").Length == 1) await Task.Delay(100); }, () => MinecraftLauncher == true),

            // log in to minecraft launcher
            ("Please log in to your Minecraft Launcher account", async () => { while (Process.GetProcessesByName("MinecraftLauncher").Length > 1) await Task.Delay(500); }, () => MinecraftLauncher == true),

            // remove minecraft launcher desktop shortcut
            ("Removing Minecraft Launcher desktop shortcut", async () => File.Delete(@"C:\Users\Public\Desktop\Minecraft Launcher.lnk"), () => MinecraftLauncher == true),

            // download rockstar games launcher
            ("Downloading Rockstar Games Launcher", async () => await ProcessActions.RunDownload("https://gamedownloads.rockstargames.com/public/installer/Rockstar-Games-Launcher.exe", ApplicationData.Current.TemporaryFolder.Path, "Rockstar-Games-Launcher.exe"), () => RockstarGamesLauncher == true),
            
            // extract rockstar games launcher
            ("Extracting Rockstar Games Launcher", async () => rockstarGamesLauncherVersion = FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Rockstar-Games-Launcher.exe")).ProductVersion, () => RockstarGamesLauncher == true),
            ("Extracting Rockstar Games Launcher", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7z.exe"), Arguments = @$"x ""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Rockstar-Games-Launcher.exe")}"" -t# -aoa -bd -bb1 -o""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Rockstar-Games-Launcher")}"" -y"  , CreateNoWindow = true })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("Rockstar-Games-Launcher.exe")).DeleteAsync(); }, () => RockstarGamesLauncher == true),
            ("Extracting Rockstar Games Launcher", async () => { Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher"); Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher\Redistributables\VCRed"); Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Steam"); Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Epic"); }, () => RockstarGamesLauncher == true),

            // install rockstar games launcher
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\2.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-console-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\3.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-datetime-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\4.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-debug-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\5.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-errorhandling-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\6.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-file-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\7.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-file-l1-2-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\8.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-file-l2-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\9.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-handle-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\10.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-heap-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\11.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-interlocked-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\12.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-libraryloader-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\13.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-localization-l1-2-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\14.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-memory-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\15.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-namedpipe-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\16.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-processenvironment-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\17.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-processthreads-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\18.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-processthreads-l1-1-1.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\19.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-profile-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\20.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-rtlsupport-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\21.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-string-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\22.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-synch-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\23.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-synch-l1-2-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\24.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-sysinfo-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\25.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-timezone-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\26.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-util-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\27.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-conio-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\28.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-convert-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\29.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-environment-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\30.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-filesystem-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\31.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-heap-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\32.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-locale-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\33.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-math-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\34.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-multibyte-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\35.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-private-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\36.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-process-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\37.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-runtime-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\38.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-stdio-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\39.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-string-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\40.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-time-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\41.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-utility-l1-1-0.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\42.Launcher.exe"), @"C:\Program Files\Rockstar Games\Launcher\Launcher.exe", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\43"), @"C:\Program Files\Rockstar Games\Launcher\Launcher.rpf", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\44.LauncherPatcher.exe"), @"C:\Program Files\Rockstar Games\Launcher\LauncherPatcher.exe", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\45.dll"), @"C:\Program Files\Rockstar Games\Launcher\libovr.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\46.zip"), @"C:\Program Files\Rockstar Games\Launcher\offline.pak", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\48.RockstarService.exe"), @"C:\Program Files\Rockstar Games\Launcher\RockstarService.exe", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\49.RockstarSteamHelper.exe"), @"C:\Program Files\Rockstar Games\Launcher\RockstarSteamHelper.exe", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\50.ucrtbase.dll"), @"C:\Program Files\Rockstar Games\Launcher\ucrtbase.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\51.Rockstar-Games-Launcher.exe"), @"C:\Program Files\Rockstar Games\Launcher\uninstall.exe", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\52.exe"), @"C:\Program Files\Rockstar Games\Launcher\Redistributables\VCRed\vc_redist.x64.exe", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\53.exe"), @"C:\Program Files\Rockstar Games\Launcher\Redistributables\VCRed\vc_redist.x86.exe", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\54.steam_api.dll"), @"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Steam\steam_api64.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\55.EOSSDK-Win64-Shipping.dll"), @"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Epic\EOSSDK-Win64-Shipping.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\56.EOSSDK-Win64-Shipping.dll"), @"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Epic\EOSSDK-Win64-Shipping-1.14.2.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => File.Copy(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, @"Rockstar-Games-Launcher\57.XboxHelper.dll"), @"C:\Program Files\Rockstar Games\Launcher\RockstarXboxHelper.dll", true), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "DisplayName", "Rockstar Games Launcher", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "Comments", "Rockstar Games Launcher", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "UninstallString", "\"C:\\Program Files\\Rockstar Games\\Launcher\\uninstall.exe\"", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "QuietUninstallString", "\"C:\\Program Files\\Rockstar Games\\Launcher\\uninstall.exe\" /S", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "InstallLocation", "\"C:\\Program Files\\Rockstar Games\\Launcher\"", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "DisplayIcon", "C:\\Program Files\\Rockstar Games\\Launcher\\Launcher.exe, 0", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "Publisher", "Rockstar Games", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "HelpLink", "https://www.rockstargames.com/support", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "Readme", "https://www.rockstargames.com/support", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "URLUpdateInfo", "https://www.rockstargames.com", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "URLInfoAbout", "https://www.rockstargames.com/support", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "DisplayVersion", rockstarGamesLauncherVersion, RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "NoModify", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "NoRepair", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher", "EstimatedSize", 0x927c0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Version", rockstarGamesLauncherVersion, RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "InstallFolder", "C:\\Program Files\\Rockstar Games\\Launcher", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Language", "en-US", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Shortcut", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "Silent", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher", "RGL", 2552918, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "AUTO", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "BOOT", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "DEFDIR", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "DPI", 100, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "INSTVER", rockstarGamesLauncherVersion, RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "LANG", "en-US", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "REDIST", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "SHRT", 1, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "SIL", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "UPVER", 0, RegistryValueKind.DWord), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "PARPRO", "C:\\Windows\\explorer.exe", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add", "INSTPATH", "C:\\Program Files\\Rockstar Games\\Launcher", RegistryValueKind.String), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; New-Item -Path ([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Rockstar Games')) -ItemType Directory -Force | Out-Null; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Rockstar Games\Rockstar Games Launcher.lnk')); $Shortcut.TargetPath = 'C:\Program Files\Rockstar Games\Launcher\LauncherPatcher.exe'; $Shortcut.Save()"), () => RockstarGamesLauncher == true),

            // update rock star games launcher
            ("Updating Rockstar Games Launcher", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Rockstar Games\Launcher\LauncherPatcher.exe") , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); while (Process.GetProcessesByName("dxdiag").Length > 1) await Task.Delay(500); while (Process.GetProcessesByName("SocialClubHelper").Length == 0) await Task.Delay(500); }, () => RockstarGamesLauncher == true),

            // log in to rockstar games launcher
            ("Please log in to your Rockstar Games Launcher account", async () => { while (Process.GetProcessesByName("Launcher").Length == 1) await Task.Delay(500); }, () => RockstarGamesLauncher == true),
        
            // download fivem
            ("Downloading FiveM", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/tn48g2m1qisdsir80ixu8/FiveM.zip?rlkey=c54qzh36fr3p8yb09q4zlt0gi&st=ca6wjcgx&dl=0", ApplicationData.Current.TemporaryFolder.Path, "FiveM.zip"), () => FiveM == true),

            // install fivem
            ("Installing FiveM", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "FiveM.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("FiveM.zip")).DeleteAsync(); }, () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "DisplayName", "FiveM", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "DisplayIcon", "C:\\Users\\user\\AppData\\Local\\FiveM\\FiveM.exe,0", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "HelpLink", "https://cfx.re/", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "InstallLocation", "C:\\Users\\user\\AppData\\Local\\FiveM", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "Publisher", "Cfx.re", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "UninstallString", "\"C:\\Users\\user\\AppData\\Local\\FiveM\\FiveM.exe\" -uninstall app", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "URLInfoAbout", "https://cfx.re/", RegistryValueKind.String), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "NoModify", 1, RegistryValueKind.DWord), () => FiveM == true),
            ("Installing FiveM", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM", "NoRepair", 1, RegistryValueKind.DWord), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell; $p=[System.IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs'); $sc1=$s.CreateShortcut([System.IO.Path]::Combine($p,'FiveM.lnk')); $sc1.TargetPath=[System.IO.Path]::Combine($env:LOCALAPPDATA,'FiveM\FiveM.exe'); $sc1.Description='FiveM is a modification framework based on the Cfx.re platform'; $sc1.Save(); $sc2=$s.CreateShortcut([System.IO.Path]::Combine($p,'FiveM - Cfx.re Development Kit (FxDK).lnk')); $sc2.TargetPath=[System.IO.Path]::Combine($env:LOCALAPPDATA,'FiveM\FiveM - Cfx.re Development Kit (FxDK).lnk'); $sc2.Save()"), () => FiveM == true),
        
            // download faceit
            ("Downloading FACEIT", async () => await ProcessActions.RunDownload("https://faceit-client.faceit-cdn.net/release/FACEIT-setup-latest.exe", ApplicationData.Current.TemporaryFolder.Path, "FACEIT-setup-latest.exe"), () => FACEIT == true),

            // install faceit
            ("Installing FACEIT", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "FACEIT-setup-latest.exe"), Arguments = "/S", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("FACEIT-setup-latest.exe")).DeleteAsync(); }, () => FACEIT == true),

            // log in to faceit
            ("Please log in to your FACEIT account", async () => { while (Process.GetProcessesByName("FACEIT").Length > 1) await Task.Delay(500); }, () => FACEIT == true),

            // remove faceit desktop shortcut 
            ("Removing FACEIT desktop shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FACEIT.lnk")), () => FACEIT == true),

            // disable faceit startup entry
            ("Disabling FACEIT startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "FACEIT", new byte[] { 0x01 }, RegistryValueKind.Binary), () => FACEIT == true),
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
}
