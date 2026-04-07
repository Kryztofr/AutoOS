using AutoOS.Helpers.Device;
using AutoOS.Helpers.GPU;
using AutoOS.Helpers.Monitor;
using AutoOS.Helpers.RAM;
using AutoOS.Views.Installer.Stages;
using Microsoft.Win32;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WinRT;
using Windows.Storage;

namespace AutoOS.Helpers.Network;

public enum NetworkSettingType
{
    Enum,
    Dword,
    Int,
    Edit
}

[GeneratedBindableCustomProperty]
public partial class NetworkAdvancedSetting
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public NetworkSettingType Type { get; set; } = NetworkSettingType.Enum;
    public List<NetworkSettingOption> Options { get; set; } = [];
    public int Base { get; set; } = 10;
    public int? Min { get; set; }
    public int? Max { get; set; }
    public int? Step { get; set; }
    public int? LimitText { get; set; }
    public bool UpperCase { get; set; }
    public bool Optional { get; set; }
    public Dictionary<string, string> RawMetadata { get; set; } = [];
}

[GeneratedBindableCustomProperty]
public partial class NetworkSettingOption
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public static class NetworkHelper
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static List<NetworkAdvancedSetting> GetAdvancedSettings(DeviceInfo device)
    {
        var settings = new List<NetworkAdvancedSetting>();
        if (string.IsNullOrEmpty(device.RegistryPath)) return settings;
        
        using var deviceKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath);
        if (deviceKey == null) return settings;
        
        using var paramsKey = deviceKey.OpenSubKey(@"Ndi\Params");
        if (paramsKey == null) return settings;

        foreach (var paramKeyName in paramsKey.GetSubKeyNames())
        {
            using var paramKey = paramsKey.OpenSubKey(paramKeyName);
            if (paramKey == null) continue;
            
            var typeValue = paramKey.GetValue("type")?.ToString()?.ToLowerInvariant();
            var type = typeValue switch
            {
                "enum" => NetworkSettingType.Enum,
                "dword" => NetworkSettingType.Dword,
                "int" => NetworkSettingType.Int,
                "edit" => NetworkSettingType.Edit,
                _ => NetworkSettingType.Enum
            };

            var setting = new NetworkAdvancedSetting
            {
                Key = paramKeyName,
                Name = paramKey.GetValue("ParamDesc")?.ToString(),
                CurrentValue = deviceKey.GetValue(paramKeyName)?.ToString() ?? string.Empty,
                DefaultValue = paramKey.GetValue("default")?.ToString() ?? string.Empty,
                Type = type
            };

            foreach (var vn in paramKey.GetValueNames())
                setting.RawMetadata[vn] = paramKey.GetValue(vn)?.ToString() ?? string.Empty;

            switch (type)
            {
                case NetworkSettingType.Enum:
                    using (var enumKey = paramKey.OpenSubKey("Enum"))
                    {
                        if (enumKey != null)
                        {
                            foreach (var valueName in enumKey.GetValueNames())
                            {
                                setting.Options.Add(new NetworkSettingOption
                                {
                                    Value = valueName,
                                    Name = enumKey.GetValue(valueName)?.ToString() ?? valueName
                                });
                            }
                        }
                    }
                    break;

                case NetworkSettingType.Dword:
                case NetworkSettingType.Int:
                    var baseValue = paramKey.GetValue("Base")?.ToString();
                    if (baseValue == "8" || baseValue == "16")
                        setting.Base = 16;
                    else
                        setting.Base = 10;

                    if (int.TryParse(paramKey.GetValue("min")?.ToString(), out int min))
                        setting.Min = min;
                    if (int.TryParse(paramKey.GetValue("max")?.ToString(), out int max))
                        setting.Max = max;
                    if (int.TryParse(paramKey.GetValue("step")?.ToString(), out int step))
                        setting.Step = step;
                    break;

                case NetworkSettingType.Edit:
                    if (int.TryParse(paramKey.GetValue("LimitText")?.ToString(), out int limit))
                        setting.LimitText = limit;

                    var upperCaseValue = paramKey.GetValue("UpperCase")?.ToString();
                    setting.UpperCase = upperCaseValue == "1" || upperCaseValue?.ToLowerInvariant() == "true";

                    var optionalValue = paramKey.GetValue("Optional")?.ToString();
                    setting.Optional = optionalValue == "1" || optionalValue?.ToLowerInvariant() == "true";
                    break;
            }

            bool hasValidData = type switch
            {
                NetworkSettingType.Enum => setting.Options.Count > 0,
                NetworkSettingType.Dword or NetworkSettingType.Int => true,
                NetworkSettingType.Edit => true,
                _ => false
            };

            if (hasValidData)
                settings.Add(setting);
        }

        return settings;
    }

    public static void SetAdvancedSetting(DeviceInfo device, string key, string value)
    {
        Microsoft.Win32.Registry.LocalMachine.OpenSubKey(device.RegistryPath, true)?.SetValue(key, value, RegistryValueKind.String);
    }

    public static bool OptimizeAdapter(DeviceInfo device)
    {
        var settings = GetAdvancedSettings(device);
        bool anyChanged = false;

        if (device.NicType == NicDeviceType.WiFi)
        {
            anyChanged |= ApplySetting(device, settings, "2.4G Wireless Mode", "IEEE 802.11b/g/n/ax");
            anyChanged |= ApplySetting(device, settings, "5G Wireless Mode", "IEEE 802.11a/n/ac/ax");
            anyChanged |= ApplySetting(device, settings, "802.11ax/ac/n/abg", "1. 802.11ax");
            anyChanged |= ApplySetting(device, settings, "802.11a/b/g Wireless Mode", "6. Dual Band 802.11a/b/g");
            anyChanged |= ApplySetting(device, settings, "802.11be/ax/ac/n/abg", "1. 802.11be");
            anyChanged |= ApplySetting(device, settings, "802.11d", "Disabled");
            anyChanged |= ApplySetting(device, settings, "802.11/ac/ax Wireless Mode", "4. 802.11ax");
            anyChanged |= ApplySetting(device, settings, "802.11n channel width for 2.4GHz", "Auto");
            anyChanged |= ApplySetting(device, settings, "802.11n channel width for 5.2GHz", "Auto");
            anyChanged |= ApplySetting(device, settings, "802.11n/ac Wireless Mode", "802.11ac");
            anyChanged |= ApplySetting(device, settings, "802.11n/ac/ax Wireless Mode", "4. 802.11ax");
            anyChanged |= ApplySetting(device, settings, "ARP offload for WoWLAN", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Band Selection", "1. All Band");
            anyChanged |= ApplySetting(device, settings, "Beacon Interval", "100");
            anyChanged |= ApplySetting(device, settings, "Channel Width for 2.4GHz", "Auto");
            anyChanged |= ApplySetting(device, settings, "Channel Width for 5GHz", "Auto");
            anyChanged |= ApplySetting(device, settings, "Channel Width for 6GHz", "Auto");
            anyChanged |= ApplySetting(device, settings, "D0 PacketCoalescing", "Disable");
            anyChanged |= ApplySetting(device, settings, "Dynamic MIMO Power Save", "Disable");
            anyChanged |= ApplySetting(device, settings, "EnableAdaptivity", "Auto");
            anyChanged |= ApplySetting(device, settings, "Fat Channel Intolerant", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Global BG Scan blocking", "Always");
            anyChanged |= ApplySetting(device, settings, "GTK rekeying for WoWLAN", "Disabled");
            anyChanged |= ApplySetting(device, settings, "HLDiffForAdaptivity", "7");
            anyChanged |= ApplySetting(device, settings, "HT mode", "VHT mode");
            anyChanged |= ApplySetting(device, settings, "Idle Power Down Restriction", "Enabled");
            anyChanged |= ApplySetting(device, settings, "L2HForAdaptivity", "Auto");
            anyChanged |= ApplySetting(device, settings, "MIMO Power Save Mode", "No SMPS");
            anyChanged |= ApplySetting(device, settings, "Mixed Mode Protection", "CTS-to-self Enabled");
            anyChanged |= ApplySetting(device, settings, "Multi-Channel Concurrent", "Enabled + Hotspot");
            anyChanged |= ApplySetting(device, settings, "NS offload for WoWLAN", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Packet Coalescing", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Power Saving", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Preferred Band", "1. No Preference");
            anyChanged |= ApplySetting(device, settings, "Preamble Mode", "Short & long");
            anyChanged |= ApplySetting(device, settings, "QoS Support", "Auto");
            anyChanged |= ApplySetting(device, settings, "Roaming Aggressiveness", "1. Lowest");
            anyChanged |= ApplySetting(device, settings, "Roaming Aggressiveness", "1. Disabled");
            anyChanged |= ApplySetting(device, settings, "Roaming aggressiveness", "1.Lowest");
            anyChanged |= ApplySetting(device, settings, "Roaming Sensitivity Level", "Disable");
            anyChanged |= ApplySetting(device, settings, "Sleep on WoWLAN Disconnect", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Throughput Booster", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Transmit Power", "5. Highest");
            anyChanged |= ApplySetting(device, settings, "Transmit Power Level", "1. Highest");
            anyChanged |= ApplySetting(device, settings, "U-APSD Support", "Disabled");
            anyChanged |= ApplySetting(device, settings, "U-APSD support", "Disabled");
            anyChanged |= ApplySetting(device, settings, "USB Mode", "Auto");
            anyChanged |= ApplySetting(device, settings, "Ultra High Band (6GHz)", "Enabled");
            anyChanged |= ApplySetting(device, settings, "VHT 2.4G", "Enable");
            anyChanged |= ApplySetting(device, settings, "Wake on Magic Packet", "Disabled");
            anyChanged |= ApplySetting(device, settings, "WakeOnMagicPacket", "Disable");
            anyChanged |= ApplySetting(device, settings, "Wake on Pattern Match", "Disabled");
            anyChanged |= ApplySetting(device, settings, "WakeOnPatternMatch", "Disable");
            anyChanged |= ApplySetting(device, settings, "Wireless Mode", "6. 802.11a/b/g");
            anyChanged |= ApplySetting(device, settings, "Wireless Mode", "12 - 11 a/b/g/n/ac");
        }
        else if (device.NicType == NicDeviceType.LAN)
        {
            anyChanged |= ApplySetting(device, settings, "Adaptive Inter-Frame Spacing", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Advanced EEE", "Disabled");
            anyChanged |= ApplySetting(device, settings, "ARP Offload", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Auto Disable Gigabit", "Disabled");
            anyChanged |= ApplySetting(device, settings, "DMA Coalescing", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Downshift retries", "0");
            //anyChanged |= ApplySetting(device, settings, "Enable PME", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Energy Efficient Ethernet", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Energy Efficient Ethernet", "Off");
            anyChanged |= ApplySetting(device, settings, "Energy-Efficient Ethernet", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Flow Control", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Gigabit Lite", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Gigabit Master Slave Mode", "Auto Detect");
            anyChanged |= ApplySetting(device, settings, "Gigabit PHY Mode", "Auto Detect");
            anyChanged |= ApplySetting(device, settings, "Green Ethernet", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Idle power down restriction", "Enabled");
            if (settings.Any(s => s.Name == "Interrupt Moderation Rate"))
            {
                anyChanged |= ApplySetting(device, settings, "Interrupt Moderation", "Enabled");
                anyChanged |= ApplySetting(device, settings, "Interrupt Moderation Rate", "Medium");
            }
            else
            {
                anyChanged |= ApplySetting(device, settings, "Interrupt Moderation", "Disabled");
            }
            anyChanged |= ApplySetting(device, settings, "IPv4 Checksum Offload", "Rx & Tx Enabled");
            anyChanged |= ApplySetting(device, settings, "Jumbo Packet", "Disabled");
            anyChanged |= ApplySetting(device, settings, "JumboPacket", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Large Send Offload Version 1", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Large Send Offload V2 (IPv4)", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Large Send Offload V2 (IPv6)", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Link Speed Battery Saver", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Locally Administered Address", "--");
            anyChanged |= ApplySetting(device, settings, "Log Link State Event", "Disabled");
            //anyChanged |= ApplySetting(device, settings, "Maximum Number of RSS Queues", "4 Queues");
            anyChanged |= ApplySetting(device, settings, "Multi-Channel Concurrent", "Disabled");
            anyChanged |= ApplySetting(device, settings, "NS Offload", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Packet Priority & VLAN", "Packet Priority & VLAN Enabled");
            anyChanged |= ApplySetting(device, settings, "PCI Express Link Power Saving", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Power Saving", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Power Saving Mode", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Preamble Mode", "Short");
            anyChanged |= ApplySetting(device, settings, "Protocol ARP Offload", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Protocol NS Offload", "Enabled");
            anyChanged |= ApplySetting(device, settings, "PTP Hardware Timestamp", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Recv Segment Coalescing (IPv4)", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Recv Segment Coalescing (IPv6)", "Enabled");
            anyChanged |= ApplySetting(device, settings, "Receive Side Scaling", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Reduce Speed On Power Down", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Selective Suspend", "Disabled");
            anyChanged |= ApplySetting(device, settings, "SelectiveSuspend", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Selective Suspend Idle Timeout", "60");
            anyChanged |= ApplySetting(device, settings, "Software Timestamp ", "Disabled");
            //anyChanged |= ApplySetting(device, settings, "Shutdown Wake-On-Lan", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Speed & Duplex", "Auto Negotiation");
            anyChanged |= ApplySetting(device, settings, "System Idle Power Saver", "Disabled");
            anyChanged |= ApplySetting(device, settings, "TCP Checksum Offload (IPv4)", "Rx & Tx Enabled");
            anyChanged |= ApplySetting(device, settings, "TCP Checksum Offload (IPv6)", "Rx & Tx Enabled");
            anyChanged |= ApplySetting(device, settings, "TCP/UDP Checksum Offload (IPv4)", "Rx & Tx Enabled");
            anyChanged |= ApplySetting(device, settings, "TCP/UDP Checksum Offload (IPv6)", "Rx & Tx Enabled");
            anyChanged |= ApplySetting(device, settings, "UDP Checksum Offload (IPv4)", "Rx & Tx Enabled");
            anyChanged |= ApplySetting(device, settings, "UDP Checksum Offload (IPv6)", "Rx & Tx Enabled");
            anyChanged |= ApplySetting(device, settings, "Ultra Low Power Mode", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Wait for Link", "Off");
            // anyChanged |= ApplySetting(device, settings, "Wake from power off state", "Disabled");
            // anyChanged |= ApplySetting(device, settings, "Wake from S0ix on Magic Packet", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Wake on Link", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Wake on Link Settings", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Wake On Link Up", "Disabled");
            // anyChanged |= ApplySetting(device, settings, "Wake on Magic Packet", "Disabled");
            // anyChanged |= ApplySetting(device, settings, "Wake On Magic Packet From S5", "Disabled");
            // anyChanged |= ApplySetting(device, settings, "Wake on magic packet when system is in the S0ix power state", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Wake on Pattern Match", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Wake on pattern match", "Disabled");
            anyChanged |= ApplySetting(device, settings, "Wake on Ping", "Disabled");
            anyChanged |= ApplySetting(device, settings, "WOL & Shutdown Link Speed", "Not Speed Down");
            anyChanged |= ApplySetting(device, settings, "WOL Link Power Saving", "Disabled");
        }

        return anyChanged;
    }

    private static bool ApplySetting(DeviceInfo device, List<NetworkAdvancedSetting> settings, string displayName, string displayValue)
    {
        var setting = settings.FirstOrDefault(s => s.Name == displayName);
        if (setting == null) return false;

        var option = setting.Options.FirstOrDefault(o => o.Name == displayValue);
        if (option == null) return false;

        string currentVal = (setting.CurrentValue ?? "").Trim();
        string targetVal = (option.Value ?? "").Trim();

        if (!string.Equals(currentVal, targetVal, StringComparison.OrdinalIgnoreCase))
        {
            SetAdvancedSetting(device, setting.Key, option.Value);
            setting.CurrentValue = option.Value;
            return true;
        }
        return false;
    }

    public static async Task LogNetworkSettings()
    {
        string installStart = localSettings.Values["Install_Start"]?.ToString() ?? "N/A";
        string installEnd = localSettings.Values["Install_End"]?.ToString() ?? "N/A";

        string cpuName = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "")?.ToString() ?? "";

        string manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", "")?.ToString() ?? "";

        string product = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";

        string motherboard = $"{manufacturer} {product}".Trim();

        string ram = $"{(RamHelper.GetRam() is var r ? $"{r.CapacityGB:N1} GB {r.DDRVersion} @ {r.MaxSpeedMHz} MHz" : "")}";

        var currentGpus = GpuHelper.GetGPUs();
        string gpus = string.Join(", ", currentGpus.Select(gpu => $"{gpu.DeviceName} (DeviceId: {gpu.DeviceId}, Install: {PreparingStage.GPUs.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId)?.Install ?? true}, {gpu.CurrentVersion})"));

        string monitors = string.Join(", ", MonitorHelper.GetMonitors().Select(m => $"{m.DeviceName} ({m.Resolution.Width}x{m.Resolution.Height} @ {m.RefreshRate} Hz)"));

        var nicsList = DeviceHelper.GetDevices(DeviceType.NIC);
        string nics = nicsList.Count > 0 ? string.Join("\n", nicsList.Select(n => $"{n.FriendlyName} (DeviceId: {n.DeviceId}, Current Version: {n.DriverType} {n.CurrentVersion}, Connected: {n.IsActive})")) : "N/A";

        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        string build = key.GetValue("CurrentBuild")?.ToString() ?? "";
        string ubr = key.GetValue("UBR")?.ToString() ?? "";
        string osVersion = $"{build}.{ubr}";

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
            catch
            {

            }
        }

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(
                $"<@{discordId}>\n" +
                $"{discordUsername}\n" +
                $"{motherboard}\n" +
                $"{cpuName}\n" +
                $"{ram}\n" +
                $"{gpus}\n" +
                $"{monitors}\n" +
                $"{nics}\n" +
                $"{osVersion}\n" +
                $"Install start: {installStart}\n" +
                $"Install end: {installEnd}\n" +
                $"{ProcessInfoHelper.Version}"
            ), "content" }
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

            var settings = GetAdvancedSettings(device);
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

                if (setting.Type == NetworkSettingType.Enum && setting.Options.Count > 0)
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

        if (sb.Length > 0)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("AutoOS"));
            multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(sb.ToString())), "file", "network_settings.md");
            await client.PostAsync("https://discord.com/api/webhooks/1444743232679579779/kY5L3BixE536ykBsk5t4ymdkrBn0EvqN4YAYAkFwi-wDP1uQOkZinTy_HgD__UptnGMM", multipart);
        }
    }
}
