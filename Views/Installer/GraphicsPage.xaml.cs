using System.Management;
using Windows.Storage;

namespace AutoOS.Views.Installer;

public sealed partial class GraphicsPage : Page
{
    private bool isInitializingHDCPState = true;
    private bool isInitializingHDMIDPAudioState = true;
    private bool isInitializingOBSState = true;
    private static readonly HttpClient httpClient = new();
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public GraphicsPage()
    {
        InitializeComponent();
        GetGPUs();
        GetHDCPState();
        GetHDMIDPAudioState();
        GetMsiProfile();
        GetOBSState();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(GraphicsPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    private async void GetGPUs()
    {
        var detectedGPUs = new List<string>();
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
            string pnpDeviceId = obj["PNPDeviceID"]?.ToString();
            if (string.IsNullOrEmpty(pnpDeviceId) || !pnpDeviceId.StartsWith("PCI\\VEN_") || !pnpDeviceId.Contains("&DEV_"))
                continue;

            string vendorId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("VEN_") + 4, 4).ToLowerInvariant();
            string deviceId = pnpDeviceId.Substring(pnpDeviceId.IndexOf("DEV_") + 4, 4).ToLowerInvariant();

            if (!pciDb.TryGetValue(vendorId, out var vendor)) continue;
            if (!vendor.Devices.TryGetValue(deviceId, out var rawDeviceName)) continue;

            string deviceName = rawDeviceName.Split('[', ']') is { Length: > 1 } parts ? parts[1] : rawDeviceName;
            string codename = rawDeviceName.Split('[')[0].Trim();

            switch (vendorId)
            {
                case "10de":
                    Nvidia_SettingsGroup.Visibility = Visibility.Visible;
                    Nvidia_SettingsGroup.Header = $"NVIDIA {deviceName}";
                    detectedGPUs.Add("NVIDIA");
                    break;

                case "1002":
                    string[] amdRx = { "Navi 10", "Navi 14", "Navi 21", "Navi 22", "Navi 23", "Navi 31", "Navi 32", "Navi 33", "Navi 44", "Navi 48" };
                    if (amdRx.Any(c => codename.Contains(c)))
                    {
                        Amd_SettingsGroup.Visibility = Visibility.Visible;
                        Amd_SettingsGroup.Header = $"AMD {deviceName}";
                        detectedGPUs.Add("AMD Radeon™ RX 5000 - 9000 series");
                    }
                    break;

                case "8086":
                    Intel_SettingsGroup.Visibility = Visibility.Visible;
                    Intel_SettingsGroup.Header = $"INTEL {deviceName}";

                    string[] intel6th = { "Skylake", "Apollo Lake" };
                    string[] intel7to10 = { "Kaby Lake", "Coffee Lake", "Whiskey Lake", "Comet Lake", "Ice Lake", "Lakefield", "Elkhart Lake" };
                    string[] intel11to14 = { "Tiger Lake", "Alder Lake", "Raptor Lake", "DG1" };
                    string[] intelArc = { "Battlemage", "Meteor Lake", "Lunar Lake", "Arrow Lake", "Panther Lake" };

                    if (intel6th.Any(c => codename.Contains(c)) || intel7to10.Any(c => codename.Contains(c)) || intel11to14.Any(c => codename.Contains(c)) || intelArc.Any(c => codename.Contains(c)))
                    {
                        Intel_SettingsGroup.Visibility = Visibility.Visible;
                        Intel_SettingsGroup.Header = $"INTEL {deviceName}";

                        if (intel6th.Any(c => codename.Contains(c)))
                            detectedGPUs.Add("Intel® 6th Gen Processor Graphics");
                        else if (intel7to10.Any(c => codename.Contains(c)))
                            detectedGPUs.Add("Intel® 7th-10th Gen Processor Graphics");
                        else if (intel11to14.Any(c => codename.Contains(c)))
                            detectedGPUs.Add("Intel® 11th-14th Gen Processor Graphics");
                        else if (intelArc.Any(c => codename.Contains(c)))
                            detectedGPUs.Add("Intel® Arc™ Graphics");
                    }
                    break;
            }
        }

        localSettings.Values["GpuBrand"] = string.Join(", ", detectedGPUs);
    }

    private void GetHDCPState()
    {
        if (!localSettings.Values.TryGetValue("HighBandwidthDigitalContentProtection", out object value))
        {
            localSettings.Values["HighBandwidthDigitalContentProtection"] = 0;
        }
        else
        {
            HDCP.IsOn = Convert.ToInt32(value) == 1;
        }

        isInitializingHDCPState = false;
    }

