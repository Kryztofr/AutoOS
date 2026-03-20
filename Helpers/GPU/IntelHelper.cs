using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Registry;
using AutoOS.Helpers.Services;
using PuppeteerSharp;
using System.Text.RegularExpressions;
using AutoOS.Helpers.Store;
using System.Diagnostics;
using Microsoft.Win32;

namespace AutoOS.Helpers.GPU
{
    public static partial class IntelHelper
    {
        [GeneratedRegex(@"(\d+\.\d+\.\d+\.\d+)\s*\(Latest\)", RegexOptions.IgnoreCase)]
        private static partial Regex VersionRegex();

        [GeneratedRegex(@"downloadmirror\.intel\.com\/(\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex IntelIdRegex();

        [GeneratedRegex(@"(gfx_win_[0-9.]+\.zip)", RegexOptions.IgnoreCase)]
        private static partial Regex ZipFileRegex();

        [GeneratedRegex(@"(gfx_win_[0-9.]+\.exe)", RegexOptions.IgnoreCase)]
        private static partial Regex ExeFileRegex();

        public static async Task<(string newestVersion, string newestDownloadUrl)> CheckUpdate(GpuInfo gpu)
        {
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

            string bodyText = await page.EvaluateExpressionAsync<string>("document.body.innerText");
            string domHtml = await page.EvaluateExpressionAsync<string>("document.documentElement.outerHTML");

            var versionMatch = VersionRegex().Match(bodyText);
            if (versionMatch.Success) newestVersion = versionMatch.Groups[1].Value;

            var fileMatch = is6thGen ? ZipFileRegex().Match(domHtml) : ExeFileRegex().Match(domHtml);
            string fileName = fileMatch.Success ? fileMatch.Groups[1].Value : string.Empty;

            var idMatch = IntelIdRegex().Match(domHtml);
            string fileId = idMatch.Success ? idMatch.Groups[1].Value : string.Empty;

            if (!string.IsNullOrEmpty(fileId) && !string.IsNullOrEmpty(fileName))
                newestDownloadUrl = $"https://downloadmirror.intel.com/{fileId}/{fileName}";

            return (newestVersion, newestDownloadUrl);
        }

        public static List<(string Title, Func<Task> Action, Func<bool> Condition)> InstallActions(GpuInfo gpu, string newestDownloadUrl, ProgressButton progressButton = null)
        {
            string codename = gpu.Codename;
            static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

            string[] intel6th = ["Skylake", "Apollo Lake"];

            bool Intel_6th = false;

            if (intel6th.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_6th = true;

            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // download intel driver
                (@"Downloading INTEL driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.zip"), () => Intel_6th == true),

                 // extract intel driver
                (@"Extracting INTEL driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.zip"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel_6th == true),

                // download intel driver
                (@"Downloading INTEL driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.exe"), () => Intel_6th == false),

                // extract intel driver
                (@"Extracting INTEL driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel_6th == false),

                // update/install intel driver
                (gpu.IsInstalled ?  "Updating INTEL driver" : "Installing INTEL driver", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(Path.GetTempPath(), "driver", "Installer.exe"), Arguments = "/silent", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
                (gpu.IsInstalled ?  "Updating INTEL driver" : "Installing INTEL driver", async () => await Task.Delay(3000), null),
                (gpu.IsInstalled ? "Updating INTEL driver" : "Installing INTEL driver", async () => GpuHelper.RefreshGpu(gpu), null)
            };

            return actions;
        }

        public static List<(string Title, Func<Task> Action, Func<bool> Condition)> TweakActions(GpuInfo gpu)
        {
            string codename = gpu.Codename;
            static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

            string[] intel6th = ["Skylake", "Apollo Lake"];
            string[] intel7to10 = ["Kaby Lake", "Coffee Lake", "Whiskey Lake", "Comet Lake", "Ice Lake", "Lakefield", "Elkhart Lake"];
            string[] intel11to14 = ["Tiger Lake", "Alder Lake", "Raptor Lake", "DG1"];
            //string[] intelArc = ["Arc", "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake"];

            bool Intel_6th = false;
            bool Intel_7th_10th = false;
            bool Intel_11th_14th = false;
            //bool Intel_Arc = false;

            if (intel6th.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_6th = true;
            else if (intel7to10.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_7th_10th = true;
            else if (intel11to14.Any(c => Normalize(codename).Contains(Normalize(c))))
                Intel_11th_14th = true;
            //else if (intelArc.Any(c => Normalize(codename).Contains(Normalize(c))))
                //Intel_Arc = true;

            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // download intel® graphics command center (beta)
                ("Downloading Intel® Graphics Command Center (Beta)", async () => await StoreHelper.Download("AppUp.IntelGraphicsCommandCenterBeta_8wekyb3d8bbwe"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_11th_14th == true),

                // install intel® graphics command center (beta)
                ("Installing Intel® Graphics Command Center (Beta)", async () => await StoreHelper.Install("AppUp.IntelGraphicsCommandCenterBeta_8wekyb3d8bbwe"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),

                // configure settings
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "ProcAmpApplyAlways", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "ProcAmpBrightness", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "ProcAmpContrast", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "ProcAmpHue", 3221225472U, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "ProcAmpSaturation", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SharpnessEnabledAlways", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SharpnessFactor", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "NoiseReductionEnabledAlways", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "NoiseReductionAutoDetectEnabledAlways", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "NoiseReductionEnabledChroma", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "NoiseReductionFactor", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "EnableFMD", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "EnableSTE", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SkinTone", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "EnableTCC", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SatFactorRed", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SatFactorGreen", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SatFactorBlue", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SatFactorCyan", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SatFactorMagenta", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "SatFactorYellow", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "EnableACE", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "AceLevel", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "InputYUVRangeApplyAlways", 1, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "InputYUVRange", 2, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "EnableIS", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "EnableNLAS", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys", "UISharpnessOptimalEnabledAlways", 0, RegistryValueKind.DWord), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "AdaptiveVsyncEnable", 0, RegistryValueKind.DWord), null),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "AcUserPreferredPolicy", 0, RegistryValueKind.DWord), null),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DcUserPreferredPolicy", 0, RegistryValueKind.DWord), null),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PowerDpstAggressivenessLevel", 0, RegistryValueKind.DWord), null),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PowerGpsAggressivenessLevel", 0, RegistryValueKind.DWord), null),
                ("Configuring settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "Dpst6_3ApplyExtraDimming", 0, RegistryValueKind.DWord), null),

                // disable unnecessary services
                ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cphs", "Start", 4, RegistryValueKind.DWord), () => Intel_6th == true),
                ("Disabling unnecessary services", async () => ServicesHelper.StopService("cphs"), () => Intel_6th == true),
                ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cplspcon", "Start", 4, RegistryValueKind.DWord), null),
                ("Disabling unnecessary services", async () => ServicesHelper.StopService("cplspcon"), null),
                ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igccservice", "Start", 4, RegistryValueKind.DWord), () => Intel_6th == true),
                ("Disabling unnecessary services", async () => ServicesHelper.StopService("igccservice"), () => Intel_6th == true),
                ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igfxCUIService2.0.0.0", "Start", 4, RegistryValueKind.DWord), null),
                ("Disabling unnecessary services", async () => ServicesHelper.StopService("igfxCUIService2.0.0.0"), null),
            
                // disable high-definition multimedia interface (hdmi)/displayport (dp) audio
                ("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => GpuHelper.ToggleHdmiDpAudio(gpu, false), () => gpu.HDMIDPAudio == false)
            };

            return actions;
        }
    }
}
