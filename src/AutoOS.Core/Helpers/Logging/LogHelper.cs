using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.GPU.Models;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Monitor;
using AutoOS.Core.Helpers.OS;
using AutoOS.Core.Helpers.RAM;
using DevWinUI;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json;
using System.Text;
using Windows.Storage;

namespace AutoOS.Core.Helpers.Logging;

public static partial class LogHelper
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    private static readonly HttpClient httpClient = new(new SocketsHttpHandler
		{
			SslOptions = new SslClientAuthenticationOptions
			{
				EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
			}
		})
		{
			DefaultRequestHeaders =
			{
				UserAgent =
				{
					new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
				}
			}
		};

    public static async Task Log(IEnumerable<GpuInfo> selectedGpus = null, bool bios = false)
    {
        string hardwareInfo = GetHardwareInfo(selectedGpus, false);
        var (discordId, discordUsername) = GetDiscordUserInfo();
        
        using var multipart = new MultipartFormDataContent
        {
            { new StringContent($"<@{discordId}>\n{discordUsername}\n{hardwareInfo}\n{ProcessInfoHelper.Version}"), "content" }
        };

        if (bios)
        {
            string nvramPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt");
            if (File.Exists(nvramPath))
            {
                multipart.Add(new ByteArrayContent(File.ReadAllBytes(nvramPath)), "file", "nvram.txt");
            }
        }

        string webhook = bios ? LogConfig.Bios : LogConfig.Log;
        if (!string.IsNullOrEmpty(webhook))
        {
            await httpClient.PostAsync(webhook, multipart);
        }
    }

    public static async Task LogError(Exception ex, IEnumerable<GpuInfo> selectedGpus = null, string actionTitle = null)
    {
        string hardwareInfo = GetHardwareInfo(selectedGpus, true);
        var (discordId, discordUsername) = GetDiscordUserInfo();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<@{discordId}>");
        sb.AppendLine(discordUsername);
        sb.AppendLine(hardwareInfo);
        if (!string.IsNullOrEmpty(actionTitle)) sb.AppendLine($"Action Title: {actionTitle}");
        sb.AppendLine($"{ex.GetType().FullName}");
        sb.AppendLine($"Message: {ex.Message}");
        sb.AppendLine($"HResult: 0x{ex.HResult:X}");
        sb.AppendLine($"Source: {ex.Source}");
        sb.AppendLine("StackTrace:");
        sb.AppendLine(ex.StackTrace);
        if (ex.InnerException != null) sb.AppendLine($"\nInnerException:\n{ex.InnerException}");
        sb.AppendLine($"{ProcessInfoHelper.Version}");

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(sb.ToString()), "content" }
        };

        if (!string.IsNullOrEmpty(LogConfig.Error))
        {
            await httpClient.PostAsync(LogConfig.Error, multipart);
        }
    }

    public static async Task LogNetworkSettings(IEnumerable<GpuInfo> selectedGpus = null)
    {
        string hardwareInfo = GetHardwareInfo(selectedGpus, false);
        var (discordId, discordUsername) = GetDiscordUserInfo();

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent($"<@{discordId}>\n{discordUsername}\n{hardwareInfo}\n{ProcessInfoHelper.Version}"), "content" }
        };

        var devices = DeviceHelper.GetDevices(DeviceType.NIC);
        var sb = new StringBuilder();

        foreach (var device in devices)
        {
            if (device.NicType != NicDeviceType.WiFi && device.NicType != NicDeviceType.LAN) continue;

            sb.AppendLine($"# Adapter: {device.FriendlyName}");
            sb.AppendLine($"- **PnpID**: `{device.PnpDeviceId}`");
            sb.AppendLine($"- **RegistryPath**: `{device.RegistryPath}`");
            sb.AppendLine($"- **Driver**: `{device.DriverType} {device.CurrentVersion}`");

            var settings = AutoOS.Core.Helpers.Network.NetworkHelper.GetAdvancedSettings(device);
            foreach (var setting in settings.OrderBy(s => s.Name))
            {
                sb.AppendLine();
                sb.AppendLine($"## {setting.Name}");
                sb.AppendLine($"- **Key**: `{setting.Key}`");
                sb.AppendLine($"- **Type**: `{setting.Type}`");

                var currentOption = setting.Options.FirstOrDefault(o => o.Value == setting.CurrentValue);
                string currentText = currentOption != null ? $" ({currentOption.Name})" : "";
                sb.AppendLine($"- **Current Value**: `{setting.CurrentValue}`{currentText}");

                if (!string.IsNullOrEmpty(setting.DefaultValue))
                {
                    var defaultOption = setting.Options.FirstOrDefault(o => o.Value == setting.DefaultValue);
                    string defaultText = defaultOption != null ? $" ({defaultOption.Name})" : "";
                    sb.AppendLine($"- **Default Value**: `{setting.DefaultValue}`{defaultText}");
                }

                sb.AppendLine("- **Parameters**:");
                foreach (var meta in setting.RawMetadata.OrderBy(m => m.Key))
                {
                    sb.AppendLine($"  - **{meta.Key}**: `{meta.Value}`");
                }

                if (setting.Type == Network.Models.NetworkSettingType.Enum && setting.Options.Count > 0)
                {
                    sb.AppendLine("- **Options**:");
                    foreach (var opt in setting.Options)
                    {
                        sb.AppendLine($"  - `{opt.Value}`: {opt.Name}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        if (sb.Length > 0 && !string.IsNullOrEmpty(LogConfig.Network))
        {
            multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(sb.ToString())), "file", "network_settings.md");
            await httpClient.PostAsync(LogConfig.Network, multipart);
        }
    }

    private static string GetHardwareInfo(IEnumerable<GpuInfo> selectedGpus = null, bool includeVendorId = false)
    {
        string installStart = localSettings.Values["Install_Start"]?.ToString() ?? "N/A";
        string installEnd = localSettings.Values["Install_End"]?.ToString() ?? "N/A";

        string cpuName = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "")?.ToString() ?? "";
        string manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", "")?.ToString() ?? "";
        string product = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";
        string motherboard = $"{manufacturer} {product}".Trim();

        var ramInfo = RamHelper.GetRam();
        string ram = ramInfo != null ? $"{ramInfo.CapacityGB:N1} GB {ramInfo.DDRVersion} @ {ramInfo.MaxSpeedMHz} MHz" : "N/A";

        var currentGpus = GpuHelper.GetGPUs();
        string gpus = string.Join(", ", currentGpus.Select(gpu => 
        {
            bool install = selectedGpus?.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId)?.Install ?? true;
            return $"{gpu.DeviceName} (DeviceId: {gpu.DeviceId}, Install: {install}, {gpu.CurrentVersion})";
        }));

        string monitors = string.Join(", ", MonitorHelper.GetMonitors().Select(m => $"{m.DeviceName} ({m.Resolution.Width}x{m.Resolution.Height} @ {m.RefreshRate} Hz)"));

        var nicsList = DeviceHelper.GetDevices(DeviceType.NIC);
        string nics = nicsList.Count > 0 ? string.Join("\n", nicsList.Select(n => 
        {
            string vendorPart = includeVendorId ? $", VendorId: {n.VendorId}" : "";
            return $"{n.FriendlyName} (DeviceId: {n.DeviceId}{vendorPart}, Current Version: {n.DriverType} {n.CurrentVersion}, Connected: {n.IsActive})";
        })) : "N/A";

        string osVersionString = OSHelper.GetWindowsVersionString();

        return $"{motherboard}\n{cpuName}\n{ram}\n{gpus}\n{monitors}\n{nics}\n{osVersionString}\nInstall start: {installStart}\nInstall end: {installEnd}";
    }

    private static (string Id, string Username) GetDiscordUserInfo()
    {
        string discordId = "Failed to get Discord account id";
        string discordUsername = "Failed to get Discord username";

        string discordJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "sentry", "scope_v3.json");
        if (File.Exists(discordJsonPath))
        {
            try
            {
                string jsonText = File.ReadAllText(discordJsonPath);
                using JsonDocument doc = JsonDocument.Parse(jsonText);

                if (doc.RootElement.TryGetProperty("scope", out var scope) &&
                    scope.TryGetProperty("user", out var user))
                {
                    discordId = user.GetProperty("id").GetString() ?? discordId;
                    discordUsername = user.GetProperty("username").GetString() ?? discordUsername;
                }
            }
            catch { }
        }

        if (discordId == "Failed to get Discord account id")
        {
            string discordLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "logs", "renderer_js.log");

            if (File.Exists(discordLogPath))
            {
                try
                {
                    using var fs = new FileStream(discordLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("[DatabaseManager] removing database (user: "))
                        {
                            int startIndex = line.IndexOf("user: ") + 6;
                            int endIndex = line.IndexOf(",", startIndex);
                            if (startIndex != -1 && endIndex != -1)
                            {
                                discordId = line.Substring(startIndex, endIndex - startIndex);
                            }
                        }
                    }
                }
                catch { }
            }
        }
        return (discordId, discordUsername);
    }
}
