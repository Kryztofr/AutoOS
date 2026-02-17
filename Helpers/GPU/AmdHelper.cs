using AutoOS.Views.Installer.Actions;
using System.Text.Json;
using System.Management;

namespace AutoOS.Helpers.GPU
{
    public static class AmdHelper
    {
        private static readonly HttpClient httpClient = new();

        public static async Task<(string newestVersion, string newestDownloadUrl)> CheckUpdate(GpuModel gpu)
        {
            string deviceId = gpu.DeviceId;
            string newestVersion = null;
            string newestDownloadUrl = null;

            var endpoints = new[]
            {
                "https://www2.ati.com/drivers/installer/json/DrvDldDetails_Consumer_Legacy_Win10.json",
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
                var builds = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                if (builds == null) continue;

                foreach (var build in builds)
                {
                    if (!build.TryGetValue("skus", out var skusElem) || skusElem.ValueKind != JsonValueKind.Array) continue;

                    foreach (var sku in skusElem.EnumerateArray())
                    {
                        if (sku.GetString()?.Contains(deviceId, StringComparison.InvariantCultureIgnoreCase) != true)
                            continue;

                        newestVersion = build.TryGetValue("externalbuildversion", out var ver) ? ver.GetString() : null;
                        newestDownloadUrl = build.TryGetValue("fullbuild", out var url) ? url.GetString() : null;
                        break;
                    }

                    if (newestVersion != null) break;
                }

                if (newestVersion != null) break;
            }

            return (newestVersion, newestDownloadUrl);
        }

        public static List<(string Title, Func<Task> Action, Func<bool> Condition)> DriverActions(GpuModel gpu, string newestDownloadUrl, ProgressButton progressButton = null)
        {
            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // download amd driver
                ($@"Downloading AMD driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.exe", progressButton), null),

