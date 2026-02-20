using AutoOS.Views.Installer.Actions;
using PuppeteerSharp;
using System.Text.RegularExpressions;

namespace AutoOS.Helpers.GPU
{
    public static class IntelHelper
    {
        public static async Task<(string newestVersion, string newestDownloadUrl)> CheckUpdate(GpuInfo gpu)
        {
            string deviceId = gpu.DeviceId;
            string codename = gpu.Codename;
            string driverPageUrl = string.Empty;
            string newestVersion = string.Empty;
            string newestDownloadUrl = string.Empty;

            static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

            string[] intel6th = ["Skylake", "Apollo Lake"];
            string[] intel7to10 = ["Kaby Lake", "Coffee Lake", "Whiskey Lake", "Comet Lake", "Ice Lake", "Lakefield", "Elkhart Lake"];
            string[] intel11to14 = ["Tiger Lake", "Alder Lake", "Raptor Lake", "DG1"];
            string[] intelArc = ["Arc", "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake"];

            bool is6thGen = intel6th.Any(c => Normalize(codename).Contains(Normalize(c)));
            bool is7to10 = intel7to10.Any(c => Normalize(codename).Contains(Normalize(c)));
            bool is11to14 = intel11to14.Any(c => Normalize(codename).Contains(Normalize(c)));
            bool isArc = intelArc.Any(c => Normalize(codename).Contains(Normalize(c)));

            if (is6thGen)
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/762755/intel-6th-gen-processor-graphics-windows.html";
            else if (is7to10)
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/776137/intel-7th-10th-gen-processor-graphics-windows.html";
            else if (is11to14)
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/864990/intel-11th-14th-gen-processor-graphics-windows.html";
            else if (isArc)
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/785597/intel-arc-graphics-windows.html";
            else
                throw new InvalidOperationException($"Unsupported Codename: {codename}");

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
            });

            var page = await browser.NewPageAsync();
            await page.GoToAsync(driverPageUrl, new NavigationOptions { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });

            string bodyText = await page.EvaluateFunctionAsync<string>("() => document.body.innerText");
            string domHtml = await page.EvaluateFunctionAsync<string>("() => document.documentElement.outerHTML");

            var versionMatch = Regex.Match(bodyText, @"(\d+\.\d+\.\d+\.\d+)\s*\(Latest\)", RegexOptions.IgnoreCase);
            if (versionMatch.Success) newestVersion = versionMatch.Groups[1].Value;

            string filePattern = is6thGen ? @"(gfx_win_[0-9.]+\.zip)" : @"(gfx_win_[0-9.]+\.exe)";
            var fileMatch = Regex.Match(domHtml, filePattern, RegexOptions.IgnoreCase);
            string fileName = fileMatch.Success ? fileMatch.Groups[1].Value : string.Empty;

            var idMatch = Regex.Match(domHtml, @"downloadmirror\.intel\.com\/(\d+)", RegexOptions.IgnoreCase);
            string fileId = idMatch.Success ? idMatch.Groups[1].Value : string.Empty;

            if (!string.IsNullOrEmpty(fileId) && !string.IsNullOrEmpty(fileName))
                newestDownloadUrl = $"https://downloadmirror.intel.com/{fileId}/{fileName}";

            return (newestVersion, newestDownloadUrl);
        }

        public static List<(string Title, Func<Task> Action, Func<bool> Condition)> DriverActions(GpuInfo gpu, string newestDownloadUrl, ProgressButton progressButton = null)
        {
            string codename = gpu.Codename;
            static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

            string[] intel6th = ["Skylake", "Apollo Lake"];
            string[] intel7to10 = ["Kaby Lake", "Coffee Lake", "Whiskey Lake", "Comet Lake", "Ice Lake", "Lakefield", "Elkhart Lake"];
            string[] intel11to14 = ["Tiger Lake", "Alder Lake", "Raptor Lake", "DG1"];
            string[] intelArc = ["Arc", "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake"];

            bool Intel_6th = false;
            bool Intel_7th_10th = false;
            bool Intel_11th_14th = false;
            bool Intel_Arc = false; 

            if (intel6th.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_6th = true;
            else if (intel7to10.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_7th_10th = true;
            else if (intel11to14.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_11th_14th = true;
            else if (intelArc.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_Arc = true;

            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // download intel driver
                (@"Downloading INTEL driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.zip"), () => Intel_6th == true),

                 // extract intel driver
                (@"Extracting INTEL driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.zip"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel_6th == true),

                // download intel driver
                (@"Downloading INTEL driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.exe"), null),

                // extract intel driver
                (@"Extracting INTEL driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), null),

                // update/install intel driver
                (gpu.IsInstalled ?  "Updating INTEL driver" : "Installing INTEL driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), null),
                (gpu.IsInstalled ?  "Updating INTEL driver" : "Installing INTEL driver", async () => await Task.Delay(3000), null),
                (gpu.IsInstalled ? "Updating INTEL driver" : "Installing INTEL driver", async () => GpuHelper.RefreshGpu(gpu), null),

                // download intel® graphics command center (beta)
                ("Downloading Intel® Graphics Command Center (Beta)", async () => await ProcessActions.RunMicrosoftStoreDownload("AppUp.IntelGraphicsCommandCenterBeta", "20ad3d91-bf78-4dbd-86cd-e9d215cc9299", "appxBundle", 0, false), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_11th_14th == true),

                // install intel® graphics command center (beta)
                ("Installing Intel® Graphics Command Center (Beta)", async () => await ProcessActions.RunPowerShell(@"Add-AppxPackage -Path (Get-ChildItem -Path \""$env:TEMP\AppUp.IntelGraphicsCommandCenterBeta (Package)\"" | Select-Object -First 1).FullName"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),

                // configure settings
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpApplyAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpBrightness"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpContrast"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpHue"" /t REG_DWORD /d 3221225472 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpSaturation"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SharpnessEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SharpnessFactor"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionAutoDetectEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionEnabledChroma"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionFactor"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableFMD"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableSTE"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SkinTone"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableTCC"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorRed"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorGreen"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorBlue"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorCyan"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorMagenta"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorYellow"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableACE"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""AceLevel"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""InputYUVRangeApplyAlways"" /t REG_DWORD /d 1 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""InputYUVRange"" /t REG_DWORD /d 2 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableIS"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableNLAS"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""UISharpnessOptimalEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v AdaptiveVsyncEnable /t REG_DWORD /d 0 /f"), null),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v AcUserPreferredPolicy /t REG_DWORD /d 0 /f"), null),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DcUserPreferredPolicy /t REG_DWORD /d 0 /f"), null),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerDpstAggressivenessLevel /t REG_DWORD /d 0 /f"), null),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerGpsAggressivenessLevel /t REG_DWORD /d 0 /f"), null),
                ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v Dpst6_3ApplyExtraDimming /t REG_DWORD /d 0 /f"), null),

                // disable unnecessary services
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cphs"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop cphs"), () => Intel_6th == true),
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cplspcon"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop cplspcon"), null),
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igccservice"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop igccservice"), () => Intel_6th == true),
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igfxCUIService2.0.0.0"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop igfxCUIService2.0.0.0"), null),
            
                // disable high-definition multimedia interface (hdmi)/displayport (dp) audio
                ("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => GpuHelper.ToggleHdmiDpAudio(gpu, false), () => gpu.HDMIDPAudio == false)
            };

            return actions;
        }
    }
}
