using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Registry;
using AutoOS.Helpers.Services;
using System.Text.Json.Nodes;
using System.Diagnostics;
using Microsoft.Win32;
using Windows.Storage;

namespace AutoOS.Helpers.GPU;

public static class AmdHelper
{
    private static readonly HttpClient httpClient = new();

    public static async Task<(string newestVersion, string newestDownloadUrl)> CheckUpdate(GpuInfo gpu)
    {
        string deviceId = gpu.DeviceId;
        string newestVersion = null;
        string newestDownloadUrl = null;

        var endpoints = new[]
        {
            "https://www2.ati.com/drivers/installer/json/DrvDldDetails_Consumer_Legacy_Win10.json",
            "https://www2.ati.com/drivers/installer/json/DrvDldDetails_Consumer_WHQL_Win11.json",
            "https://www2.ati.com/drivers/installer/json/DrvDldDetails_Consumer_WHQL_Win11_p.json",
            "https://www2.ati.com/drivers/installer/json/drvdlddetails_ws_maintenance_win11.json",
            "https://www2.ati.com/drivers/installer/json/DrvDldDetails_WS_Legacy_Win10.json"
        };

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "http://support.amd.com");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AMD Catalyst Install Manager/0.0");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "Keep-Alive");

        var jsonTasks = endpoints.Select(endpoint => httpClient.GetStringAsync(endpoint));
        var responses = await Task.WhenAll(jsonTasks);

        foreach (var json in responses)
        {
            var builds = JsonNode.Parse(json).AsArray();
            if (builds == null) continue;

            foreach (var buildNode in builds)
            {
                var build = buildNode.AsObject();
                if (build == null) continue;

                if (!build.TryGetPropertyValue("skus", out var skusNode) || skusNode is not JsonArray skusArray)
                    continue;

                foreach (var sku in skusArray)
                {
                    if (sku?.ToString().Contains(deviceId, StringComparison.InvariantCultureIgnoreCase) != true)
                        continue;

                    newestVersion = build["externalbuildversion"]?.ToString();
                    newestDownloadUrl = build["fullbuild"]?.ToString();
                    break;
                }

                if (newestVersion != null) break;
            }

            if (newestVersion != null) break;
        }