                // extract amd driver
                (@"Extracting AMD driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), null),

                // strip amd driver
                (@"Stripping AMD driver", async () => await ProcessActions.RunApplication("RadeonSoftwareSlimmer", "RadeonSoftwareSlimmer.exe", $@"--extracted-installer ""{Path.Combine(Path.GetTempPath(), "driver")}"" --config ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RadeonSoftwareSlimmer", "config.ini")}"""), null),

                // install amd driver
                (gpu.IsInstalled ? "Updating AMD driver" : "Installing AMD driver", async () => await ProcessActions.RunNsudo("CurrentUser", $@"""%TEMP%\driver\Setup.exe"" -install"), null),
                (gpu.IsInstalled ? "Updating AMD driver" : "Installing AMD driver", async () => await Task.Delay(3000), null),
                (gpu.IsInstalled ? "Updating AMD driver" : "Installing AMD driver", async () => await GpuHelper.RefreshGpu(gpu), null),

                // accept eula
                ("Accepting EULA", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\DisplayOverride"" /v ""EulaAccepted"" /t REG_SZ /d true /f"), null),

                // disable issue detection
                ("Disabling issue detection", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\AIM"" /v ""LaunchBugTool"" /t REG_DWORD /d 0 /f"), null),

                // settings -> hotkeys
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\R3DBk"" /v ChillHk /t REG_DWORD /d 4730 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ChillHk /t REG_DWORD /d 4730 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\R3DBk"" /v DelagHk /t REG_DWORD /d 4684 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v DelagHk /t REG_DWORD /d 4684 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\R3DBk"" /v BoostHk /t REG_DWORD /d 4683 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v BoostHk /t REG_DWORD /d 4683 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\R3DBk"" /v DelagBoostIndicatorHk /t REG_DWORD /d 1053260 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v DelagBoostIndicatorHk /t REG_DWORD /d 1053260 /f"), null),
                (@"Disabling ""Use Hotkeys""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\DVR"" /v HotkeysDisabled /t REG_DWORD /d 1 /f"), null),

                // settings -> general
                (@"Disabling ""In-Game Overlay""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\DVR"" /v ""ShowRSOverlay"" /t REG_SZ /d false /f"), null),
                (@"Disabling ""In-Game Overlay""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\Performance"" /v ""MetricsOverlayState"" /t REG_DWORD /d 0 /f"), null),
                (@"Disabling ""Web Browser""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""AllowWebContent"" /t REG_SZ /d false /f"), null),
                (@"Disabling ""Web Browser""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""RSXBrowserUnavailable"" /t REG_SZ /d true /f"), null), // older versions not sure
                (@"Disabling ""System Tray Menu""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""SystemTray"" /t REG_SZ /d false /f"), null),
                (@"Disabling ""Tutorials""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""CN_Hide_Tutorials"" /t REG_SZ /d true /f"), null),
                (@"Disabling ""Advertisements""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""CN_Hide_FeatureData"" /t REG_SZ /d true /f"), null),
                (@"Disabling ""Toast Notifications""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""CN_Hide_Toast_Notification"" /t REG_SZ /d true /f"), null),
                (@"Disabling ""Animations & effects""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""AnimationEffect"" /t REG_SZ /d false /f"), null),

                // set theme to system
                (@"Setting ""Theme"" to ""System""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""RSXColorScheme"" /t REG_DWORD /d 0 /f"), null),

                // disable "radeon™ super resolution"
                (@"Disabling ""Radeon™ Super Resolution""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_RadeonUpscalingEnabled /t REG_DWORD /d 0 /f"), null),

                // disable "amd fluid motion frames 2.1"
                (@"Disabling ""AMD Fluid Motion Frames 2.1""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DrvFrameGenEnabled /t REG_BINARY /d 00000000 /f"), null),

                // disable "radeon™ anti lag"
                (@"Disabling ""Radeon™ Anti Lag""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_DeLagEnabled /t REG_DWORD /d 0 /f"), null),

                // disable "radeon™ boost"
                (@"Disabling ""Radeon™ Boost""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_RadeonBoostEnabled /t REG_DWORD /d 0 /f"), null),

                // disable "radeon™ chill"
                (@"Disabling ""Radeon™ Chill""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_ChillEnabled /t REG_DWORD /d 0 /f"), null),

                // disable "radeon™ image sharpening"
                (@"Disabling ""Radeon™ Image Sharpening""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_USUEnable /t REG_DWORD /d 0 /f"), null),

                // disable "radeon™ enhanced sync"
                (@"Disabling ""Radeon™ Enhanced Sync""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v TurboSync /t REG_BINARY /d 3000 /f"), null),

                // set "wait for vertical refresh" to "off, unless application specifies"
                (@"Setting ""Wait for Vertical Refresh"" to Off, unless application specifies", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v VSyncControl /t REG_BINARY /d 3100 /f"), null),

                // disable "frame rate target control"
                (@"Disabling ""Frame rate target control""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_FRTEnabled /t REG_DWORD /d 0 /f"), null),

                // set "anti-aliasing" to "use application settings"
                (@"Setting ""Anti-Aliasing"" to Use application settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v EQAA /t REG_BINARY /d 3000 /f"), null),

                // set "anti-aliasing method" to "multisampling"
                (@"Setting ""Anti-Aliasing Method"" to Multisampling", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v ASTT /t REG_BINARY /d 3000 /f"), null),

                // disable "morphological anti-aliasing"
                (@"Disabling ""Morphological Anti-Aliasing""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v MLF /t REG_BINARY /d 3000 /f"), null),

                // set "texture filtering quality" to "performance"
                (@"Setting ""Texture Filtering Quality"" to Performance", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v TFQ /t REG_BINARY /d 3200 /f"), null),

                // enable "surface format optimization"
                (@"Enabling ""Surface Format Optimization""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v SurfaceFormatReplacements /t REG_BINARY /d 3100 /f"), null),

                // set "tessellation mode" to "override application setting"
                (@"Setting ""Tessellation Mode"" to Override application setting", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v Tessellation_OPTION /t REG_BINARY /d 3200 /f"), null),

                // set "maximum tessellation level" to "off"
                (@"Setting ""Maximum Tessellation Level"" to Off", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v Tessellation /t REG_BINARY /d 3100 /f"), null),

                // disable "opengl triple buffering"
                (@"Disabling ""OpenGL Triple Buffering""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v EnableTripleBuffering /t REG_BINARY /d 3000 /f"), null),

                // disable "10-bit pixel format"
                (@"Disabling ""10-Bit Pixel Format""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}\UMD"" /v VisualEnhancements_Capabilities /t REG_BINARY /d 00000000 /f"), null),
                (@"Disabling ""10-Bit Pixel Format""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_10BitMode /t REG_DWORD /d 2 /f"), null),

                // Credit: imribiy
                // https://github.com/imribiy/amd-gpu-tweaks
                // configuring miscellaneous amd settings
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v StutterMode /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_EnableAmdFendrOptions /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_FramePacingSupport /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DalDisableStutter /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableBlockWrite /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableFBCSupport /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableFBCForFullScreenApp /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PP_Force3DPerformanceMode /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PP_ForceHighDPMLevel /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PP_SclkDeepSleepDisable /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PP_GfxOffControl /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PP_ThermalAutoThrottlingEnable /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PP_EnableRaceToIdle /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableUlps /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableUlps_NA /t REG_SZ /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PP_DisableULPS /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_EnableULPS /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v KMD_ForceD3ColdSupport /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableAspmL0s /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableAspmL1 /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableAspmL1SS /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableAspmL0s /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableAspmL1 /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGfxClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableVceClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableSamuClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableRomMGCGClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGfxCoarseGrainClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGfxMediumGrainClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGfxFineGrainClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableHdpMGClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableVceSwClockGating /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableUvdClockGating /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableGfxClockGatingThruSmu /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableSysClockGatingThruSmu /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableXdmaSclkGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DalFineGrainClockGating /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableRomMediumGrainClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableNbioMediumGrainClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableMcMediumGrainClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v IRQMgrDisableIHClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGfxMGLS /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableHdpClockPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableUVDPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableVCEPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableAcpPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableDrmdmaPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGfxCGPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableStaticGfxMGPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableDynamicGfxMGPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableCpPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGDSPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableXdmaPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableGFXPipelinePowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisableQuickGfxMGPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DisablePowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v SMU_DisableMmhubPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v SMU_DisableAthubPowerGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DalForceMaxDisplayClock /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DalDisableClockGating /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DalDisableDeepSleep /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v DalDisableDiv2 /t REG_DWORD /d 1 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableSpreadSpectrum /t REG_DWORD /d 0 /f"), null),
                (@"Configuring Miscellaneous AMD Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableVcePllSpreadSpectrum /t REG_DWORD /d 0 /f"), null),

                // disable unnecessary services
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AMD Crash Defender Service"" /v Start /t REG_DWORD /d 4 /f & sc stop ""AMD Crash Defender Service"""), null),
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdfendr"" /v Start /t REG_DWORD /d 4 /f & sc stop ""amdfendr"""), null),
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdfendrmgr"" /v Start /t REG_DWORD /d 4 /f & sc stop ""amdfendrmgr"""), null),
                ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdlog"" /v Start /t REG_DWORD /d 4 /f & sc stop ""amdlog"""), null),

                // disable high-definition multimedia interface (hdmi)/displayport (dp) audio
                ("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => await Task.Run(() => new ManagementObjectSearcher("SELECT DeviceID, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Name LIKE '%High Definition Audio Controller%'").Get().OfType<ManagementObject>().Where(obj => obj["DeviceID"]?.ToString().Contains(gpu.PnPDeviceId[(gpu.PnPDeviceId.LastIndexOf('\\') + 1)..gpu.PnPDeviceId.LastIndexOf('&')], StringComparison.OrdinalIgnoreCase) == true).ToList().ForEach(obj => obj.InvokeMethod("Disable", null, null))), () => gpu.HDMIDPAudio == false)
            };

            return actions;
        }
    }
}