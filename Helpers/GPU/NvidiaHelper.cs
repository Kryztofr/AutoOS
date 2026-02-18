using System.Management;
using System.Text.Json;
using AutoOS.Views.Installer.Actions;
using System.Diagnostics;

namespace AutoOS.Helpers.GPU
{
    public static class NvidiaHelper
    {
        private static readonly HttpClient httpClient = new();
        public static async Task<(string newestVersion, string newestDownloadUrl)> CheckUpdate(GpuInfo gpu)
        {
            string deviceName = gpu.DeviceName;
            bool isNotebook = false;
            string gpuId = string.Empty;
            string newestVersion = string.Empty;
            string newestDownloadUrl = string.Empty;

            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure").Get().Cast<ManagementObject>())
            {
                ushort[] chassisTypes = (ushort[])obj["ChassisTypes"];
                if (chassisTypes != null && chassisTypes.Any(type => new ushort[] { 1, 8, 9, 10, 11, 12, 14, 18, 21, 31, 32 }.Contains(type)))
                {
                    isNotebook = true;
                    break;
                }
            }

            string json = await httpClient.GetStringAsync("https://raw.githubusercontent.com/ZenitH-AT/nvidia-data/main/gpu-data.json");
            using var gpuDoc = JsonDocument.Parse(json);

            if (gpuDoc.RootElement.TryGetProperty(isNotebook ? "notebook" : "desktop", out JsonElement section))
            {
                string deviceNameLower = deviceName.ToLower();
                string bestMatchKey = null;
                double bestScore = -1;

                foreach (var prop in section.EnumerateObject())
                {
                    string keyLower = prop.Name.ToLower();
                    var deviceWords = new HashSet<string>(deviceNameLower.Split(' ', '/', '-', '+'));
                    var keyWords = new HashSet<string>(keyLower.Split(' ', '/', '-', '+'));

                    int common = deviceWords.Intersect(keyWords).Count();
                    double score = (double)common / keyWords.Count;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatchKey = prop.Name;
                    }
                }

                if (bestMatchKey != null)
                {
                    gpuId = section.GetProperty(bestMatchKey).GetString();
                }
            }

            if (!string.IsNullOrEmpty(gpuId))
            {
                string response = await httpClient.GetStringAsync($"https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&pfid={gpuId}&osID=135&dch=1&upCRD=0");

                using var respDoc = JsonDocument.Parse(response);
                var root = respDoc.RootElement;

                if (int.TryParse(root.GetProperty("Success").GetString(), out int success) && success == 1)
                {
                    var info = root.GetProperty("IDS")[0].GetProperty("downloadInfo");
                    newestVersion = info.GetProperty("Version").GetString();
                    newestDownloadUrl = info.GetProperty("DownloadURL").GetString();
                }
            }

            return (newestVersion, newestDownloadUrl);
        }

        public static async Task StripDriver()
        {
            foreach (var directory in Directory.GetDirectories(Path.Combine(Path.GetTempPath(), "driver")))
            {
                string folderName = Path.GetFileName(directory);

                if (folderName != "Display.Driver" && folderName != "NVI2")
                {
                    Directory.Delete(directory, true);
                }
            }

            string setupCfgPath = Path.Combine(Path.Combine(Path.GetTempPath(), "driver"), "setup.cfg");

            if (File.Exists(setupCfgPath))
            {
                var lines = await File.ReadAllLinesAsync(setupCfgPath);
                var newLines = lines.Where(line => !line.Contains("<file name=\"${{EulaHtmlFile}}\"/>") && !line.Contains("<file name=\"${{FunctionalConsentFile}}\"/>") && !line.Contains("<file name=\"${{PrivacyPolicyFile}}\"/>")).ToList();

                await File.WriteAllLinesAsync(setupCfgPath, newLines);
            }

            string presentationsCfgPath = Path.Combine(Path.Combine(Path.GetTempPath(), "driver"), "NVI2", "presentations.cfg");

            if (File.Exists(presentationsCfgPath))
            {
                var lines = await File.ReadAllLinesAsync(presentationsCfgPath);
                var newLines = lines.Select(line =>
                {
                    if (line.Contains("<string name=\"ProgressPresentationUrl\""))
                    {
                        return "        <string name=\"ProgressPresentationUrl\" value=\"\"/>";
                    }
                    else if (line.Contains("<string name=\"ProgressPresentationSelectedPackageUrl\""))
                    {
                        return "        <string name=\"ProgressPresentationSelectedPackageUrl\" value=\"\"/>";
                    }
                    return line;
                }).ToList();

                await File.WriteAllLinesAsync(presentationsCfgPath, newLines);
            }
        }