        return (newestVersion, newestDownloadUrl);
    }

    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> InstallActions(GpuInfo gpu, string newestDownloadUrl, ProgressButton progressButton = null)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download amd driver
            ($@"Downloading AMD driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "AMD"), "driver.exe", progressButton), null),

            // extract amd driver
            (@"Extracting AMD driver", async () => await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "AMD", "driver.exe"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "AMD", "driver")), null),

            // strip amd driver
            (@"Stripping AMD driver", async () => await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RadeonSoftwareSlimmer", "RadeonSoftwareSlimmer.exe"), $@"--extracted-installer ""{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "AMD", "driver")}"" --config ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RadeonSoftwareSlimmer", "config.ini")}""") { CreateNoWindow = true })!.WaitForExitAsync(), null),

            // install amd driver
            (gpu.IsInstalled ? "Updating AMD driver" : "Installing AMD driver", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "AMD", "driver", "Setup.exe"), Arguments = "-install", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
            (gpu.IsInstalled ? "Updating AMD driver" : "Installing AMD driver", async () => await Task.Delay(3000), null),
            (gpu.IsInstalled ? "Updating AMD driver" : "Installing AMD driver", async () => GpuHelper.RefreshGpu(gpu), null),
            ("Cleaning up AMD files", async () => await (await ApplicationData.Current.TemporaryFolder.GetFolderAsync("AMD")).DeleteAsync(), null)
        };

        return actions;
    }

    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> TweakActions(GpuInfo gpu)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // accept eula
            ("Accepting EULA", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN\DisplayOverride", "EulaAccepted", "true", RegistryValueKind.String), null),

            // settings -> system
            (@"Disabling ""Issue detection""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\AIM", "LaunchBugTool", 0, RegistryValueKind.DWord), null),

            // settings -> hotkeys
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN\R3DBk", "ChillHk", 4730, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "ChillHk", 4730, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN\R3DBk", "DelagHk", 4684, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "DelagHk", 4684, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN\R3DBk", "BoostHk", 4683, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "BoostHk", 4683, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN\R3DBk", "DelagBoostIndicatorHk", 1053260, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "DelagBoostIndicatorHk", 1053260, RegistryValueKind.DWord), null),
            (@"Disabling ""Use Hotkeys""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\DVR", "HotkeysDisabled", 1, RegistryValueKind.DWord), null),

            // settings -> general
            (@"Disabling ""In-Game Overlay""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\DVR", "ShowRSOverlay", "false", RegistryValueKind.String), null),
            (@"Disabling ""In-Game Overlay""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN\Performance", "MetricsOverlayState", 0, RegistryValueKind.DWord), null),
            (@"Disabling ""Web Browser""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "AllowWebContent", "false", RegistryValueKind.String), null),
            (@"Disabling ""Web Browser""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "RSXBrowserUnavailable", "true", RegistryValueKind.String), null), // older versions not sure
            (@"Disabling ""System Tray Menu""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "SystemTray", "false", RegistryValueKind.String), null),
            (@"Disabling ""Tutorials""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "CN_Hide_Tutorials", "true", RegistryValueKind.String), null),
            (@"Disabling ""Advertisements""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "CN_Hide_FeatureData", "true", RegistryValueKind.String), null),
            (@"Disabling ""Toast Notifications""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "CN_Hide_Toast_Notification", "true", RegistryValueKind.String), null),
            (@"Disabling ""Animations & effects""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "AnimationEffect", "false", RegistryValueKind.String), null),

            // set theme to system
            (@"Setting ""Theme"" to ""System""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\AMD\CN", "RSXColorScheme", 0, RegistryValueKind.DWord), null),

            // disable "radeon™ super resolution"
            (@"Disabling ""Radeon™ Super Resolution""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_RadeonUpscalingEnabled", 0, RegistryValueKind.DWord), null),

            // disable "amd fluid motion frames 2.1"
            (@"Disabling ""AMD Fluid Motion Frames 2.1""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DrvFrameGenEnabled", new byte[] { 0, 0, 0, 0 }, RegistryValueKind.Binary), null),

            // disable "radeon™ anti lag"
            (@"Disabling ""Radeon™ Anti Lag""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_DeLagEnabled", 0, RegistryValueKind.DWord), null),

            // disable "radeon™ boost"
            (@"Disabling ""Radeon™ Boost""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_RadeonBoostEnabled", 0, RegistryValueKind.DWord), null),

            // disable "radeon™ chill"
            (@"Disabling ""Radeon™ Chill""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_ChillEnabled", 0, RegistryValueKind.DWord), null),

            // disable "radeon™ image sharpening"
            (@"Disabling ""Radeon™ Image Sharpening""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_USUEnable", 0, RegistryValueKind.DWord), null),

            // disable "radeon™ enhanced sync"
            (@"Disabling ""Radeon™ Enhanced Sync""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "TurboSync", new byte[] { 0x30, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // set "wait for vertical refresh" to "off, unless application specifies"
            (@"Setting ""Wait for Vertical Refresh"" to Off, unless application specifies", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "VSyncControl", new byte[] { 0x31, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // disable "frame rate target control"
            (@"Disabling ""Frame rate target control""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_FRTEnabled", 0, RegistryValueKind.DWord), null),

            // set "anti-aliasing" to "use application settings"
            (@"Setting ""Anti-Aliasing"" to Use application settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "EQAA", new byte[] { 0x30, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // set "anti-aliasing method" to "multisampling"
            (@"Setting ""Anti-Aliasing Method"" to Multisampling", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "ASTT", new byte[] { 0x30, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // disable "morphological anti-aliasing"
            (@"Disabling ""Morphological Anti-Aliasing""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "MLF", new byte[] { 0x30, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // set "texture filtering quality" to "performance"
            (@"Setting ""Texture Filtering Quality"" to Performance", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "TFQ", new byte[] { 0x32, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // enable "surface format optimization"
            (@"Enabling ""Surface Format Optimization""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "SurfaceFormatReplacements", new byte[] { 0x31, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // set "tessellation mode" to "override application setting"
            (@"Setting ""Tessellation Mode"" to Override application setting", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "Tessellation_OPTION", new byte[] { 0x32, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // set "maximum tessellation level" to "off"
            (@"Setting ""Maximum Tessellation Level"" to Off", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "Tessellation", new byte[] { 0x31, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // disable "opengl triple buffering"
            (@"Disabling ""OpenGL Triple Buffering""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "EnableTripleBuffering", new byte[] { 0x30, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary), null),

            // disable "10-bit pixel format"
            (@"Disabling ""10-Bit Pixel Format""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"{gpu.RegistryPath}\UMD", "VisualEnhancements_Capabilities", new byte[] { 0, 0, 0, 0 }, RegistryValueKind.Binary), null),
            (@"Disabling ""10-Bit Pixel Format""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_10BitMode", 2, RegistryValueKind.DWord), null),

            // Credit: imribiy
            // https://github.com/imribiy/amd-gpu-tweaks
            // configuring miscellaneous amd settings
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "StutterMode", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_EnableAmdFendrOptions", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_FramePacingSupport", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DalDisableStutter", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableBlockWrite", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableFBCSupport", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableFBCForFullScreenApp", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PP_Force3DPerformanceMode", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PP_ForceHighDPMLevel", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PP_SclkDeepSleepDisable", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PP_GfxOffControl", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PP_ThermalAutoThrottlingEnable", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PP_EnableRaceToIdle", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableUlps", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableUlps_NA", "0", RegistryValueKind.String), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "PP_DisableULPS", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_EnableULPS", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "KMD_ForceD3ColdSupport", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableAspmL0s", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableAspmL1", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableAspmL1SS", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableAspmL0s", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableAspmL1", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGfxClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableVceClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableSamuClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableRomMGCGClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGfxCoarseGrainClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGfxMediumGrainClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGfxFineGrainClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableHdpMGClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableVceSwClockGating", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableUvdClockGating", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableGfxClockGatingThruSmu", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableSysClockGatingThruSmu", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableXdmaSclkGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DalFineGrainClockGating", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableRomMediumGrainClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableNbioMediumGrainClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableMcMediumGrainClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "IRQMgrDisableIHClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGfxMGLS", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableHdpClockPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableUVDPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableVCEPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableAcpPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableDrmdmaPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGfxCGPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableStaticGfxMGPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableDynamicGfxMGPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableCpPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGDSPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableXdmaPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableGFXPipelinePowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisableQuickGfxMGPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DisablePowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "SMU_DisableMmhubPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "SMU_DisableAthubPowerGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DalForceMaxDisplayClock", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DalDisableClockGating", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DalDisableDeepSleep", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "DalDisableDiv2", 1, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableSpreadSpectrum", 0, RegistryValueKind.DWord), null),
            (@"Configuring Miscellaneous AMD Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableVcePllSpreadSpectrum", 0, RegistryValueKind.DWord), null),

            // disable unnecessary services
            ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AMD Crash Defender Service", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling unnecessary services", async () => ServicesHelper.StopService("AMD Crash Defender Service"), null),
            ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdfendr", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling unnecessary services", async () => ServicesHelper.StopService("amdfendr"), null),
            ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdfendrmgr", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling unnecessary services", async () => ServicesHelper.StopService("amdfendrmgr"), null),
            ("Disabling unnecessary services", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdlog", "Start", 4, RegistryValueKind.DWord), null),
            ("Disabling unnecessary services", async () => ServicesHelper.StopService("amdlog"), null),

            // disable high-definition multimedia interface (hdmi)/displayport (dp) audio
            ("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => GpuHelper.ToggleHdmiDpAudio(gpu, false), () => gpu.HDMIDPAudio == false)
        };

        return actions;
    }
}