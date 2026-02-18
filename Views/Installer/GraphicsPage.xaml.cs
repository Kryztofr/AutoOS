using AutoOS.Helpers.GPU;
using System.Collections.ObjectModel;
using System.Text.Json;
using Windows.Storage;
using AutoOS.Helpers.Picker;

namespace AutoOS.Views.Installer;

public sealed partial class GraphicsPage : Page
{
    private bool isInitializingOBSState = true;
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public ObservableCollection<GpuInfo> GPUs { get; } = [];
    public GraphicsPage()
    {
        InitializeComponent();
        GetGpus();
        GetMsiProfile();
        GetOBSState();
        Unloaded += GraphicsPage_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(GraphicsPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    private void GraphicsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        localSettings.Values["GPUs"] = JsonSerializer.Serialize(GPUs);
    }

    public void GetGpus()
    {
        GPUs.Clear();

        List<GpuInfo> savedGpus = null;

        if (localSettings.Values.TryGetValue("GPUs", out object savedObj))
        {
            try
            {
                savedGpus = JsonSerializer.Deserialize<List<GpuInfo>>(savedObj.ToString());
            }
            catch { }
        }

        var detected = GpuHelper.GetGPUs();

        foreach (var gpu in detected)
        {
            var saved = savedGpus?.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId);

            if (saved != null)
            {
                gpu.Install = saved.Install;
                gpu.HDCP = saved.HDCP;
                gpu.HDMIDPAudio = saved.HDMIDPAudio;
            }

            GPUs.Add(gpu);
        }
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