        public static List<(string Title, Func<Task> Action, Func<bool> Condition)> DriverActions(GpuInfo gpu, string newestDownloadUrl, ProgressButton progressButton = null)
        {
            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // download nvidia driver
                (@"Downloading NVIDIA driver", async () => await ProcessActions.RunDownload(newestDownloadUrl, Path.GetTempPath(), "driver.exe", progressButton), null),

                // extract nvidia driver
                (@"Extracting NVIDIA driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), null),

                // strip nvidia driver
                (@"Stripping NVIDIA driver", async () => await NvidiaHelper.StripDriver(), null),

                // update/install nvidia driver
                (gpu.IsInstalled ? "Updating NVIDIA driver" : "Installing NVIDIA driver", async () => await ProcessActions.RunNsudo("CurrentUser", $@"""%TEMP%\driver\setup.exe"" /s{(gpu.IsInstalled ? " /clean" : "")}"), null),
                (gpu.IsInstalled ? "Updating NVIDIA driver" : "Installing NVIDIA driver", async () => await Task.Delay(3000), null),
                (gpu.IsInstalled ? "Updating NVIDIA driver" : "Installing NVIDIA driver", async () => GpuHelper.RefreshGpu(gpu), null),

                // disable nvidia tray icon
                (@"Disabling ""Show Notification Tray Icon""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\NvTray"" /v StartOnLogin /t REG_DWORD /d 0 /f"), null),
                (@"Disabling ""Show Notification Tray Icon""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""HideXGpuTrayIcon"" /t REG_DWORD /d 1 /f"), null),
                (@"Disabling ""Show Notification Tray Icon""", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager"" /v ""ShowTrayIcon"" /t REG_DWORD /d 0 /f"), null),

                // select "use the advanced 3d image settings"
                (@"Selecting ""Use the advanced 3D image settings""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\NVIDIA Corporation\Global\NVTweak"" /v ""Gestalt"" /t REG_DWORD /d 515 /f"), null),

                // import optimized nvidia profile
                ("Importing optimized NVIDIA profile", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", "nvidiaProfileInspector.exe"), Arguments = $"-silentimport \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NvidiaProfileInspector", "BaseProfile.nip")}\"", CreateNoWindow = true })!.WaitForExitAsync(), null),

                // configure physx to use gpu
                ("Configuring PhysX to use GPU", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""NvCplPhysxAuto"" /t REG_DWORD /d 0 /f"), null),

                // use nvidia settings for edge enhancements
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_Edge_Enhance /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_Edge_Enhance /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_Edge_Enhance /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_Edge_Enhance /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Edge enhancements"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_Edge_Enhance /t REG_DWORD /d 2147483649 /f"), null),

