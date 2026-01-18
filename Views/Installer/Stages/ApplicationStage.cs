using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Management;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class ApplicationStage
{
    public static bool? Fortnite;
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        string ScheduleMode = PreparingStage.ScheduleMode;
        string LightTime = PreparingStage.LightTime;
        string DarkTime = PreparingStage.DarkTime;

        bool? iCloud = PreparingStage.iCloud;
        bool? Bitwarden = PreparingStage.Bitwarden;
        bool? OnePassword = PreparingStage.OnePassword;
        bool? AlwaysShowTrayIcons = PreparingStage.AlwaysShowTrayIcons;

        bool? Word = PreparingStage.Word;
        bool? Excel = PreparingStage.Excel;
        bool? PowerPoint = PreparingStage.PowerPoint;
        bool? OneNote = PreparingStage.OneNote;
        bool? Teams = PreparingStage.Teams;
        bool? Outlook = PreparingStage.Outlook;
        bool? OneDrive = PreparingStage.OneDrive;

        bool? VisualStudio = PreparingStage.VisualStudio;
        bool? VisualStudioCode = PreparingStage.VisualStudioCode;
        bool? Git = PreparingStage.Git;
        bool? Python = PreparingStage.Python;
        bool? Nodejs = PreparingStage.Nodejs;
        bool? Trello = PreparingStage.Trello;

        bool? AppleMusic = PreparingStage.AppleMusic;
        bool? Tidal = PreparingStage.Tidal;
        bool? Qobuz = PreparingStage.Qobuz;
        bool? AmazonMusic = PreparingStage.AmazonMusic;
        bool? DeezerMusic = PreparingStage.DeezerMusic;
        bool? Spotify = PreparingStage.Spotify;
        bool? WhatsApp = PreparingStage.WhatsApp;
        bool? Discord = PreparingStage.Discord;
        bool? EpicGames = PreparingStage.EpicGames;
        bool? EpicGamesAccount = PreparingStage.EpicGamesAccount;
        bool? EpicGamesGames = PreparingStage.EpicGamesGames;
        bool? Steam = PreparingStage.Steam;
        bool? SteamGames = PreparingStage.SteamGames;
        bool? RiotClient = PreparingStage.RiotClient;
        bool? EA = PreparingStage.EA;
        bool? UbisoftConnect = PreparingStage.UbisoftConnect;
        bool? BattleNet = PreparingStage.BattleNet;
        bool? MinecraftLauncher = PreparingStage.MinecraftLauncher;
        bool? RockstarGamesLauncher = PreparingStage.RockstarGamesLauncher;
        bool? FiveM = PreparingStage.FiveM;
        bool? FACEIT = PreparingStage.FACEIT;

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
            // download heif image extension
            ("Downloading HEIF Image Extension", async () => await ProcessActions.RunMicrosoftStoreDownload("Microsoft.HEIFImageExtension", "f4ccc4c1-6e9a-49a0-8f19-46f1717c7ba3", "appxbundle", 0, false), null),

            // install heif image extension
            ("Installing HEIF Image Extension", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\Microsoft.HEIFImageExtension (Package)\"" | Select-Object -First 1).FullName"), null),

            // download mpeg-2 video extension
            ("Downloading MPEG-2 Video Extension", async () => await ProcessActions.RunMicrosoftStoreDownload("Microsoft.MPEG2VideoExtension", "886ca98c-991c-40d0-b374-1417d6d437a1", "appxbundle", 0, false), null),

            // install mpeg-2 video extension
            ("Installing MPEG-2 Video Extension", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\Microsoft.MPEG2VideoExtension (Package)\"" | Select-Object -First 1).FullName"), null),

            // download av1 video extension
            ("Downloading AV1 Video Extension", async () => await ProcessActions.RunMicrosoftStoreDownload("Microsoft.AV1VideoExtension", "6ff5769f-7177-4ca5-b6f4-939194121c82", "appxbundle", 0, false), null),

            // install av1 video extension
            ("Installing AV1 Video Extension", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\Microsoft.AV1VideoExtension (Package)\"" | Select-Object -First 1).FullName"), null),

            // download avc encoder video extension
            ("Downloading AVC Encoder Video Extension", async () => await ProcessActions.RunMicrosoftStoreDownload("Microsoft.AVCEncoderVideoExtension", "4be34c88-8464-488e-97bb-70acee4a55a0" ,"appxbundle", 0, false), null),

            // install avc encoder video extension
            ("Installing AVC Encoder Video Extension", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\Microsoft.AVCEncoderVideoExtension (Package)\"" | Select-Object -First 1).FullName"), null),

            // download dolby vision extension
            ("Downloading Dolby Vision Extension", async () => await ProcessActions.RunMicrosoftStoreDownload("DolbyLaboratories.DolbyVisionAccess", "c143786a-e5c5-4dc3-a480-601c65c31538", "msixbundle", 0, false), null),

            // install dolby vision extension
            ("Installing Dolby Vision Extension", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\DolbyLaboratories.DolbyVisionAccess (Package)\"" | Select-Object -First 1).FullName"), null),

            //// download movies & tv
            //("Downloading Movies & TV", async () => await ProcessActions.RunMicrosoftStoreDownload("Microsoft.ZuneVideo_8wekyb3d8bbwe", "64b22df1-5a9c-4c88-aa1f-42cefaf8b281", "appxbundle", 0, false), null),

            //// install movies & tv
            //("Installing Movies & TV", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\Microsoft.ZuneVideo_8wekyb3d8bbwe (Package)\"" | Select-Object -First 1).FullName"), null),

            // download icloud dependencies
            ("Downloading iCloud Dependencies", async () => await ProcessActions.RunMicrosoftStoreDownload("AppleInc.iCloud", "1e4f5d0e-4b36-4f9b-bfbc-9fec63fd0f1e", "", 0, true), () => iCloud == true),

            // install icloud
            ("Installing iCloud Dependencies", async () => await ProcessActions.RunPowerShell(@"Get-ChildItem -Path \""$env:TEMP\AppleInc.iCloud (Dependencies)\"" | ForEach-Object { Add-AppxPackage -Path $_.FullName -ErrorAction SilentlyContinue }"), () => iCloud == true),

            // download icloud
            ("Downloading iCloud", async () => await ProcessActions.RunMicrosoftStoreDownload("AppleInc.iCloud", "1e4f5d0e-4b36-4f9b-bfbc-9fec63fd0f1e", "appx", 0, false), () => iCloud == true),

            // install icloud
            ("Installing iCloud", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\AppleInc.iCloud (Package)\"" | Select-Object -First 1).FullName"), () => iCloud == true),
            ("Installing iCloud", async () => icloudVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AppleInc.iCloud\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim();}), () => iCloud == true),

            // log in to icloud
            ("Please log in to your iCloud account", async () => await ProcessActions.Sleep(1000), () => iCloud == true),
            ("Please log in to your iCloud account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.iCloud_" + icloudVersion + "_x64__nzyj5cx40ttqa", "iCloud", "iCloudHome.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => iCloud == true),

            // disable icloud startup entries
            ("Disabling iCloud startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudHomeStartupTask"" /v State /t REG_DWORD /d 1 /f"), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudDriveStartupTask"" /v State /t REG_DWORD /d 1 /f"), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudCKKSStartupTask"" /v State /t REG_DWORD /d 1 /f"), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudPhotosStartupTask"" /v State /t REG_DWORD /d 1 /f"), () => iCloud == true),
            ("Disabling iCloud startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\AppleInc.iCloud_nzyj5cx40ttqa\iCloudPhotoStreamsStartupTask"" /v State /t REG_DWORD /d 1 /f"), () => iCloud == true),

            // download bitwarden
            ("Downloading Bitwarden", async () => await ProcessActions.RunMicrosoftStoreDownload("8bitSolutionsLLC.bitwardendesktop", "98b94e11-5303-4222-8c4b-8e039b5f9d31", "appx", 0, false), () => Bitwarden == true),

            // install bitwarden
            ("Installing Bitwarden", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\8bitSolutionsLLC.bitwardendesktop (Package)\"" | Select-Object -First 1).FullName"), () => Bitwarden == true),
            ("Installing Bitwarden", async () => bitwardenVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"8bitSolutionsLLC.bitwardendesktop\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => Bitwarden == true),

            // log in to bitwarden
            ("Please log in to your Bitwarden account", async () => await ProcessActions.Sleep(1000), () => Bitwarden == true),
            ("Please log in to your Bitwarden account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\8bitSolutionsLLC.bitwardendesktop_" + bitwardenVersion + "_x64__h4e712dmw3xyy", "app", "Bitwarden.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => Bitwarden == true),

            // download 1password
            ("Downloading 1Password", async () => await ProcessActions.RunDownload("https://downloads.1password.com/win/1PasswordSetup-latest.exe", Path.GetTempPath(), "1PasswordSetup-latest.exe"), () => OnePassword == true),

            // install 1password
            ("Installing 1Password", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\1PasswordSetup-latest.exe"" --silent"), () => OnePassword == true),
            ("Installing 1Password", async () => onePasswordVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\1PasswordSetup-latest.exe")).ProductVersion), () => OnePassword == true),
            ("Installing 1Password", async () => { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "settings", "settings.json"); Directory.CreateDirectory(Path.GetDirectoryName(path) !); await File.WriteAllTextAsync(path, "{ \"version\": 1, \"updates.updateChannel\": \"PRODUCTION\", \"authTags\": {}, \"app.keepInTray\": false }"); }, () => OnePassword == true),

            // log in to 1password
            ("Please log in to your 1Password account", async () => await Task.Run(() => Process.GetProcessesByName("1Password").ToList().ForEach(p => p.Kill())), () => OnePassword == true),
            ("Please log in to your 1Password account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "1Password", "app", onePasswordVersion, "1Password.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => OnePassword == true),

            // download nanazip
            ("Downloading NanaZip", async () => await ProcessActions.RunMicrosoftStoreDownload("40174MouriNaruto.NanaZip", "6045570b-8398-4779-90e1-d9aef6f18823", "msixbundle", 0, false), null),

            // install nanazip
            ("Installing NanaZip", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\40174MouriNaruto.NanaZip (Package)\"" | Select-Object -First 1).FullName"), null),

            //// download files
            //("Downloading Files", async () => await ProcessActions.RunDownload("https://files.community/appinstallers/Files.stable.appinstaller", Path.GetTempPath(), "Files.stable.appinstaller"), null),

            //// install files
            //("Installing Files", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -AppInstallerFile ""$env:TEMP\Files.stable.appinstaller"""), null),
            //("Installing Files", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/u2hcpijo21p8i0u6lj6qm/Files.zip?rlkey=e5pq2cbj4sevh5lf5jfmvv5hc&st=8o8frer3&dl=0", Path.GetTempPath(), "Files.zip"), null),
            //("Installing Files", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "Files.zip"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "Files_1y0xx7n9077q4", "LocalState")), null),
            //("Installing Files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command"" /ve /t REG_EXPAND_SZ /d ""\""%LOCALAPPDATA%\\Files\\Files.App.Launcher.exe\"" \""%1\"""" /f"), null),
            //("Installing Files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Folder\shell\open\command"" /v ""DelegateExecute"" /t REG_SZ /d ""2"" /f"), null),
            //("Installing Files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Folder\shell\explore\command"" /ve /t REG_EXPAND_SZ /d ""\""%LOCALAPPDATA%\\Files\\Files.App.Launcher.exe\"" \""%1\"""" /f"), null),
            //("Installing Files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Folder\shell\explore\command"" /v ""DelegateExecute"" /t REG_SZ /d ""2"" /f"), null),
            //("Installing Files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}\shell\opennewwindow\command"" /ve /t REG_EXPAND_SZ /d ""\""%LOCALAPPDATA%\\Files\\Files.App.Launcher.exe\"""" /f"), null),
            //("Installing Files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}\shell\opennewwindow\command"" /v ""DelegateExecute"" /t REG_SZ /d ""2"" /f"), null),

            // download everything
            ("Downloading Everything", async () => await ProcessActions.RunDownload("https://www.voidtools.com/Everything-1.5.0.1404a.x64-Setup.exe", Path.GetTempPath(), "Everything.exe"), null),
            
            // install everything
            ("Installing Everything", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\Everything.exe"" /S"), null),
            ("Installing Everything", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""%APPDATA%\Everything"""), null),
            ("Installing Everything", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "Everything-1.5a.ini"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything", "Everything-1.5a.ini"), true)), null),
            ("Installing Everything", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Everything 1.5a\Everything.exe", WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-install-run-on-system-startup"})), null),
            ("Installing Everything", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Everything 1.5a\Everything.exe", WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-startup", })), null),

            // remove everything desktop shortcut 
            ("Removing Everything desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Everything 1.5a.lnk"""), null),

            // download windhawk
            ("Downloading Windhawk", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/omk2gg29v8yguskw4jhng/Windhawk.zip?rlkey=tljvtfus2tq57d3y5mzdt8ges&st=5h7z80ir&dl=0", Path.GetTempPath(), "Windhawk.zip"), null),
        
            // install windhawk
            ("Installing Windhawk", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "Windhawk.zip"), @"C:\Program Files\Windhawk"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c move ""C:\Program Files\Windhawk\Windhawk"" ""%ProgramData%\Windhawk"""), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "windhawk.reg")}\""), null),
            //("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings"" /v LightThemePath /t REG_SZ /d {LightThemePath}  /f"), null),
            //("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings"" /v DarkThemePath /t REG_SZ /d {DarkThemePath} /f"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher"" /v Disabled /t REG_DWORD /d 1 /f"), () => ScheduleMode == "Always Light" || ScheduleMode == "Always Dark"),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings"" /v ScheduleMode /t REG_SZ /d {scheduleMode} /f"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings"" /v CustomLight /t REG_SZ /d {LightTime} /f"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher\Settings"" /v CustomDark /t REG_SZ /d {DarkTime} /f"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\taskbar-notification-icons-show-all"" /v Disabled /t REG_DWORD /d 1 /f"), () => AlwaysShowTrayIcons == false),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc create Windhawk binPath= ""\""C:\Program Files\Windhawk\windhawk.exe\"" -service"" start= auto"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs\Windhawk.lnk'));$sc.TargetPath='C:\Program Files\Windhawk\windhawk.exe';$sc.Save()"), null),
            ("Installing Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc start Windhawk"), null),
            
            // download startallback
            ("Downloading StartAllBack", async () => await ProcessActions.RunDownload("https://www.startallback.com/download.php", Path.GetTempPath(), "StartAllBackSetup.exe"), null),

            // install startallback
            ("Installing StartAllBack", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "startallback.reg")}\""), null),
            ("Installing StartAllBack", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\StartAllBackSetup.exe"" /silent /allusers"), null),
            ("Installing StartAllBack", async () => await ProcessActions.RunNsudo("CurrentUser", @"SCHTASKS /Change /TN ""StartAllBack Update"" /Disable"), null),

            // activate startallback
            ("Activating StartAllBack", async () => await ProcessActions.RunPowerShellScript("startallback.ps1", ""), null),
            ("Activating StartAllBack", async () => await ProcessActions.Sleep(2000), null),

            // download process explorer
            ("Downloading Process Explorer", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/a8l16rp3cpcvkkryavix1/procexp64.exe?rlkey=5fec8mcmkfcxlum9a95o1xn3t&st=mjkrpc1f&dl=0", @"C:\Windows", "procexp64.exe"), null),
            //("Downloading Process Explorer", async () => await ProcessActions.RunDownload("https://download.sysinternals.com/files/ProcessExplorer.zip", Path.GetTempPath(), "ProcessExplorer.zip"), null),

            // install process explorer
            //("Installing Process Explorer", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "ProcessExplorer.zip"), Path.Combine(Path.GetTempPath(), "ProcessExplorer")), null),
            //("Installing Process Explorer", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), "ProcessExplorer", "procexp64.exe"), @"C:\Windows\procexp64.exe", true)), null),
            ("Installing Process Explorer", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "processexplorer.reg")}\""), null),
            ("Installing Process Explorer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\taskmgr.exe"" /v Debugger /t REG_SZ /d ""\""C:\Windows\procexp64.exe\"""" /f"), null),
            ("Installing Process Explorer", async () => await ProcessActions.Sleep(500), null),

            // download office
            ("Downloading Office", async () => await ProcessActions.RunDownload("https://officecdn.microsoft.com/pr/wsus/setup.exe", Path.GetTempPath(), "setup.exe"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

            // install office
            ("Installing Office", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "configuration.xml"), Path.Combine(Path.GetTempPath(), "configuration.xml"), true)), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Installing Office", async () => await Task.Run(() => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Word").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }), () => Word == true),
            ("Installing Office", async () => await Task.Run(() => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Excel").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }), () => Excel == true),
            ("Installing Office", async () => await Task.Run(() => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "PowerPoint").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }), () => PowerPoint == true),
            ("Installing Office", async () => await Task.Run(() => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OneNote").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }), () => OneNote == true),
            ("Installing Office", async () => await Task.Run(() => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "Teams").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }), () => Teams == true),
            ("Installing Office", async () => await Task.Run(() => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OutlookForWindows").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }), () => Outlook == true),
            ("Installing Office", async () => await Task.Run(() => { var doc = XDocument.Load(Path.Combine(Path.GetTempPath(), "configuration.xml")); doc.Root.Descendants("ExcludeApp").Where(x => (string)x.Attribute("ID") == "OneDrive").Remove(); doc.Save(Path.Combine(Path.GetTempPath(), "configuration.xml")); }), () => OneDrive == true),
            ("Installing Office", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\setup.exe"" /configure ""%TEMP%\configuration.xml"""), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

            // disable office startup entries
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CLASSES_ROOT\PROTOCOLS\Filter\AutorunsDisabled\text/xml\CLSID"" /t REG_SZ /d ""{807583E5-5146-11D5-A672-00B0D022E945}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_CLASSES_ROOT\PROTOCOLS\Filter\text/xml"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb-roaming.16\CLSID"" /t REG_SZ /d ""{83C25742-A9F7-49FB-9138-434302C88D07}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\mso-minsb-roaming.16"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16\CLSID"" /t REG_SZ /d ""{42089D2D-912D-4018-9087-2B87803E93FB}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\osf-roaming.16\CLSID"" /t REG_SZ /d ""{42089D2D-912D-4018-9087-2B87803E93FB}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\osf-roaming.16"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\osf.16\CLSID"" /t REG_SZ /d ""{5504BE45-A83B-4808-900A-3A5C36E7F77A}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\osf.16"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""(Default)"" /t REG_SZ /d ""Lync Click to Call BHO"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""NoExplorer"" /t REG_SZ /d ""1"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""(Default)"" /t REG_SZ /d ""Lync Click to Call"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""MenuText"" /t REG_SZ /d ""Lync Click to Call"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""Icon"" /t REG_SZ /d ""C:\Program Files\Microsoft Office\root\VFS\ProgramFilesX86\Microsoft Office\Office16\lync.exe,1"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""HotIcon"" /t REG_SZ /d ""C:\Program Files\Microsoft Office\root\VFS\ProgramFilesX86\Microsoft Office\Office16\lync.exe,1"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""CLSID"" /t REG_SZ /d ""{1FBA04EE-3024-11d2-8F1F-0000F87ABD16}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""ClsidExtension"" /t REG_SZ /d ""{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""Default Visible"" /t REG_SZ /d ""Yes"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\AutorunsDisabled\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /v ""ButtonText"" /t REG_SZ /d ""Lync Click to Call"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\Extensions\{31D09BA0-12F5-4CCE-BE8A-2923E76605DA}"" /f"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\Microsoft\Office\Office Actions Server"" /Disable"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\Microsoft\Office\Office Automatic Updates 2.0"" /Disable"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\Microsoft\Office\Office Background Push Maintenance"" /Disable"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\Microsoft\Office\Office ClickToRun Service Monitor"" /Disable"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\Microsoft\Office\Office Feature Updates"" /Disable"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\Microsoft\Office\Office Feature Updates Logon"" /Disable"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\Microsoft\Office\Office Performance Monitor"" /Disable"), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\AutorunsDisabled\mso-minsb.16\CLSID"" /t REG_SZ /d ""{42089D2D-912D-4018-9087-2B87803E93FB}"" /f"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_CLASSES_ROOT\PROTOCOLS\Handler\mso-minsb.16"" /f"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /Change /TN ""\OneDrive Per-Machine Standalone Update Task"" /Disable"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunPowerShell(@"Get-ScheduledTask | Where-Object {$_.TaskName -like 'OneDrive Reporting Task*'} | Disable-ScheduledTask"), () => OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FileSyncHelper"" /v Start /t REG_DWORD /d 4 /f & sc stop ""FileSyncHelper"""), () => OneDrive == true),
            ("Disabling Office startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OneDrive Updater Service"" /v Start /t REG_DWORD /d 4 /f & sc stop ""OneDrive Updater Service"""), () => OneDrive == true),

            // disable office telemetry
            ("Disabling Office telemetry", async () => await ProcessActions.RunPowerShellScript("disableofficetelemetry.ps1", ""), () => Word == true || Excel == true || PowerPoint == true || OneNote == true || Teams == true || Outlook == true || OneDrive == true),

            // download visual studio
            ("Downloading Visual Studio", async () => await ProcessActions.RunDownload("https://aka.ms/vs/stable/vs_community.exe", Path.GetTempPath(), "vs_Community.exe"), () => VisualStudio == true),

            // install visual studio
            ("Installing Visual Studio", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\vs_Community.exe"" --quiet --wait"), () => VisualStudio == true),

            // optimize visual studio
            ("Optimizing Visual Studio", async () => await Task.Run(async () => { while (Process.GetProcessesByName("VSNgenRunner").Length == 1) await Task.Delay(500); }), () => VisualStudio == true),

            // pin visual studio to the taskbar
            ("Pinning Visual Studio to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type Link -Path ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Visual Studio.lnk"""), () => VisualStudio == true),

            // download mica visual studio
            ("Downloading Mica Visual Studio", async () => await ProcessActions.RunDownload("https://github.com/Tech5G5G/Mica-Visual-Studio/releases/latest/download/MicaVisualStudio.vsix", Path.GetTempPath(), "MicaVisualStudio.vsix"), () => VisualStudio == true),

            // install mica visual studio
            ("Installing Mica Visual Studio", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe"" /quiet /admin %TEMP%\MicaVisualStudio.vsix"), () => VisualStudio == true),

            // download xaml styler
            ("Downloading XAML Styler", async () => await ProcessActions.RunDownload("https://marketplace.visualstudio.com/_apis/public/gallery/publishers/TeamXavalon/vsextensions/XAMLStyler2022/3.2501.8/vspackage", Path.GetTempPath(), "XamlStyler.Extension.Windows.VS2022.vsix"), () => VisualStudio == true),

            // install xaml styler
            ("Installing XAML Styler", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe"" /quiet /admin %TEMP%\XamlStyler.Extension.Windows.VS2022.vsix"), () => VisualStudio == true),

            // download visual studio code
            ("Downloading Visual Studio Code", async () => await ProcessActions.RunDownload("https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user", Path.GetTempPath(), "VSCodeUserSetup-x64.exe"), () => VisualStudioCode == true),

            // install visual studio code
            ("Installing Visual Studio Code", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\VSCodeUserSetup-x64.exe"" /VERYSILENT /NORESTART /MERGETASKS=!runcode"), () => VisualStudioCode ==  true),
            
            // download git
            ("Downloading Git", async () => await ProcessActions.RunDownload("https://github.com/git-for-windows/git/releases/download/v2.52.0.windows.1/Git-2.52.0-64-bit.exe", Path.GetTempPath(), "Git64-bit.exe"), () => Git == true),

            // install git
            ("Installing Git", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\Git64-bit.exe"" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART"), () => Git ==  true),

            // download pyton
            ("Downloading Pyton", async () => await ProcessActions.RunDownload("https://www.python.org/ftp/python/3.14.2/python-3.14.2-amd64.exe", Path.GetTempPath(), "python-3.14.2-amd64.exe"), () => Python == true),

            // install python
            ("Installing Python", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\python-3.14.2-amd64.exe"" /quiet InstallAllUsers=1 PrependPath=1"), () => Python == true),
           
            // download nodejs
            ("Downloading Node.js", async () => await ProcessActions.RunDownload("https://nodejs.org/dist/v24.12.0/node-v24.12.0-x64.msi", Path.GetTempPath(), "node-v24.12.0-x64.msi"), () => Nodejs == true),

            // install nodejs
            ("Installing Node.js", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\node-v24.12.0-x64.msi"" /qn"), () => Nodejs ==  true),

            // download trello
            ("Downloading Trello", async () => await ProcessActions.RunMicrosoftStoreDownload("45273LiamForsyth.PawsforTrello", "ccb2a6eb-4608-4202-a709-929b87799a92", "appx", 1, false), () => Trello == true),

            // install trello
            ("Installing Trello", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\45273LiamForsyth.PawsforTrello (Package)\"" | Select-Object -First 1).FullName"), () => Trello == true),
            ("Installing Trello", async () => trelloVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"45273LiamForsyth.PawsforTrello\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => Trello == true),

            // pin trello to the taskbar
            ("Pinning Trello to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path 45273LiamForsyth.PawsforTrello_7pb5ddty8z1pa!trello"), () => Trello == true),

            // log in to trello
            ("Please log in to your Trello account", async () => await ProcessActions.Sleep(1000), () => Trello == true),
            ("Please log in to your Trello account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\45273LiamForsyth.PawsforTrello_" + trelloVersion + @"_x64__7pb5ddty8z1pa\app", "Trello.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => Trello == true),

            // download dolby access
            ("Downloading Dolby Access", async () => await ProcessActions.RunMicrosoftStoreDownload("DolbyLaboratories.DolbyAccess", "61e179bf-d7a6-4201-aa9b-88cf1bcbc472", "msixbundle", 1, false), () => AppleMusic == true),

            // install dolby access
            ("Installing Dolby Access", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\DolbyLaboratories.DolbyAccess (Package)\"" | Select-Object -First 1).FullName"), () => AppleMusic == true),
            ("Installing Dolby Access", async () => dolbyAccessVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"DolbyLaboratories.DolbyAccess\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => AppleMusic == true),

            // log in to dolby access
            ("Please log in to your Dolby Access account", async () => await ProcessActions.Sleep(1000), () => AppleMusic == true),
            ("Please log in to your Dolby Access account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\DolbyLaboratories.DolbyAccess_" + dolbyAccessVersion + "_x64__rz1tebttyb220", "DolbyAccess.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => AppleMusic == true),

            // download apple music
            ("Downloading Apple Music", async () => await ProcessActions.RunMicrosoftStoreDownload("AppleInc.AppleMusicWin", "cf497837-70f4-4c2a-9b9d-3d5767379bb1", "msixbundle", 0, false), () => AppleMusic == true),

            // install apple music
            ("Installing Apple Music", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\AppleInc.AppleMusicWin (Package)\"" | Select-Object -First 1).FullName"), () => AppleMusic == true),
            ("Installing Apple Music", async () => appleMusicVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AppleInc.AppleMusicWin\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => AppleMusic == true),
            
            // enable keep miniplayer on top of all other windows
            ("Enabling Keep Miniplayer on top of all other windows", async () => await ProcessActions.RunPowerShellScript("applemusic.ps1", ""), () => AppleMusic == true),

            // pin apple music to the taskbar
            ("Pinning Apple Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path AppleInc.AppleMusicWin_nzyj5cx40ttqa!App"), () => AppleMusic == true),

            // log in to apple music
            ("Please log in to your Apple Music account", async () => await ProcessActions.Sleep(1000), () => AppleMusic == true),
            ("Please log in to your Apple Music account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AppleInc.AppleMusicWin_" + appleMusicVersion + "_x64__nzyj5cx40ttqa", "AppleMusic.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => AppleMusic == true),

            // download tidal
            ("Downloading TIDAL", async () => await ProcessActions.RunMicrosoftStoreDownload("WiMPMusic.27241E05630EA", "b938b446-5909-4b21-8034-c0eee2fa0bb5", "appx", 0, false), () => Tidal == true),

            // install tidal
            ("Installing TIDAL", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\WiMPMusic.27241E05630EA (Package)\"" | Select-Object -First 1).FullName"), () => Tidal == true),
            ("Installing TIDAL", async () => tidalVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"WiMPMusic.27241E05630EA\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => Tidal == true),

            // pin tidal to the taskbar
            ("Pinning TIDAL to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path WiMPMusic.27241E05630EA_kn85bz84x7te4!TIDAL"), () => Tidal == true),

            // log in to tidal
            ("Please log in to your TIDAL account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\WiMPMusic.27241E05630EA_" + tidalVersion + @"_x86__kn85bz84x7te4\app", "TIDAL.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => Tidal == true),

            // download qobuz
            ("Downloading Qobuz", async () => await ProcessActions.RunDownload("https://desktop.qobuz.com/releases/win32/x64/windows7_8_10/8.1.0-b019/Qobuz_Installer.exe", Path.GetTempPath(), "Qobuz_Installer.exe"), () => Qobuz == true),

            // install qobuz
            ("Installing Qobuz", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\Qobuz_Installer.exe"" -s"), () => Qobuz == true),

            // pin qobuz to the taskbar
            ("Pinning Qobuz to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Qobuz\Qobuz.lnk")}"""), () => Qobuz == true),

            // log in to qobuz
            ("Please log in to your Qobuz account", async () => await ProcessActions.Sleep(1000), () => Qobuz == true),
            ("Please log in to your Qobuz account", async () => { await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Qobuz", "Qobuz.exe"), WindowStyle = ProcessWindowStyle.Maximized })!.WaitForExitAsync()); while (Process.GetProcessesByName("Qobuz").Length > 2) await Task.Delay(500); }, () => Qobuz == true),

            // download amazon music
            ("Downloading Amazon Music", async () => await ProcessActions.RunMicrosoftStoreDownload("AmazonMobileLLC.AmazonMusic", "7fb9f901-50c2-4974-a65c-01b4cd17ca77", "appx", 0, false), () => AmazonMusic == true),

            // install amazon music
            ("Installing Amazon Music", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\AmazonMobileLLC.AmazonMusic (Package)\"" | Select-Object -First 1).FullName"), () => AmazonMusic == true),
            ("Installing Amazon Music", async () => amazonMusicVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"AmazonMobileLLC.AmazonMusic\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => AmazonMusic == true),

            // pin amazon music to the taskbar
            ("Pinning Amazon Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path AmazonMobileLLC.AmazonMusic_kc6t79cpj4tp0!AmazonMobileLLC.AmazonMusic"), () => AmazonMusic == true),

            // log in to amazon music
            ("Please log in to your Amazon Music account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\AmazonMobileLLC.AmazonMusic_" + amazonMusicVersion + "_x86__kc6t79cpj4tp0", "Amazon Music.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => AmazonMusic == true),

            // download deezer music
            ("Downloading Deezer Music", async () => await ProcessActions.RunMicrosoftStoreDownload("Deezer.62021768415AF", "9ba24187-b508-4235-ad59-e78e833322a4", "appxbundle", 0, false), () => DeezerMusic == true),

            // install deezer music
            ("Installing Deezer Music", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\Deezer.62021768415AF (Package)\"" | Select-Object -First 1).FullName"), () => DeezerMusic == true),
            ("Installing Deezer Music", async () => deezerMusicVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"Deezer.62021768415AF\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => DeezerMusic == true),

            // pin deezer music to the taskbar
            ("Pinning Deezer Music to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path Deezer.62021768415AF_q7m17pa7q8kj0!Deezer.Music"), () => DeezerMusic == true),

            // log in to deezer music
            ("Please log in to your Deezer Music account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\Deezer.62021768415AF_" + deezerMusicVersion + @"_x86__q7m17pa7q8kj0\app", "Deezer.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => DeezerMusic == true),

            // download spotify
            ("Downloading Spotify", async () => await ProcessActions.RunDownload("https://download.scdn.co/SpotifyFullSetupX64.exe", Path.GetTempPath(), "SpotifyFullSetupX64.exe"), () => Spotify == true),

            // install spotify
            ("Installing Spotify", async () => spotifyVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\SpotifyFullSetupX64.exe")).ProductVersion), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\SpotifyFullSetupX64.exe"" /extract ""%APPDATA%\Spotify"""), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayIcon"" /t REG_SZ /d ""%AppData%\Spotify\Spotify.exe"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayName"" /t REG_SZ /d ""Spotify"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""DisplayVersion"" /t REG_SZ /d ""{spotifyVersion}"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""InstallLocation"" /t REG_SZ /d ""%AppData%\Spotify"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""NoModify"" /t REG_DWORD /d 1 /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""NoRepair"" /t REG_DWORD /d 1 /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""Publisher"" /t REG_SZ /d ""Spotify AB"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""Publisher"" /t REG_SZ /d ""Spotify AB"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""UninstallString"" /t REG_SZ /d ""%AppData%\Spotify\Spotify.exe /uninstall"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""URLInfoAbout"" /t REG_SZ /d ""https://www.spotify.com"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunNsudo("CurrentUser", $@"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spotify"" /v ""Version"" /t REG_SZ /d ""{spotifyVersion}"" /f"), () => Spotify == true),
            ("Installing Spotify", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Spotify.lnk')); $Shortcut.TargetPath = [System.IO.Path]::Combine($env:APPDATA, 'Spotify\Spotify.exe'); $Shortcut.Save()"), () => Spotify == true),

            // pin spotify to the taskbar
            ("Pinning Spotify to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Spotify.lnk")}"""), () => Spotify == true),

            // disable spotify hardware acceleration
            ("Disabling Spotify hardware acceleration", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "prefs"), "ui.hardware_acceleration=false"), () => Spotify == true),

            // download spotx
            ("Downloading SpotX", async () => await ProcessActions.RunDownload("https://raw.githubusercontent.com/SpotX-Official/SpotX/main/run.ps1", Path.GetTempPath(), "run.ps1"), () => Spotify == true),

            // install spotx
            ("Installing SpotX", async () => await ProcessActions.RunPowerShell($@"& $env:TEMP\run.ps1 -new_theme -adsections_off -podcasts_off -block_update_off -version {spotifyVersion}-1234"), () => Spotify == true),

            // log in to spotify
            ("Please log in to your Spotify account", async () => await ProcessActions.Sleep(1000), () => Spotify == true),
            ("Please log in to your Spotify account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => Spotify == true),
            
            // remove spotify desktop shortcut
            ("Removing Spotify desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Spotify.lnk"""), () => Spotify == true),

            // disable spotify startup entry
            ("Disabling Spotify startup entry", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Spotify"" /t REG_BINARY /d ""01"" /f"), () => Spotify == true),

            // download discord
            ("Downloading Discord", async () => await ProcessActions.RunDownload("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64", Path.GetTempPath(), "DiscordSetup.exe"), () => Discord == true),

            // install discord
            ("Installing Discord", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\DiscordSetup.exe"" /silent"), () => Discord == true),
            ("Installing Discord", async () => discordVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\DiscordSetup.exe")).ProductVersion), () => Discord == true),
            ("Installing Discord", async () => await Task.Run(() => File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "installer.db"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "installer.db"), true)), () => Discord == true),

            // pin discord to the taskbar
            ("Pinning Discord to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", $@"-Type Link -Path ""{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Discord Inc\Discord.lnk")}"""), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Discord.lnk"""), () => Discord == true),

            // disable discord startup entry
            ("Disabling Discord startup entry", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Discord"" /t REG_BINARY /d ""01"" /f"), () => Discord == true),

            // optimize discord settings
            ("Optimizing Discord settings", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "settings.json"), "{\"enableHardwareAcceleration\": false, \"OPEN_ON_STARTUP\": false, \"MINIMIZE_TO_TRAY\": false, \"debugLogging\": false}"), () => Discord == true),

            // download vencord
            ("Downloading Vencord", async () => await ProcessActions.RunDownload("https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", Path.GetTempPath(), "VencordInstallerCli.exe"), () => Discord == true),

            // install vencord
            ("Installing Vencord", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\VencordInstallerCli.exe"" -install -branch auto"), () => Discord == true),

            // import vencord settings
            ("Importing Vencord settings", async () => await Task.Run(() => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings"))), () => Discord == true),
            ("Importing Vencord settings", async () => await Task.Run(() => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true)), () => Discord == true),
            ("Importing Vencord settings", async () => await ProcessActions.Sleep(500), () => Discord == true),

            // log in to discord
            ("Please log in to your Discord account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "Discord.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => Discord == true),

            // remove discord desktop shortcut 
            ("Removing Discord desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Discord.lnk"""), () => Discord == true),

            // debloat discord
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_cloudsync-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_dispatch-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_erlpack-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_game_utils-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_overlay2-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_rpc-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_spellcheck-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_zstd-1"), true); } catch { } }), () => Discord == true),

            // download whatsapp
            ("Downloading WhatsApp", async () => await ProcessActions.RunMicrosoftStoreDownload("5319275A.WhatsAppDesktop", "3dadc9b1-3603-496c-a6d1-bf2fda81df89", "msixbundle", 0, false), () => WhatsApp == true),

            // install whatsapp
            ("Installing WhatsApp", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\5319275A.WhatsAppDesktop (Package)\"" | Select-Object -First 1).FullName"), () => WhatsApp == true),
            ("Installing WhatsApp", async () => whatsAppVersion = await Task.Run(() => { var process = new Process { StartInfo = new ProcessStartInfo("powershell.exe", "Get-AppxPackage -Name \"5319275A.WhatsAppDesktop\" | Select-Object -ExpandProperty Version") { RedirectStandardOutput = true, CreateNoWindow = true } }; process.Start(); return process.StandardOutput.ReadToEnd().Trim(); }), () => WhatsApp == true),

            // pin whatsapp to the taskbar
            ("Pinning WhatsApp to the taskbar", async () => await ProcessActions.RunPowerShellScript("taskbarpin.ps1", @"-Type UWA -Path 5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App"), () => WhatsApp == true),

            // log in to whatsapp
            ("Please log in to your WhatsApp account", async () => await ProcessActions.Sleep(1000), () => WhatsApp == true),
            ("Please log in to your WhatsApp account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\WindowsApps\5319275A.WhatsAppDesktop_" + whatsAppVersion + "_x64__cv1g1gvanyjgm", "WhatsApp.Root.exe"), WindowStyle = ProcessWindowStyle.Maximized }) !.WaitForExitAsync()), () => WhatsApp == true),

            // disable whatsapp startup entry
            ("Disabling WhatsApp startup entry", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\5319275A.WhatsAppDesktop_cv1g1gvanyjgm\2defd21c-0b9e-4e4e-873a-2a68c47d7da5"" /v State /t REG_DWORD /d 1 /f"), () => WhatsApp == true),

            // download epic games launcher
            ("Downloading Epic Games Launcher", async () => await ProcessActions.RunDownload("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", Path.GetTempPath(), "EpicGamesLauncherInstaller.msi"), () => EpicGames == true),

            // install epic games launcher
            ("Installing Epic Games Launcher", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\EpicGamesLauncherInstaller.msi"" /qn"), () => EpicGames == true),

            // remove epic games launcher desktop shortcut
            ("Removing Epic Games Launcher desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Epic Games Launcher.lnk"""), () => EpicGames == true),

            // update epic games launcher
            ("Updating Epic Games Launcher", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe") }) !.WaitForExitAsync()), () => EpicGames == true),
            ("Updating Epic Games Launcher", async () => { while (true) { using (var searcher = new ManagementObjectSearcher($"SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'EpicGamesLauncher.exe'")) { foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>()) { string cmdLine = obj["CommandLine"]?.ToString() ?? ""; int pid = Convert.ToInt32(obj["ProcessId"]); if (cmdLine.Contains(@"""C:/Program Files/Epic Games/Launcher/Portal/Binaries/Win64/EpicGamesLauncher.exe""  -AllowSoftwareRendering -SaveToUserDir -Messaging", StringComparison.OrdinalIgnoreCase)) { try { Process.GetProcessById(pid).Kill(); return; } catch { } } } } await Task.Delay(100); } }, () => EpicGames == true),

            // import epic games launcher account
            ("Importing Epic Games Launcher Account", async () => await ProcessActions.RunImportEpicGamesLauncherAccount(), () => EpicGames == true && EpicGamesAccount == true),

            // import epic games launcher games
            ("Importing Epic Games Launcher Games", async () => await ProcessActions.RunImportEpicGamesLauncherGames(), () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => Fortnite = File.Exists(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat") && (JsonNode.Parse(await File.ReadAllTextAsync(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat"))?["InstallationList"] is JsonArray installations) && installations.Any(entry => entry?["AppName"]?.ToString() == "Fortnite") , () => EpicGames == true && EpicGamesGames == true),
            ("Importing Epic Games Launcher Games", async () => await ProcessActions.Sleep(1000), () => EpicGames == true && EpicGamesGames == true),

            // log in to epic games launcher account
            ("Please log in to your Epic Games Launcher account", async () => await ProcessActions.EpicGamesLogin(), () => EpicGames == true && EpicGamesAccount == false),

            // disable epic games startup entries
            ("Disabling Epic Games startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicOnlineServices"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop EpicOnlineServices"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EpicGamesUpdater"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop EpicGamesUpdater"), () => EpicGames == true),
            ("Disabling Epic Games startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""EpicGamesLauncher"" /t REG_BINARY /d ""01"" /f"), () => EpicGames == true),
        
            // download steam
            ("Downloading Steam", async () => await ProcessActions.RunDownload("https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", Path.GetTempPath(), "SteamSetup.exe"), () => Steam == true),

            // install steam
            ("Installing Steam", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\SteamSetup.exe"" /S"), () => Steam == true),

            // update steam
            ("Updating Steam", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Steam\Steam.exe") }) !.WaitForExitAsync()), () => Steam == true),

            // log in to steam
            ("Please log in to your Steam account", async () => await ProcessActions.SteamLogin(), () => Steam == true),

            // import steam games
            ("Importing Steam Games", async () => await ProcessActions.RunImportSteamGames(), () => Steam == true && SteamGames == true),

            // remove steam desktop shortcut
            ("Removing Steam desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Steam.lnk"""), () => Steam == true),

            // disable steam startup entries
            ("Disabling Steam startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Steam Client Service"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop ""Steam Client Service"""), () => Steam == true),
            ("Disabling Steam startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Steam"" /t REG_BINARY /d ""01"" /f"), () => Steam == true),

            // download riot client
            ("Downloading Riot Client", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/lhjc10gc9i31bptzw6ism/Riot-Games.zip?rlkey=07n3ek47oaus1olu86u08yw04&st=t0vspqv4&dl=0", Path.GetTempPath(), "Riot Games.zip"), () => RiotClient == true),

            // install riot client
            ("Installing Riot Client", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "Riot Games.zip"), @"C:\"), () => RiotClient == true),

            // log in to riot client
            ("Please log in to your Riot account", async () => await Task.Run(async () => { Process.Start(new ProcessStartInfo { FileName = @"C:\Riot Games\Riot Client\RiotClientServices.exe", WindowStyle = ProcessWindowStyle.Maximized }); while (Process.GetProcessesByName("RiotClientCrashHandler").Length == 0 || Process.GetProcessesByName("Riot Client").Length == 0) await Task.Delay(500); while (Process.GetProcessesByName("Riot Client").Length > 0) await Task.Delay(500); }), () => RiotClient == true),

            // disable riot client startup entry
            ("Disabling Riot Client startup entry", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""RiotClient"" /t REG_BINARY /d ""01"" /f"), () => RiotClient == true),

            // download ea
            ("Downloading EA", async () => await ProcessActions.RunDownload("https://origin-a.akamaihd.net/EA-Desktop-Client-Download/installer-releases/EAappInstaller.exe", Path.GetTempPath(), "EAappInstaller.exe"), () => EA == true),

            // install ea
            ("Installing EA", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\EAappInstaller.exe"" /s"), () => EA == true),

            // log in to ea
            ("Please log in to your EA account", async () => await Task.Run(async () => { Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EADesktop.exe", WindowStyle = ProcessWindowStyle.Maximized }); while (Process.GetProcessesByName("EADesktop").Length > 0) await Task.Delay(500); }), () => EA == true),

            // remove ea desktop shortcut
            ("Removing EA desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\EA.lnk"""), () => EA == true),

            // download ubisoft connect
            ("Downloading Ubisoft Connect", async () => await ProcessActions.RunDownload("https://static3.cdn.ubi.com/orbit/launcher_installer/UbisoftConnectInstaller.exe", Path.GetTempPath(), "UbisoftConnectInstaller.exe"), () => UbisoftConnect == true),

            // install ubisoft connect
            ("Installing Ubisoft Connect", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\UbisoftConnectInstaller.exe"" /S"), () => UbisoftConnect == true),

            // log in to ubisoft connect
            ("Please log in to your Ubisoft Connect account", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\upc.exe") }) !.WaitForExitAsync()), () => UbisoftConnect == true),

            // remove ubisoft connect desktop shortcut 
            ("Removing Ubisoft Connect desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\Ubisoft Connect.lnk"""), () => UbisoftConnect == true),

            // disable ubisoft connect startup entries
            ("Disabling Ubisoft Connect startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"schtasks /change /tn ""\Ubisoft\Ubisoft Connect Background Update"" /disable"), () => UbisoftConnect == true),
            ("Disabling Ubisoft Connect startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\UpcElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop UpcElevationService"), () => UbisoftConnect == true),

            // download battle.net
            ("Downloading Battle.Net", async () => await ProcessActions.RunDownload("https://downloader.battle.net//download/getInstallerForGame?os=win&gameProgram=BATTLENET_APP&version=Live", Path.GetTempPath(), "Battle.net-Setup.exe"), () => BattleNet == true),

            // install battle.net
            ("Installing Battle.Net", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "Battle.net-Setup.exe"), Arguments = @"--lang=enUS --installpath=""C:\Program Files (x86)\Battle.net""" })!.WaitForExit()), () => BattleNet == true),

            // log in to battle.net
            ("Please log in to your Battle.Net account", async () => await Task.Run(async () => { while (Process.GetProcessesByName("Battle.net").Length >= 1) await Task.Delay(500); }), () => BattleNet == true),

            // disable battle.net startup entries
            ("Disabling Battle.Net startup entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\battlenet_helpersvc"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop battlenet_helpersvc"), () => BattleNet == true),
            ("Disabling Battle.Net startup entries", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Battle.Net"" /t REG_BINARY /d ""01"" /f"), () => BattleNet == true),

            // remove battle.net desktop shortcut
            ("Removing Battle.Net desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Battle.net.lnk"""), () => BattleNet == true),

            // download minecraft launcher
            ("Downloading Minecraft Launcher", async () => await ProcessActions.RunDownload("https://launcher.mojang.com/download/MinecraftInstaller.msi", Path.GetTempPath(), "MinecraftInstaller.msi"), () => MinecraftLauncher == true),

            // install minecraft launcher
            ("Installing Minecraft Launcher", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c ""%TEMP%\MinecraftInstaller.msi"" /qn"), () => MinecraftLauncher == true),

            // update minecraft launcher
            ("Updating Minecraft Launcher", async () => await Task.Run(async () => { Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\Minecraft Launcher\MinecraftLauncher.exe" }); while (Process.GetProcessesByName("MinecraftLauncher").Length == 1) await Task.Delay(500); while (Process.GetProcessesByName("MinecraftLauncher").Length == 0) await Task.Delay(500); while (Process.GetProcessesByName("MinecraftLauncher").Length == 1) await Task.Delay(100); }), () => MinecraftLauncher == true),

            // log in to minecraft launcher
            ("Please log in to your Minecraft Launcher account", async () => await Task.Run(async () => { while (Process.GetProcessesByName("MinecraftLauncher").Length > 1) await Task.Delay(500); }), () => MinecraftLauncher == true),

            // remove minecraft launcher desktop shortcut
            ("Removing Minecraft Launcher desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""C:\Users\Public\Desktop\Minecraft Launcher.lnk"""), () => MinecraftLauncher == true),

            // download rockstar games launcher
            ("Downloading Rockstar Games Launcher", async () => await ProcessActions.RunDownload("https://gamedownloads.rockstargames.com/public/installer/Rockstar-Games-Launcher.exe", Path.GetTempPath(), "Rockstar-Games-Launcher.exe"), () => RockstarGamesLauncher == true),
            
            // extract rockstar games launcher
            ("Extracting Rockstar Games Launcher", async () => rockstarGamesLauncherVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"%TEMP%\Rockstar-Games-Launcher.exe")).ProductVersion), () => RockstarGamesLauncher == true),
            ("Extracting Rockstar Games Launcher", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7z.exe"), Arguments = @$"x ""{Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher.exe")}"" -t# -aoa -bd -bb1 -o""{Path.Combine(Path.GetTempPath(), "Rockstar-Games-Launcher")}"" -y", CreateNoWindow = true })!.WaitForExitAsync(), () => RockstarGamesLauncher == true),
            ("Extracting Rockstar Games Launcher", async () => await Task.Run(() => { Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher"); Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher\Redistributables\VCRed"); Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Steam"); Directory.CreateDirectory(@"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Epic"); }), () => RockstarGamesLauncher == true),

            // install rockstar games launcher
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\2.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-console-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\3.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-datetime-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\4.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-debug-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\5.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-errorhandling-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\6.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-file-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\7.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-file-l1-2-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\8.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-file-l2-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\9.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-handle-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\10.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-heap-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\11.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-interlocked-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\12.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-libraryloader-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\13.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-localization-l1-2-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\14.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-memory-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\15.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-namedpipe-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\16.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-processenvironment-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\17.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-processthreads-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\18.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-processthreads-l1-1-1.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\19.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-profile-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\20.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-rtlsupport-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\21.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-string-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\22.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-synch-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\23.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-synch-l1-2-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\24.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-sysinfo-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\25.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-timezone-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\26.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-core-util-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\27.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-conio-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\28.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-convert-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\29.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-environment-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\30.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-filesystem-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\31.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-heap-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\32.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-locale-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\33.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-math-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\34.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-multibyte-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\35.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-private-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\36.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-process-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\37.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-runtime-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\38.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-stdio-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\39.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-string-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\40.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-time-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\41.apisetstub"), @"C:\Program Files\Rockstar Games\Launcher\api-ms-win-crt-utility-l1-1-0.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\42.Launcher.exe"), @"C:\Program Files\Rockstar Games\Launcher\Launcher.exe", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\43"), @"C:\Program Files\Rockstar Games\Launcher\Launcher.rpf", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\44.LauncherPatcher.exe"), @"C:\Program Files\Rockstar Games\Launcher\LauncherPatcher.exe", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\45.dll"), @"C:\Program Files\Rockstar Games\Launcher\libovr.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\46.zip"), @"C:\Program Files\Rockstar Games\Launcher\offline.pak", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\48.RockstarService.exe"), @"C:\Program Files\Rockstar Games\Launcher\RockstarService.exe", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\49.RockstarSteamHelper.exe"), @"C:\Program Files\Rockstar Games\Launcher\RockstarSteamHelper.exe", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\50.ucrtbase.dll"), @"C:\Program Files\Rockstar Games\Launcher\ucrtbase.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\51.Rockstar-Games-Launcher.exe"), @"C:\Program Files\Rockstar Games\Launcher\uninstall.exe", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\52.exe"), @"C:\Program Files\Rockstar Games\Launcher\Redistributables\VCRed\vc_redist.x64.exe", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\53.exe"), @"C:\Program Files\Rockstar Games\Launcher\Redistributables\VCRed\vc_redist.x86.exe", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\54.steam_api.dll"), @"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Steam\steam_api64.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\55.EOSSDK-Win64-Shipping.dll"), @"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Epic\EOSSDK-Win64-Shipping.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\56.EOSSDK-Win64-Shipping.dll"), @"C:\Program Files\Rockstar Games\Launcher\ThirdParty\Epic\EOSSDK-Win64-Shipping-1.14.2.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await Task.Run(() => File.Copy(Path.Combine(Path.GetTempPath(), @"Rockstar-Games-Launcher\57.XboxHelper.dll"), @"C:\Program Files\Rockstar Games\Launcher\RockstarXboxHelper.dll", true)), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v DisplayName /t REG_SZ /d ""Rockstar Games Launcher"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v Comments /t REG_SZ /d ""Rockstar Games Launcher"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v UninstallString /t REG_SZ /d ""\""C:\Program Files\Rockstar Games\Launcher\uninstall.exe\"""" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v QuietUninstallString /t REG_SZ /d ""\""C:\Program Files\Rockstar Games\Launcher\uninstall.exe\"" /S"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v InstallLocation /t REG_SZ /d ""\""C:\Program Files\Rockstar Games\Launcher\"""" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v DisplayIcon /t REG_SZ /d ""C:\Program Files\Rockstar Games\Launcher\Launcher.exe, 0"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v Publisher /t REG_SZ /d ""Rockstar Games"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v HelpLink /t REG_SZ /d ""https://www.rockstargames.com/support"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v Readme /t REG_SZ /d ""https://www.rockstargames.com/support"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v URLUpdateInfo /t REG_SZ /d ""https://www.rockstargames.com"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v URLInfoAbout /t REG_SZ /d ""https://www.rockstargames.com/support"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v DisplayVersion /t REG_SZ /d ""{rockstarGamesLauncherVersion}"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v NoModify /t REG_DWORD /d 1 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v NoRepair /t REG_DWORD /d 1 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Rockstar Games Launcher"" /v EstimatedSize /t REG_DWORD /d 0x927c0 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher"" /v Version /t REG_SZ /d ""{rockstarGamesLauncherVersion}"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher"" /v InstallFolder /t REG_SZ /d ""C:\Program Files\Rockstar Games\Launcher"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher"" /v Language /t REG_SZ /d ""en-US"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher"" /v Shortcut /t REG_DWORD /d 1 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher"" /v Silent /t REG_DWORD /d 0 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher"" /v RGL /t REG_DWORD /d 2552918 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v AUTO /t REG_DWORD /d 1 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v BOOT /t REG_DWORD /d 0 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v DEFDIR /t REG_DWORD /d 1 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v DPI /t REG_DWORD /d 100 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v INSTVER /t REG_SZ /d ""{rockstarGamesLauncherVersion}"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v LANG /t REG_SZ /d ""en-US"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v REDIST /t REG_DWORD /d 0 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v SHRT /t REG_DWORD /d 1 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v SIL /t REG_DWORD /d 0 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v UPVER /t REG_DWORD /d 0 /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v PARPRO /t REG_SZ /d ""C:\Windows\explorer.exe"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\Add"" /v INSTPATH /t REG_SZ /d ""C:\Program Files\Rockstar Games\Launcher"" /f"), () => RockstarGamesLauncher == true),
            ("Installing Rockstar Games Launcher", async () => await ProcessActions.RunPowerShell(@"$Shell = New-Object -ComObject WScript.Shell; New-Item -Path ([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Rockstar Games')) -ItemType Directory -Force | Out-Null; $Shortcut = $Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Rockstar Games\Rockstar Games Launcher.lnk')); $Shortcut.TargetPath = 'C:\Program Files\Rockstar Games\Launcher\LauncherPatcher.exe'; $Shortcut.Save()"), () => RockstarGamesLauncher == true),

            // update rock star games launcher
            ("Updating Rockstar Games Launcher", async () => await Task.Run(async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(@"C:\Program Files\Rockstar Games\Launcher\LauncherPatcher.exe") })!.WaitForExitAsync(); while (Process.GetProcessesByName("dxdiag").Length > 1) await Task.Delay(500); while (Process.GetProcessesByName("SocialClubHelper").Length == 0) await Task.Delay(500); }), () => RockstarGamesLauncher == true),

            // log in to rockstar games launcher
            ("Please log in to your Rockstar Games Launcher account", async () => await Task.Run(async () => { while (Process.GetProcessesByName("Launcher").Length == 1) await Task.Delay(500); }), () => RockstarGamesLauncher == true),
        
            // download fivem
            ("Downloading FiveM", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/tn48g2m1qisdsir80ixu8/FiveM.zip?rlkey=c54qzh36fr3p8yb09q4zlt0gi&st=ca6wjcgx&dl=0", Path.GetTempPath(), "FiveM.zip"), () => FiveM == true),

            // install fivem
            ("Installing FiveM", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "FiveM.zip"), Path.Combine(Path.GetTempPath(), "FiveM")), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c move ""%TEMP%\FiveM"" ""%LOCALAPPDATA%\FiveM"""), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v DisplayName /t REG_SZ /d ""FiveM"" /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v DisplayIcon /t REG_SZ /d ""C:\Users\user\AppData\Local\FiveM\FiveM.exe,0"" /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v HelpLink /t REG_SZ /d ""https://cfx.re/"" /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v InstallLocation /t REG_SZ /d ""C:\Users\user\AppData\Local\FiveM"" /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v Publisher /t REG_SZ /d ""Cfx.re"" /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v UninstallString /t REG_SZ /d ""\""C:\Users\user\AppData\Local\FiveM\FiveM.exe\"" -uninstall app"" /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v URLInfoAbout /t REG_SZ /d ""https://cfx.re/"" /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v NoModify /t REG_DWORD /d 1 /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\CitizenFX_FiveM"" /v NoRepair /t REG_DWORD /d 1 /f"), () => FiveM == true),
            ("Installing FiveM", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell; $p=[System.IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs'); $sc1=$s.CreateShortcut([System.IO.Path]::Combine($p,'FiveM.lnk')); $sc1.TargetPath=[System.IO.Path]::Combine($env:LOCALAPPDATA,'FiveM\FiveM.exe'); $sc1.Description='FiveM is a modification framework based on the Cfx.re platform'; $sc1.Save(); $sc2=$s.CreateShortcut([System.IO.Path]::Combine($p,'FiveM - Cfx.re Development Kit (FxDK).lnk')); $sc2.TargetPath=[System.IO.Path]::Combine($env:LOCALAPPDATA,'FiveM\FiveM - Cfx.re Development Kit (FxDK).lnk'); $sc2.Save()"), () => FiveM == true),
        
            // download faceit
            ("Downloading FACEIT", async () => await ProcessActions.RunDownload("https://faceit-client.faceit-cdn.net/release/FACEIT-setup-latest.exe", Path.GetTempPath(), "FACEIT-setup-latest.exe"), () => FACEIT == true),

            // install faceit
            ("Installing FACEIT", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\FACEIT-setup-latest.exe"" /S"), () => FACEIT == true),

            // log in to faceit
            ("Please log in to your FACEIT account", async () => await Task.Run(async () => { while (Process.GetProcessesByName("FACEIT").Length > 1) await Task.Delay(500); }), () => FACEIT == true),

            // remove faceit desktop shortcut 
            ("Removing FACEIT desktop shortcut", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /f /q ""%HOMEPATH%\Desktop\FACEIT.lnk"""), () => FACEIT == true),

            // disable faceit startup entry
            ("Disabling FACEIT startup entry", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""FACEIT"" /t REG_BINARY /d ""01"" /f"), () => FACEIT == true),
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