using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using AutoOS.Helpers.Picker;
using WinRT;

namespace AutoOS.Views.Settings;

public sealed partial class SettingsPage : Page
{
    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    private bool isInitializingSwitchEmulatorState = true;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        GetItems();
        GetSwitchEmulator();
    }

    private void GetItems()
    {
        SwitchEmulator.ItemsSource = new List<SettingsGridViewItem>
        {
            new() { Text = "Eden", ImageSource = "ms-appx:///Assets/Fluent/Eden.png" },
            new() { Text = "Citron", ImageSource = "ms-appx:///Assets/Fluent/Citron.png" },
            new() { Text = "Ryujinx", ImageSource = "ms-appx:///Assets/Fluent/Ryujinx.png" },
        };
    }

    private void GetSwitchEmulator()
    {
        var selectedSwitchEmulator = localSettings.Values["SwitchEmulator"] as string ?? "Eden";
        var switchEmulatorItems = SwitchEmulator.ItemsSource as List<SettingsGridViewItem>;

        var itemToSelect = switchEmulatorItems?.FirstOrDefault(ext => ext.Text == selectedSwitchEmulator) ?? switchEmulatorItems?.FirstOrDefault(ext => ext.Text == "Eden");

        if (itemToSelect != null)
        {
            SwitchEmulator.SelectedItem = itemToSelect;
            DataLocationValue.IsEnabled = itemToSelect.Text == "Ryujinx";
            DataLocationValue.IsReadOnly = itemToSelect.Text != "Ryujinx";

            string exeKey = $"{itemToSelect.Text}Location";
            string dataKey = $"{itemToSelect.Text}DataLocation";

            ExecutableLocationValue.Text = localSettings.Values[exeKey] as string ?? string.Empty;
            DataLocationValue.Text = localSettings.Values[dataKey] as string ?? string.Empty;

            if (itemToSelect.Text == "Ryujinx")
            {
                if (!string.IsNullOrWhiteSpace(DataLocationValue.Text))
                {
                    if (!Directory.Exists(DataLocationValue.Text))
                    {
                        localSettings.Values.Remove(dataKey);
                        DataLocationValue.Text = string.Empty;
                    }
                }

                if (string.IsNullOrWhiteSpace(DataLocationValue.Text))
                {
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), itemToSelect.Text);
                    var gamesDir = Path.Combine(path, "games");

                    if (Directory.Exists(gamesDir) && Directory.GetDirectories(gamesDir).Length > 0)
                    {
                        localSettings.Values[dataKey] = path;
                        DataLocationValue.Text = path;
                    }
                }
            }
            else if (itemToSelect.Text == "Eden" || itemToSelect.Text == "Citron")
            {
                if (!string.IsNullOrWhiteSpace(DataLocationValue.Text))
                {
                    if (!File.Exists(Path.Combine(DataLocationValue.Text, "cache", "game_list", "game_metadata_cache.json")))
                    {
                        localSettings.Values.Remove(dataKey);
                        DataLocationValue.Text = string.Empty;
                    }
                }

                if (string.IsNullOrWhiteSpace(DataLocationValue.Text))
                {
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), itemToSelect.Text.ToLowerInvariant());
                    if (File.Exists(Path.Combine(path, "cache", "game_list", "game_metadata_cache.json")))
                    {
                        localSettings.Values[dataKey] = path;
                        DataLocationValue.Text = path;
                    }
                }
            }
        }

        isInitializingSwitchEmulatorState = false;
    }


    private void SwitchEmulator_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingSwitchEmulatorState) return;

        if (SwitchEmulator.SelectedItem is SettingsGridViewItem selectedItem)
        {
            DataLocationValue.IsEnabled = selectedItem.Text == "Ryujinx";
            DataLocationValue.IsReadOnly = selectedItem.Text != "Ryujinx";

            ExecutableLocationValue.Text = localSettings.Values[$"{selectedItem.Text}Location"] as string ?? string.Empty;
            DataLocationValue.Text = localSettings.Values[$"{selectedItem.Text}DataLocation"] as string ?? string.Empty;

            localSettings.Values["SwitchEmulator"] = selectedItem.Text;
        }
    }

    private void ExecutableLocation_TextChanged(object sender, RoutedEventArgs e)
    {
        if (SwitchEmulator.SelectedItem is SettingsGridViewItem selectedItem)
        {
            string emulator = selectedItem.Text;

            if (!string.IsNullOrWhiteSpace(ExecutableLocationValue?.Text))
            {
                string exeName = Path.GetFileName(ExecutableLocationValue.Text).ToLowerInvariant();

                if ((emulator == "Eden" && exeName == "eden.exe") || (emulator == "Citron" && exeName == "citron.exe") || (emulator == "Ryujinx" && exeName == "ryujinx.exe"))
                {
                    localSettings.Values[$"{emulator}Location"] = ExecutableLocationValue.Text;
                }
            }
            else
            {
                localSettings.Values.Remove($"{emulator}Location");
            }
        }
    }

    private async void ExecutableLocation_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FilePicker(App.MainWindow) { ShowAllFilesOption = false };
        picker.FileTypeChoices.Add("Emulator executable", ["*.exe"]);

        var file = await picker.PickSingleFileAsync();
        if (file != null && SwitchEmulator.SelectedItem is SettingsGridViewItem selectedItem)
        {
            string emulator = selectedItem.Text;
            string exeName = Path.GetFileName(file.Path).ToLowerInvariant();

            if ((emulator == "Eden" && exeName == "eden.exe") ||
                (emulator == "Citron" && exeName == "citron.exe") ||
                (emulator == "Ryujinx" && exeName == "ryujinx.exe"))
            {
                ExecutableLocationValue.Text = file.Path;
                localSettings.Values[$"{emulator}Location"] = file.Path;
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Invalid File",
                    Content = $"Please select the correct executable for {emulator}.",
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = App.MainWindow.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }

    private void DataLocation_TextChanged(object sender, RoutedEventArgs e)
    {
        if (SwitchEmulator.SelectedItem is SettingsGridViewItem selectedItem && selectedItem.Text == "Ryujinx")
        {
            if (!string.IsNullOrWhiteSpace(DataLocationValue?.Text))
            {
                string folderName = Path.GetFileName(DataLocationValue.Text).ToLowerInvariant();

                if (folderName == "portable" || folderName == "ryujinx")
                {
                    localSettings.Values["RyujinxDataLocation"] = DataLocationValue.Text;
                }
            }
            else
            {
                localSettings.Values.Remove("RyujinxDataLocation");
            }
        }
    }

    private async void DataLocation_Click(object sender, RoutedEventArgs e)
    {
        if (SwitchEmulator.SelectedItem is not SettingsGridViewItem selectedItem || selectedItem.Text != "Ryujinx")
            return;

        var picker = new FolderPicker(App.MainWindow)
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        var folder = await picker.PickSingleFolderAsync();
        if (folder == null) return;

        string folderName = Path.GetFileName(folder.Path).ToLowerInvariant();
        if (folderName == "portable" || folderName == "ryujinx")
        {
            DataLocationValue.Text = folder.Path;
            localSettings.Values["RyujinxDataLocation"] = folder.Path;
        }
        else
        {
            var dialog = new ContentDialog
            {
                Title = "Invalid Folder",
                Content = "Please select the correct data folder for Ryujinx.",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void CloneRepo_Click(object sender, RoutedEventArgs e)
    {
        DataPackage package = new();
        package.SetText(GitCloneTextBlock.Text);
        Clipboard.SetContent(package);
    }

    private void LoadSettings()
    {
        if (!localSettings.Values.TryGetValue("IncludePrerelease", out object prereleaseValue))
        {
            localSettings.Values["IncludePrerelease"] = false;
            IncludePrerelease.IsOn = false;
        }
        else
        {
            IncludePrerelease.IsOn = (bool)prereleaseValue;
        }

        if (!localSettings.Values.TryGetValue("LaunchMinimized", out object value))
        {
            localSettings.Values["LaunchMinimized"] = 0;
        }
        else
        {
            if (value is bool)
            {
                localSettings.Values.Remove("LaunchMinimized");
            }
            else
            {
                LaunchMinimized.IsOn = (int)value == 1;
            }
        }
    }

    private void IncludePrerelease_Toggled(object sender, RoutedEventArgs e)
    {
        localSettings.Values["IncludePrerelease"] = IncludePrerelease.IsOn;
    }

    private void LaunchMinimized_Toggled(object sender, RoutedEventArgs e)
    {
        localSettings.Values["LaunchMinimized"] = LaunchMinimized.IsOn ? 1 : 0;
    }
}

[GeneratedBindableCustomProperty]
public partial class SettingsGridViewItem
{
    public string Text { get; set; }
    public string ImageSource { get; set; }
}