                // set edge enhancements to 0
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_VAL_Edge_Enhance /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_VAL_Edge_Enhance /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_VAL_Edge_Enhance /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_VAL_Edge_Enhance /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_VAL_Edge_Enhance /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XALG_Edge_Enhance /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XALG_Edge_Enhance /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XALG_Edge_Enhance /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XALG_Edge_Enhance /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Edge enhancements"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XALG_Edge_Enhance /t REG_BINARY /d 0000000000000000 /f"), null),

                // use nvidia settings for noise reduction
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_Noise_Reduce /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_Noise_Reduce /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_Noise_Reduce /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_Noise_Reduce /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""Noise reduction"" to ""Use the NVIDIA setting""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_Noise_Reduce /t REG_DWORD /d 2147483649 /f"), null),

                // set noise reduction to 0
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_VAL_Noise_Reduce /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_VAL_Noise_Reduce /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_VAL_Noise_Reduce /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_VAL_Noise_Reduce /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_VAL_Noise_Reduce /t REG_DWORD /d 0 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XALG_Noise_Reduce /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XALG_Noise_Reduce /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XALG_Noise_Reduce /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XALG_Noise_Reduce /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Noise reduction"" to ""0%""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XALG_Noise_Reduce /t REG_BINARY /d 0000000000000000 /f"), null),

                // disable use inverse telecine
                (@"Disabling ""Use inverse incline""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XALG_Cadence /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Disabling ""Use inverse incline""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XALG_Cadence /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Disabling ""Use inverse incline""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XALG_Cadence /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Disabling ""Use inverse incline""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XALG_Cadence /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Disabling ""Use inverse incline""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XALG_Cadence /t REG_BINARY /d 0000000000000000 /f"), null),

                // use nvidia settings for video color settings
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_Contrast /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_RGB_Gamma_G /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_RGB_Gamma_R /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_RGB_Gamma_B /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_Hue /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_Saturation /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_Brightness /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XEN_Color_Range /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_Contrast /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_RGB_Gamma_G /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_RGB_Gamma_R /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_RGB_Gamma_B /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_Hue /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_Saturation /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_Brightness /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XEN_Color_Range /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_Contrast /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_RGB_Gamma_G /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_RGB_Gamma_R /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_RGB_Gamma_B /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_Hue /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_Saturation /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_Brightness /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XEN_Color_Range /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_Contrast /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_RGB_Gamma_G /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_RGB_Gamma_R /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_RGB_Gamma_B /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_Hue /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_Saturation /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_Brightness /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XEN_Color_Range /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_Contrast /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_RGB_Gamma_G /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_RGB_Gamma_R /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_RGB_Gamma_B /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_Hue /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_Saturation /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_Brightness /t REG_DWORD /d 2147483649 /f"), null),
                (@"Setting ""How do you make color adjustments?"" to ""With the NVIDIA settings""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XEN_Color_Range /t REG_DWORD /d 2147483649 /f"), null),

                // set "dynamic range" to "full (0-255)"
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP0_XALG_Color_Range /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP1_XALG_Color_Range /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP2_XALG_Color_Range /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP3_XALG_Color_Range /t REG_BINARY /d 0000000000000000 /f"), null),
                (@"Setting ""Dynamic range"" to ""Full (0-255)""", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v _User_SUB0_DFP4_XALG_Color_Range /t REG_BINARY /d 0000000000000000 /f"), null),

                // configure color settings
                ("Configuring color settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""delims="" %a in ('reg query HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase') do reg add ""%a"" /v ""ColorformatConfig"" /t REG_BINARY /d ""DB02000014000000000A00080000000003010000"" /f"), null),

                // disable error code correction (ecc)
                ("Disabling Error Code Correction (ECC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"nvidia-smi.exe -e 0"), null),

                // ignore the ecc fuse
                ("Disabling Error Code Correction (ECC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RMNoECCFuseCheck"" /t REG_DWORD /d 1 /f"), null),

                // disable l1 ecc
                ("Disabling Error Code Correction (ECC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RMEnableL1ECC"" /t REG_DWORD /d 0 /f"), null),

                // disablee sm ecc
                ("Disabling Error Code Correction (ECC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RMEnableSMECC"" /t REG_DWORD /d 0 /f"), null),
                
                // disable shm ecc
                ("Disabling Error Code Correction (ECC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RMEnableSHMECC"" /t REG_DWORD /d 0 /f"), null),
                
                // disable rm assert on ecc interrupts
                ("Disabling Error Code Correction (ECC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RMAssertOnEccErrors"" /t REG_DWORD /d 0 /f"), null),
                
                // disable ecc state in guest
                ("Disabling Error Code Correction (ECC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RMGuestECCState"" /t REG_DWORD /d 0 /f"), null),

                // configure miscellaneous nvidia settings
                ("Configuring miscellaneous NVIDIA settings", async () => await ProcessActions.RunPowerShellScript("nvidiamisc.ps1", ""), null),

                // disable runtime power management
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableRuntimePowerManagement /t REG_DWORD /d 0 /f"), null),

                // disables hw fault buffers on pascal+ chips
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisableHwFaultBuffer /t REG_DWORD /d 1 /f"), null),

                // disable all engine level clock gating settings
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMElcg /t REG_DWORD /d 1431655765 /f"), null),

                // disable all engine level power gating settings
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMElpg /t REG_DWORD /d 4095 /f"), null),

                // disable all block level clock gating settings
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMBlcg /t REG_DWORD /d 286331153 /f"), null),

                // disable all second level clock gating settings
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMSlcg /t REG_DWORD /d 262131 /f"), null),

                // disable all floorsweep power gating settings
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMFspg /t REG_DWORD /d 15 /f"), null),

                // disable gc6
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMGC6Feature /t REG_DWORD /d 699050 /f"), null),

                // disable all latency optimizations for gc6
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMGC6Parameters /t REG_DWORD /d 85 /f"), null),

                // disable all gc5 idle features
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDidleFeatureGC5 /t REG_DWORD /d 44731050 /f"), null),

                // disable hot plug support
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMHotPlugSupportDisable /t REG_DWORD /d 1 /f"), null),

                // enable the paged dma mode for fbsr
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmFbsrPagedDMA /t REG_DWORD /d 1 /f"), null),

                // disable post l2 compression
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisablePostL2Compression /t REG_DWORD /d 1 /f"), null),

                // disable rc watchdog
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmRcWatchdog /t REG_DWORD /d 0 /f"), null),

                // disable event logging on rc errors
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLogonRC /t REG_DWORD /d 0 /f"), null),

                // disable more detailed debug intr logs
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMIntrDetailedLogs /t REG_DWORD /d 0 /f"), null),

                // disable fecs context switch logging
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMCtxswLog /t REG_DWORD /d 0 /f"), null),

                // disable logging
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMNvLog /t REG_DWORD /d 0 /f"), null),

                // disable logging of nvlink fatal errors
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisableInforomNvlink /t REG_DWORD /d 3 /f"), null),

                // set head0 dclk mode
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v Head0DClkMode /t REG_DWORD /d 4294967295 /f"), null),

                // set head1 dclk mode
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v Head1DClkMode /t REG_DWORD /d 4294967295 /f"), null),

                // set head2 dclk mode
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v Head2DClkMode /t REG_DWORD /d 4294967295 /f"), null),

                // set head3 dclk mode
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v Head3DClkMode /t REG_DWORD /d 4294967295 /f"), null),

                // set pclk mode
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PClkMode /t REG_DWORD /d 4294967295 /f"), null),

                // disable feature disablement
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableFeatureDisablement /t REG_DWORD /d 0 /f"), null),

                // disable break
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmBreak /t REG_DWORD /d 0 /f"), null),

                // disable breakpoint on debug resource manager on rc errors
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmBreakonRC /t REG_DWORD /d 0 /f"), null),

                // disable smc on a specific gpu
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDebugSetSMCMode /t REG_DWORD /d 0 /f"), null),

                // disable lrc coalescing
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableLRCCoalescing /t REG_DWORD /d 1 /f"), null),

                // disable i2c nanny
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmEnableI2CNanny /t REG_DWORD /d 0 /f"), null),

                // latency tolerance
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMPcieLtrOverride /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDeepL1EntryLatencyUsec /t REG_DWORD /d 1 /f"), null),

                // configure bandwidth feature
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMBandwidthFeature /t REG_DWORD /d 1896072192 /f"), null),

                // disable mempool compression
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMBandwidthFeature2 /t REG_DWORD /d 1 /f"), null),

                // disable pre os apps
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisablePreosapps /t REG_DWORD /d 1 /f"), null),

                // rmperflimitsoverride
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmPerfLimitsOverride /t REG_DWORD /d 21 /f"), null),

                // rmgcofffeature
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMGCOffFeature /t REG_DWORD /d 2 /f"), null),

                // disable aspm
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmOverrideSupportChipsetAspm /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMEnableASPMDT /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableGpuASPMFlags /t REG_DWORD /d 3 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMEnableASPMAtLoad /t REG_DWORD /d 0 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMEnableASPMPublicBits /t REG_DWORD /d 0 /f"), null),

                // disable event tracer
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMEnableEventTracer /t REG_DWORD /d 0 /f"), null),

                // skip error checks
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v SkipSwStateErrChecks /t REG_DWORD /d 1 /f"), null),

                // disable advanced error reporting
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMAERRForceDisable /t REG_DWORD /d 1 /f"), null),

                // enable opsb feature
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RM580312 /t REG_DWORD /d 1 /f"), null),

                // enable war
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMBug2519005War /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmCeElcgWar1895530 /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmWar1760398 /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RM2644249 /t REG_DWORD /d 1 /f"), null),

                // configure low power features
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMLpwrArch /t REG_DWORD /d 349525 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMLpwrEiClient /t REG_DWORD /d 5 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLpwrCtrlMsDifrCgParameters /t REG_DWORD /d 1365 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLpwrFgRppg /t REG_DWORD /d 0 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLpwrGrPgSwFilterFunction /t REG_DWORD /d 0 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLpwrCtrlMsLtcParameters /t REG_DWORD /d 5 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLpwrCtrlMsDifrSwAsrParameters /t REG_DWORD /d 5461 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLpwrCacheStatsOnD3 /t REG_DWORD /d 0 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmLpwrCtrlGrRgParameters /t REG_DWORD /d 89478485 /f"), null),

                // configure paging features
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmPgCtrlParameters /t REG_DWORD /d 1431655765 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmPgCtrlGrParameters /t REG_DWORD /d 1431655765 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmPgCtrlDiParameters /t REG_DWORD /d 21 /f"), null),

                // keep mscg enabled from rm side
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDwbMscg /t REG_DWORD /d 1 /f"), null),

                // dont use pmu spi
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMUsePmuSpi /t REG_DWORD /d 0 /f"), null),

                // disable bbx inform
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisableInforomBBX /t REG_DWORD /d 15 /f"), null),

                // prefer system memory contiguous
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PreferSystemMemoryContiguous /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm"" /v PreferSystemMemoryContiguous /t REG_DWORD /d 1 /f"), null),

                // configure sec2 to not use profile with apm task enabled
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmSec2EnableApm /t REG_DWORD /d 0 /f"), null),

                // default gpu operation mode
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMGpuOperationMode /t REG_DWORD /d 0 /f"), null),

                // disables silentrunning performance levels
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v MaxPerfWithPerfMon /t REG_DWORD /d 0 /f"), null),

                // disable lowering mclk
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmOptp2LowerMclk /t REG_DWORD /d 0 /f"), null),

                // disable slowdowns
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmOverrideIdleSlowdownSettings /t REG_DWORD /d 0 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMClkSlowDown /t REG_DWORD /d 71303168 /f"), null),

                // disable d3 related features
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMD3Feature /t REG_DWORD /d 2 /f"), null),

                // disable 10 types of acpi calls from the resource manager to the sbios
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisableACPI /t REG_DWORD /d 1023 /f"), null),

                // disable native pcie l1
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMNativePcieL1WarFlags /t REG_DWORD /d 16 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RM303107 /t REG_DWORD /d 16 /f"), null),

                // force disable clear perfmon and reset level when entering d4 state
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMResetPerfMonD4 /t REG_DWORD /d 0 /f"), null),

                // not allow mclk switching
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RM592311 /t REG_DWORD /d 2 /f"), null),

                // disable edc replay
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableEDC /t REG_DWORD /d 1 /f"), null),

                // disable lpwr fsms on init
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMElpgStateOnInit /t REG_DWORD /d 3 /f"), null),

                // disable thermal policy and thermal slowdown
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmThermPolicyOverride /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmThermPolicySwSlowdownOverride /t REG_DWORD /d 1 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ThermalPolicySW1 /t REG_DWORD /d 0 /f"), null),
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmThermalCacheDisable /t REG_DWORD /d 1 /f"), null),

                // disable optimusboost acpi
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmGpsACPIType /t REG_DWORD /d 0 /f"), null),

                // force power steering off
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmGpsPowerSteeringEnable /t REG_DWORD /d 0 /f"), null),

                // disable cpu utilization controller
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmGpsCpuUtilPoll /t REG_DWORD /d 0 /f"), null),

                // force never power off the mios
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmMIONoPowerOff /t REG_DWORD /d 1 /f"), null),

                // force highest nvlink link power states
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMNvLinkControlLinkPM /t REG_DWORD /d 170 /f"), null),

                // disable noise aware pll
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmEnableNoiseAwarePll /t REG_DWORD /d 0 /f"), null),

                // disable optimal power for padlink pll
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableOptimalPowerForPadlinkPll /t REG_DWORD /d 1 /f"), null),

                // disable clkreq and deep l1
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RM2779240 /t REG_DWORD /d 5 /f"), null),

                // disable the power-off-dram-pll-when-unused feature
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmClkPowerOffDramPllWhenUnused /t REG_DWORD /d 0 /f"), null),

                // disable opsb (optional power saving bundle)
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMOPSB /t REG_DWORD /d 10914 /f"), null),

                // disable slides mclk
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v SlideMCLK /t REG_DWORD /d 0 /f"), null),

                // disable rtd3 d3hot
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMForceRtd3D3Hot /t REG_DWORD /d 2 /f"), null),

                // disable uphy init sequence
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMNvlinkUPHYInitControl /t REG_DWORD /d 16 /f"), null),

                // disable genoa system power controller
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmGpsGenoa /t REG_DWORD /d 0 /f"), null),

                // disable telemetry collection
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMNvTelemetryCollection /t REG_DWORD /d 0 /f"), null),

                // disable aggressive vblank
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisableAggressiveVblank /t REG_DWORD /d 1 /f"), null),

                // disable glitch free mclk
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v GlitchFreeMClk /t REG_DWORD /d 0 /f"), null),

                // disable registry caching
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisableRegistryCaching /t REG_DWORD /d 15 /f"), null),

                // disable hulk features
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmHulkDisableFeatures /t REG_DWORD /d 7 /f"), null),

                // enable d3 pc latency
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v D3PCLatency /t REG_DWORD /d 1 /f"), null),

                // disable ms hybrid
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v EnableMsHybrid /t REG_DWORD /d 0 /f"), null),

                // ignore hulk errors
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmIgnoreHulkErrors /t REG_DWORD /d 1 /f"), null),

                // disable illegal compstat access
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableIntrIllegalCompstatAccess /t REG_DWORD /d 1 /f"), null),

                // disable fan diagnostics
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RmDisableFanDiag /t REG_DWORD /d 1 /f"), null),

                // set panel refresh rate
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v SetPanelRefreshRate /t REG_DWORD /d 0 /f"), null),

                // disable rc on bar fault
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableRcOnBarFault /t REG_DWORD /d 1 /f"), null),

                // enable powermizer
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerMizerEnable /t REG_DWORD /d 1 /f"), null),

                // set powermizer level to max performance
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerMizerLevel /t REG_DWORD /d 1 /f"), null),

                // set powermizer level ac to max performance
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerMizerLevelAC /t REG_DWORD /d 1 /f"), null),

                // set powermizer hard level to max performance
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerMizerHardLevel /t REG_DWORD /d 1 /f"), null),

                // set powermizer hard level ac to max performance
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerMizerHardLevelAC /t REG_DWORD /d 1 /f"), null),

                // set powermizer default to max performance
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerMizerDefault /t REG_DWORD /d 1 /f"), null),

                // set powermizer default ac to max performance
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v PowerMizerDefaultAC /t REG_DWORD /d 1 /f"), null),

                // disable non-contiguous allocation
                ("Configuring Miscellaneous NVIDIA Settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v RMDisableNoncontigAlloc /t REG_DWORD /d 1 /f"), null),

                // disable dynamic P-State/adaptive clocking
                ("Disabling dynamic P-State", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""DisableDynamicPstate"" /t REG_DWORD /d 1 /f"), null),
                
                // disable asynchronous p-state changes
                ("Disabling dynamic P-State", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""DisableAsyncPstates"" /t REG_DWORD /d 1 /f"), null),

                // disable display power savings
                ("Disabling Display Power Savings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""DisplayPowerSaving"" /t REG_DWORD /d 0 /f"), null),
                ("Disabling Display Power Savings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\NVIDIA Corporation\Global\NVTweak"" /v ""DisplayPowerSaving"" /t REG_DWORD /d 0 /f"), null),

                // disable hd audio power savings
                ("Disabling HD Audio Power Savings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm"" /v ""EnableHDAudioD3Cold"" /t REG_DWORD /d 0 /f"), null),
            
                // disable dlss indicator
                ("Disabling DLSS Indicator", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\NGXCore"" /v ""ShowDlssIndicator"" /t REG_DWORD /d 0 /f"), null),

                // disable automatic updates
                ("Disabling automatic updates", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager"" /v AutoDownload /t REG_DWORD /d 0 /f"), null),

                // disable telemetry
                ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Nvidia Corporation\NvControlPanel2\Client"" /v ""OptInOrOutPreference"" /t REG_DWORD /d 0 /f"), null),
                ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup"" /v ""SendTelemetryData"" /t REG_DWORD /d 0 /f"), null),
                ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID44231 /t REG_DWORD /d 0 /f"), null),
                ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID64640 /t REG_DWORD /d 0 /f"), null),
                ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID66610 /t REG_DWORD /d 0 /f"), null),
                ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c cd /d ""C:\Windows\System32\DriverStore\FileRepository\"" & dir NvTelemetry64.dll /a /b /s & del NvTelemetry64.dll /a /s"), null),

                // disable logging
                ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogDisableMasks /t REG_BINARY /d ""00ffff0f01ffff0f02ffff0f03ffff0f04ffff0f05ffff0f06ffff0f07ffff0f08ffff0f09ffff0f0affff0f0bffff0f0cffff0f0dffff0f0effff0f0fffff0f10ffff0f11ffff0f12ffff0f13ffff0f14ffff0f15ffff0f16ffff0f00ffff1f01ffff1f02ffff1f03ffff1f04ffff1f05ffff1f06ffff1f07ffff1f08ffff1f09ffff1f0affff1f0bffff1f0cffff1f0dffff1f0effff1f0fffff1f00ffff2f01ffff2f02ffff2f03ffff2f04ffff2f05ffff2f06ffff2f07ffff2f08ffff2f09ffff2f0affff2f0bffff2f0cffff2f0dffff2f0effff2f0fffff2f00ffff3f01ffff3f02ffff3f03ffff3f04ffff3f05ffff3f06ffff3f07ffff3f"" /f"), null),
                ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogWarningEntries /t REG_DWORD /d 0 /f"), null),
                ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogPagingEntries /t REG_DWORD /d 0 /f"), null),
                ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogEventEntries /t REG_DWORD /d 0 /f"), null),
                ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogErrorEntries /t REG_DWORD /d 0 /f"), null),

                // disable high-bandwidth digital content protection (hdcp)
                ("Disabling High-Bandwidth Digital Content Protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RMHdcpKeyglobZero"" /t REG_DWORD /d 1 /f"), null),
                ("Disabling High-Bandwidth Digital Content Protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RmDisableHdcp22"" /t REG_DWORD /d 1 /f"), null),
                ("Disabling High-Bandwidth Digital Content Protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""{gpu.RegistryPath}"" /v ""RmSkipHdcp22Init"" /t REG_DWORD /d 1 /f"), null),

                // disable high-definition multimedia interface (hdmi)/displayport (dp) audio
                ("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => await Task.Run(() => new ManagementObjectSearcher("SELECT DeviceID, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE Name LIKE '%High Definition Audio Controller%'").Get().OfType<ManagementObject>().Where(obj => obj["DeviceID"]?.ToString().Contains(gpu.PnPDeviceId[(gpu.PnPDeviceId.LastIndexOf('\\') + 1)..gpu.PnPDeviceId.LastIndexOf('&')], StringComparison.OrdinalIgnoreCase) == true).ToList().ForEach(obj => obj.InvokeMethod("Disable", null, null))), () => gpu.HDMIDPAudio == false)
            };

            return actions;
        }
    }
}