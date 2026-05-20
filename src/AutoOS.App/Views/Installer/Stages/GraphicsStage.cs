using AutoOS.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Monitor;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class GraphicsStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static async Task<List<(string Title, Func<Task> Action, Func<bool> Condition)>> GetActions()
    {
        var GPUs = PreparingStage.GPUs;
        bool MSI = PreparingStage.MSI;
        bool CRU = PreparingStage.CRU;
        bool NVIDIA = GPUs.Any(gpu => gpu.VendorId == "10de");
        bool AMD = GPUs.Any(gpu => gpu.VendorId == "1002");
        bool INTEL = GPUs.Any(gpu => gpu.VendorId == "8086");

        InIHelper iniHelper = new(Path.Combine(Path.GetTempPath(), "obs-studio", "basic", "profiles", "Untitled", "basic.ini"));
        string obsVersion = "";

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // system -> display -> graphics -> default graphics settings
            (@"Enabling ""Hardware-accelerated GPU scheduling"" (HAGS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Optimizations for windowed games""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings", "SwapEffectUpgradeEnable=1;", RegistryValueKind.String), null),

            // apply custom resolution utility (cru) profile
            ("Importing Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
            ("Importing Custom Resolution Utility (CRU) profile", async () => await Process.Start(new ProcessStartInfo { FileName = localSettings.Values["CruProfile"]?.ToString(), Arguments = "-i", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe"), "/q") { CreateNoWindow = true })!.WaitForExitAsync(), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(2000), () => CRU == true),

            // set the highest supported refresh rate for every monitor
            ("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(1000), null),
            ("Setting the highest supported refresh rate for every monitor", async () => MonitorHelper.SetHighestRefreshRates(), null),
            ("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(3000), null),

            // download msi afterburner
            ("Downloading MSI Afterburner", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/6dvl62kgm3z38x49752bt/MSI-Afterburner.zip?rlkey=h2m2riyjisrb3ph0i8j0q4eu5&st=l87whmmi&dl=0", Path.GetTempPath(), "MSI Afterburner.zip", new InstallPageReporter()), null),

            // install msi afterburner
            ("Installing MSI Afterburner", async () => { await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "MSI Afterburner.zip"), @"C:\Program Files (x86)\MSI Afterburner"); File.Delete(Path.Combine(Path.GetTempPath(), "MSI Afterburner.zip")); }, null),
            ("Installing MSI Afterburner", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\Redist\vc_redist.x86.exe", Arguments = "/q", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayIcon", @"C:\Program Files (x86)\MSI Afterburner\uninstall.exe", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayName", "MSI Afterburner 4.6.6", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayVersion", "4.6.6", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "Publisher", "MSI Co., LTD", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "UninstallString", @"C:\Program Files (x86)\MSI Afterburner\uninstall.exe", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => { string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\MSI Afterburner"); Directory.CreateDirectory(startMenuPath); Directory.CreateDirectory(Path.Combine(startMenuPath, "SDK")); }, null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunPowerShell(@"$Shell=New-Object -ComObject WScript.Shell; @(@{P='MSI Afterburner.lnk';T='C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe'},@{P='ReadMe.lnk';T='C:\Program Files (x86)\MSI Afterburner\Doc\ReadMe.pdf'},@{P='Uninstall.lnk';T='C:\Program Files (x86)\MSI Afterburner\Uninstall.exe'},@{P='SDK\MSI Afterburner localization reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\Localization reference.pdf'},@{P='SDK\MSI Afterburner skin format reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\USF skin format reference.pdf'},@{P='SDK\Samples.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Samples\'}) | % {$Shortcut=$Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\MSI Afterburner', $_.P)); $Shortcut.TargetPath=$_.T; $Shortcut.Save()}"), null),

            // import msi afterburner profile
            ("Importing MSI Afterburner profile", async () => File.Copy(localSettings.Values["MsiProfile"]?.ToString(), Path.Combine(@"C:\Program Files (x86)\MSI Afterburner\Profiles\", Path.GetFileName(localSettings.Values["MsiProfile"]?.ToString()))), () => MSI == true),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MSI == true),
        
            // download obs studio
            ("Downloading OBS Studio", async () => await DownloadHelper.Download(JsonDocument.Parse(await new HttpClient { DefaultRequestHeaders = { { "User-Agent", "AutoOS" } } }.GetStringAsync("https://api.github.com/repos/obsproject/obs-studio/releases/latest")).RootElement.GetProperty("assets").EnumerateArray().First(a => a.GetProperty("name").GetString().Contains("Windows-x64-Installer.exe")).GetProperty("browser_download_url").GetString(), Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe", new InstallPageReporter()), null),
            ("Downloading OBS Studio settings", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/gkhuws75qnckr63lnfbzn/obs-studio.zip?rlkey=6ziow6s1a85a7s5snrdi7v1x2&st=db3yzo4m&dl=0", Path.GetTempPath(), "obs-studio.zip", new InstallPageReporter()), null),
            ("Downloading OBS Studio uninstaller", async () => await DownloadHelper.Download("https://www.dl.dropboxusercontent.com/scl/fi/k8dboxunne9wk5j955n0u/uninstall.exe?rlkey=4egb9y4mbsg7pboczrrulto98&st=xmldubc2&dl=0", @"C:\Program Files\obs-studio", "uninstall.exe", new InstallPageReporter()), null),

            // install obs studio
            ("Installing OBS Studio", async () => { await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe"), @"C:\Program Files\obs-studio"); File.Delete(Path.Combine(Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe")); }, null),
            ("Installing OBS Studio", async () => { await ExtractHelper.Extract(Path.Combine(Path.GetTempPath(), "obs-studio.zip"), Path.Combine(Path.GetTempPath(), "obs-studio")); File.Delete(Path.Combine(Path.GetTempPath(), "obs-studio.zip")); }, null),
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false && AMD == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false &&  AMD == true),
            ("Installing OBS Studio", async () => Directory.Move(@"C:\Program Files\obs-studio\$APPDATA\obs-studio-hook", Environment.ExpandEnvironmentVariables(@"%ProgramData%\obs-studio-hook")), null),
            ("Installing OBS Studio", async () => { Directory.Move(Path.Combine(Path.GetTempPath(), "obs-studio"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio")); }, null),
            ("Installing OBS Studio", async () => Directory.Delete(@"C:\Program Files\obs-studio\$PLUGINSDIR", true), null),
            ("Installing OBS Studio", async () => Directory.Delete(@"C:\Program Files\obs-studio\$APPDATA", true), null),
            ("Installing OBS Studio", async () => obsVersion = FileVersionInfo.GetVersionInfo(@"C:\Program Files\obs-studio\bin\64bit\obs64.exe").ProductVersion, null),
            ("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "DisplayVersion", obsVersion, RegistryValueKind.String), null),
            ("Installing OBS Studio", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @$"import ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "obs.reg")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([System.IO.Path]::Combine($env:ProgramData,'Microsoft\Windows\Start Menu\Programs\OBS Studio.lnk'));$sc.TargetPath='C:\Program Files\obs-studio\bin\64bit\obs64.exe';$sc.WorkingDirectory='C:\Program Files\obs-studio\bin\64bit';$sc.Save()"), null)
        };

        var gpus = PreparingStage.GPUs.Where(gpu => gpu.Install).ToList();

        var latestDrivers = new Dictionary<string, (string Version, string Url)>();
        var driverInstallActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
        var driverTweakActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();

        foreach (var gpu in gpus)
        {
            (string newestVersion, string newestDownloadUrl) = gpu.VendorId switch
            {
                "10de" => await NvidiaHelper.CheckUpdate(gpu),
                "1002" => await AmdHelper.CheckUpdate(gpu),
                "8086" => await IntelHelper.CheckUpdate(gpu),
                _ => ("", "")
            };

            if (!latestDrivers.TryGetValue(gpu.VendorId, out var driver) || driver.Version != newestVersion)
            {
                latestDrivers[gpu.VendorId] = (newestVersion, newestDownloadUrl);

                switch (gpu.VendorId)
                {
                    case "10de":
                        driverInstallActions.AddRange(NvidiaHelper.InstallActions(gpu, newestVersion, newestDownloadUrl, new InstallPageReporter()));
                        break;
                    case "1002":
                        driverInstallActions.AddRange(AmdHelper.InstallActions(gpu, newestVersion, newestDownloadUrl, new InstallPageReporter()));
                        break;
                    case "8086":
                        driverInstallActions.AddRange(IntelHelper.InstallActions(gpu, newestVersion, newestDownloadUrl, new InstallPageReporter()));
                        break;
                }
            }

            switch (gpu.VendorId)
            {
                case "10de":
                    driverTweakActions.AddRange(NvidiaHelper.TweakActions(gpu));
                    break;
                case "1002":
                    driverTweakActions.AddRange(AmdHelper.TweakActions(gpu));
                    break;
                case "8086":
                    driverTweakActions.AddRange(IntelHelper.TweakActions(gpu));
                    break;
            }
        }

        return [.. driverInstallActions, .. actions.Take(2), .. actions.Skip(2).Take(8), .. driverTweakActions, .. actions.Skip(10)];
    }
}

