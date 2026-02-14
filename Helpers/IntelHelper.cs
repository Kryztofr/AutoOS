using PuppeteerSharp;
using System.Management;
using System.Text.RegularExpressions;

namespace AutoOS.Helpers
{
    public static class IntelHelper
    {
        private static readonly HttpClient httpClient = new();

        public static async Task<(string currentVersion, string newestVersion, string newestDownloadUrl)> CheckUpdate()
        {
            string currentVersion = string.Empty;
            string newestVersion = string.Empty;
            string newestDownloadUrl = string.Empty;
            string codename = string.Empty;
            string driverPageUrl = string.Empty;
            string vendorId = string.Empty;
            string deviceId = string.Empty;

            foreach (ManagementBaseObject gpu in new ManagementObjectSearcher("SELECT Name, DriverVersion, PNPDeviceID FROM Win32_VideoController").Get())
            {
                string name = gpu["Name"]?.ToString() ?? "";
                string version = gpu["DriverVersion"]?.ToString() ?? "";
                string pnp = gpu["PNPDeviceID"]?.ToString() ?? "";

                if (!name.StartsWith("Intel", StringComparison.OrdinalIgnoreCase))
                    continue;

                currentVersion = version;

                if (!string.IsNullOrEmpty(pnp) && pnp.StartsWith("PCI\\VEN_") && pnp.Contains("&DEV_"))
                {
                    vendorId = pnp.Substring(pnp.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
                    deviceId = pnp.Substring(pnp.IndexOf("DEV_") + 4, 4).ToLowerInvariant();
                }

                break;
            }

            string pciPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "pci.ids");

            if (!File.Exists(pciPath))
                await File.WriteAllBytesAsync(pciPath, await httpClient.GetByteArrayAsync("https://raw.githubusercontent.com/pciutils/pciids/master/pci.ids"));

            var pciDb = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            string currentVendor = null;

            foreach (var line in File.ReadLines(pciPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

                if (!char.IsWhiteSpace(line[0]))
                {
                    var parts = line.Split([' '], 2);
                    if (parts.Length < 2) continue;
                    currentVendor = parts[0].ToLowerInvariant();
                    pciDb[currentVendor] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else if (line.StartsWith('\t') && currentVendor != null)
                {
                    var parts = line.Trim().Split([' '], 2);
                    if (parts.Length < 2) continue;
                    pciDb[currentVendor][parts[0].ToLowerInvariant()] = parts[1].Trim();
                }
            }

            if (string.IsNullOrEmpty(vendorId) || string.IsNullOrEmpty(deviceId))
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass='Display'").Get().Cast<ManagementObject>())
                {
                    string pnp = obj["PNPDeviceID"]?.ToString();
                    if (string.IsNullOrEmpty(pnp) || !pnp.StartsWith("PCI\\VEN_") || !pnp.Contains("&DEV_")) continue;

                    string vid = pnp.Substring(pnp.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
                    string did = pnp.Substring(pnp.IndexOf("DEV_") + 4, 4).ToLowerInvariant();

                    if (vid != "8086") continue;
                    if (!pciDb.TryGetValue(vid, out var devices)) continue;
                    if (!devices.TryGetValue(did, out var rawDeviceName)) continue;

                    vendorId = vid;
                    deviceId = did;
                    codename = rawDeviceName.Split('[')[0].Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(codename) && !string.IsNullOrEmpty(vendorId) && !string.IsNullOrEmpty(deviceId))
            {
                if (pciDb.TryGetValue(vendorId, out var devices) && devices.TryGetValue(deviceId, out var rawDeviceName))
                    codename = rawDeviceName.Split('[')[0].Trim();
            }

            static string Normalize(string s) => s.Replace(" ", "").Replace("-", "").ToLowerInvariant();

            string[] intel6th = { "Skylake", "Apollo Lake" };
            string[] intel7to10 = { "Kaby Lake", "Coffee Lake", "Whiskey Lake", "Comet Lake", "Ice Lake", "Lakefield", "Elkhart Lake" };
            string[] intel11to14 = { "Tiger Lake", "Alder Lake", "Raptor Lake", "DG1" };
            string[] intelArc = { "Arc", "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake" };

            bool is6thGen = intel6th.Any(c => Normalize(codename).Contains(Normalize(c)));
            if (is6thGen)
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/762755/intel-6th-gen-processor-graphics-windows.html";
            else if (intel7to10.Any(c => Normalize(codename).Contains(Normalize(c))))
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/776137/intel-7th-10th-gen-processor-graphics-windows.html";
            else if (intel11to14.Any(c => Normalize(codename).Contains(Normalize(c))))
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/864990/intel-11th-14th-gen-processor-graphics-windows.html";
            else if (intelArc.Any(c => Normalize(codename).Contains(Normalize(c))))
                driverPageUrl = "https://www.intel.com/content/www/us/en/download/785597/intel-arc-graphics-windows.html";

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

            await MessageBox.ShowAsync(
                $"Codename: {codename}\nCurrent Version: {currentVersion}\nLatest Version: {newestVersion}\nDownload URL: {newestDownloadUrl}",
                "Intel Helper"
            );

            return (currentVersion, newestVersion, newestDownloadUrl);
        }
    }
}
