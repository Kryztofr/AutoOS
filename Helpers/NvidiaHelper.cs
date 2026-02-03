using Newtonsoft.Json.Linq;
using System.Management;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoOS.Helpers
{
    public static class NvidiaHelper
    {
        // Based on: https://github.com/ElPumpo/TinyNvidiaUpdateChecker
        private static readonly HttpClient httpClient = new();
        public static async Task<(string currentVersion, string newestVersion, string newestDownloadUrl)> CheckUpdate()
        {
            bool isNotebook = false;
            string gpuId = string.Empty;
            string currentVersion = string.Empty;
            string newestVersion = string.Empty;
            string newestDownloadUrl = string.Empty;

            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure");
            foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>().ToArray())
            {
                ushort[] chassisTypes = (ushort[])obj["ChassisTypes"];
                isNotebook = chassisTypes != null && chassisTypes.Any(t => new ushort[] { 1, 8, 9, 10, 11, 12, 14, 18, 21, 31, 32 }.Contains(t));
            }

            string deviceName = null;

            foreach (ManagementBaseObject gpu in new ManagementObjectSearcher("SELECT Name, DriverVersion, PNPDeviceID FROM Win32_VideoController").Get())
            {
                string pnp = gpu["PNPDeviceID"]?.ToString();
                if (string.IsNullOrEmpty(pnp) || !pnp.Contains("&DEV_")) continue;

                string rawName = gpu["Name"]?.ToString() ?? "";
                string rawVersion = gpu["DriverVersion"]?.ToString().Replace(".", "") ?? "";
                currentVersion = rawVersion.Length >= 5 ? rawVersion[^5..].Insert(3, ".") : "";

                if (rawName.StartsWith("NVIDIA"))
                {
                    var match = Regex.Match(rawName, @"(?<=NVIDIA )(.*?)(?= \(| [0-9]+GB| with Max-Q Design| COLLECTORS EDITION)");
                    if (match.Success) deviceName = match.Value.Trim().Replace("Super", "SUPER");
                }

                if (!string.IsNullOrEmpty(deviceName)) break;
            }

            if (string.IsNullOrEmpty(deviceName))
            {
                string pciPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "pci.ids");

                if (!File.Exists(pciPath))
                    await File.WriteAllBytesAsync(pciPath, await httpClient.GetByteArrayAsync("https://raw.githubusercontent.com/pciutils/pciids/master/pci.ids"));

                var pciDb = new Dictionary<string, (string Vendor, Dictionary<string, string> Devices)>(StringComparer.OrdinalIgnoreCase);
                string currentVendor = null;

                foreach (var line in File.ReadLines(pciPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                    if (!char.IsWhiteSpace(line[0]))
                    {
                        var parts = line.Split([' '], 2);
                        if (parts.Length < 2) continue;
                        currentVendor = parts[0].ToLowerInvariant();
                        pciDb[currentVendor] = (parts[1].Trim(), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                    }
                    else if (line.StartsWith('\t') && (line.Length < 2 || line[1] != '\t') && currentVendor != null)
                    {
                        var parts = line.Trim().Split([' '], 2);
                        if (parts.Length < 2) continue;
                        pciDb[currentVendor].Devices[parts[0].ToLowerInvariant()] = parts[1].Trim();
                    }
                }

                foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass='Display'").Get().Cast<ManagementObject>().ToArray())
                {
                    string pnp = obj["PNPDeviceID"]?.ToString();
                    if (string.IsNullOrEmpty(pnp) || !pnp.StartsWith("PCI\\VEN_") || !pnp.Contains("&DEV_")) continue;

                    string vendorId = pnp.Substring(pnp.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
                    string deviceId = pnp.Substring(pnp.IndexOf("DEV_") + 4, 4).ToLowerInvariant();

                    if (!pciDb.TryGetValue(vendorId, out var vendor)) continue;
                    if (!vendor.Devices.TryGetValue(deviceId, out var rawDeviceName)) continue;

                    deviceName = rawDeviceName.Split('[', ']') is { Length: > 1 } parts ? parts[1] : rawDeviceName;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(deviceName))
            {
                string json = await httpClient.GetStringAsync($"https://raw.githubusercontent.com/ZenitH-AT/nvidia-data/main/gpu-data.json");
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty(isNotebook ? "notebook" : "desktop", out JsonElement section) && section.TryGetProperty(deviceName, out JsonElement idElem))
                {
                    gpuId = idElem.GetString();
                }

                string response = await httpClient.GetStringAsync($"https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&pfid={gpuId}&osID=135&dch=1&upCRD=0");

                var driverObj = JObject.Parse(response);
                if ((int)driverObj["Success"] == 1)
                {
                    newestVersion = driverObj["IDS"][0]["downloadInfo"]["Version"].ToString();
                    newestDownloadUrl = driverObj["IDS"][0]["downloadInfo"]["DownloadURL"].ToString();
                }
            }

            return (currentVersion, newestVersion, newestDownloadUrl);
        }
    }
}