    private void HDCP_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingHDCPState) return;

        localSettings.Values["HighBandwidthDigitalContentProtection"] = HDCP.IsOn ? 1 : 0;
    }

    private void GetHDMIDPAudioState()
    {
        var toggleSettings = new (ToggleSwitch toggle, string key)[]
        {
            (NVIDIA_HDMIDPAudio, "NVIDIAHighDefinitionMultimediaInterface/DisplayPortAudio"),
            (AMD_HDMIDPAudio, "AMDHighDefinitionMultimediaInterface/DisplayPortAudio"),
            (INTEL_HDMIDPAudio, "INTELHighDefinitionMultimediaInterface/DisplayPortAudio")
        };

        foreach (var (toggle, key) in toggleSettings)
        {
            if (!localSettings.Values.TryGetValue(key, out object value))
            {
                localSettings.Values[key] = 1;
                toggle.IsOn = true;
            }
            else
            {
                toggle.IsOn = Convert.ToInt32(value) == 1;
            }
        }

        isInitializingHDMIDPAudioState = false;
    }

    private void HDMIDPAudio_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingHDMIDPAudioState) return;

        ToggleSwitch toggle = sender as ToggleSwitch;

        if (toggle == NVIDIA_HDMIDPAudio)
            localSettings.Values["NVIDIAHighDefinitionMultimediaInterface/DisplayPortAudio"] = toggle.IsOn ? 1 : 0;
        else if (toggle == AMD_HDMIDPAudio)
            localSettings.Values["AMDHighDefinitionMultimediaInterface/DisplayPortAudio"] = toggle.IsOn ? 1 : 0;
        else if (toggle == INTEL_HDMIDPAudio)
            localSettings.Values["INTELHighDefinitionMultimediaInterface/DisplayPortAudio"] = toggle.IsOn ? 1 : 0;
    }

    private void GetMsiProfile()
    {
        var value = localSettings.Values["MsiProfile"] as string;
        if (!string.IsNullOrEmpty(value))
        {
            var infoBar = new InfoBar
            {
                Title = value,
                IsClosable = true,
                IsOpen = true,
                Severity = InfoBarSeverity.Success,
                Margin = new Thickness(4, -4, 4, 12)
            };

            infoBar.CloseButtonClick += (_, _) =>
            {
                localSettings.Values.Remove("MsiProfile");
                MsiAfterburnerInfo.Children.Clear();
            };
            MsiAfterburnerInfo.Children.Add(infoBar);
        }
    }

    private async void BrowseMsi_Click(object sender, RoutedEventArgs e)
    {
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;
        MsiAfterburnerInfo.Children.Clear();

        MsiAfterburnerInfo.Children.Add(new InfoBar
        {
            Title = "Please select a MSI Afterburner profile (.cfg).",
            IsClosable = false,
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Margin = new Thickness(4, -4, 4, 12)
        });

        await Task.Delay(300);

        var picker = new FilePicker(App.MainWindow)
        {
            ShowAllFilesOption = false
        };
        picker.FileTypeChoices.Add("MSI Afterburner profile", ["*.cfg"]);
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            string fileContent = await FileIO.ReadTextAsync(file);

            if (fileContent.Contains("[Startup]"))
            {
                senderButton.IsEnabled = true;
                MsiAfterburnerInfo.Children.Clear();

                localSettings.Values["MsiProfile"] = file.Path;

                var infoBar = new InfoBar
                {
                    Title = file.Path,
                    IsClosable = true,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Success,
                    Margin = new Thickness(4, -4, 4, 12)
                };

                infoBar.CloseButtonClick += (_, _) =>
                {
                    localSettings.Values.Remove("MsiProfile");
                    MsiAfterburnerInfo.Children.Clear();
                };
                MsiAfterburnerInfo.Children.Add(infoBar);
            }
            else
            {
                senderButton.IsEnabled = true;
                MsiAfterburnerInfo.Children.Clear();

                MsiAfterburnerInfo.Children.Add(new InfoBar
                {
                    Title = "The selected file is not a valid MSI Afterburner profile.",
                    IsClosable = false,
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Margin = new Thickness(4, -4, 4, 12)
                });

                await Task.Delay(2000);
                MsiAfterburnerInfo.Children.Clear();
            }
        }
        else
        {
            senderButton.IsEnabled = true;
            MsiAfterburnerInfo.Children.Clear();
            GetMsiProfile();
        }
    }

    private void GetOBSState()
    {
        if (!localSettings.Values.TryGetValue("OBS", out object value))
        {
            localSettings.Values["OBS"] = 0;
        }
        else
        {
            OBS.IsOn = (int)value == 1;
        }

        isInitializingOBSState = false;
    }

    private void OBS_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializingOBSState) return;
        localSettings.Values["OBS"] = OBS.IsOn ? 1 : 0;
    }
}