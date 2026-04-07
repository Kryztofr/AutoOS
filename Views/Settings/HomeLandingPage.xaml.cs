using AutoOS.Views.Installer.Actions;
using AutoOS.Views.Updater;
using AutoOS.Views.Updater.Stages;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Win32;
using System.Net.Http.Headers;
using System.Text.Json;
using Windows.Storage;

namespace AutoOS.Views.Settings
{
    public sealed partial class HomeLandingPage : Page
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static readonly HttpClient httpClient = new()
        {
            DefaultRequestHeaders =
            {
                UserAgent =
                {
                    new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
                }
            }
        };

        public HomeLandingPage()
        {
            InitializeComponent();
            #if !DEBUG
                Loaded += CheckForUpdates;
            #endif
        }

        private async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            Version currentVersion = new(ProcessInfoHelper.Version);

            localSettings.Values.TryGetValue("Version", out var storedVersionObj);
            Version storedVersion = storedVersionObj is string storedVersionStr ? new(storedVersionStr) : null;

            if (currentVersion.CompareTo(storedVersion) > 0)
            {
                try
                {
                    using var doc = JsonDocument.Parse(await httpClient.GetStringAsync($"https://api.github.com/repos/tinodin/AutoOS/releases/tags/v{currentVersion}"));

                    if (doc.RootElement.TryGetProperty("body", out var body))
                    {
                        string rawChangelog = body.GetString();
                        string changelog = rawChangelog.Replace("`", "")[rawChangelog.IndexOf("- ")..];

                        var contentDialog = new ContentDialog
                        {
                            Title = $"What's new in AutoOS v{currentVersion}",
                            Content = new ScrollViewer
                            {
                                Content = new MarkdownTextBlock
                                {
                                    Text = changelog,
                                    Config = new MarkdownConfig()
                                },
                                Padding = new Thickness(0, 0, 36, 0)
                            },
                            CloseButtonText = "Close",
                            XamlRoot = XamlRoot
                        };

                        contentDialog.Resources["ContentDialogMaxWidth"] = 1000;
                        contentDialog.Resources["ContentDialogMaxHeight"] = 1000;

                        await contentDialog.ShowAsync();
                    }
                }
                catch
                {   }

                var updateDialog = new UpdateDialog();
                var actions = UpdateStage.UpdateActions(updateDialog);

                if (actions.Count > 0)
                {
                    var updater = new ContentDialog
                    {
                        Title = "Applying Update...",
                        Content = updateDialog,
                        Resources = new ResourceDictionary
                        {
                            ["ContentDialogMinHeight"] = 0.0,
                            ["ContentDialogMinWidth"] = 500,
                            ["ContentDialogMaxWidth"] = 1000
                        },
                        XamlRoot = XamlRoot
                    };

                    _ = updater.ShowAsync();
                    await updateDialog.RunActions(actions);
                    updateDialog.SetStatus("Update complete.");
                    updateDialog.SetSuccess();
                    await Task.Delay(1000);
                    updater.Hide();
                }

                localSettings.Values["Version"] = currentVersion.ToString();
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\AutoOS", "IsInstalled", 1, RegistryValueKind.DWord);
                await ProcessActions.Log();
            }

            try
            {
                bool includePrereleases = localSettings.Values["IncludePrerelease"] as bool? ?? false;

                var json = await httpClient.GetStringAsync("https://api.github.com/repos/tinodin/AutoOS/releases");
                using var releasesDoc = JsonDocument.Parse(json);

                var releases = releasesDoc.RootElement.EnumerateArray()
                    .Select(release =>
                    {
                        string tag = release.GetProperty("tag_name").GetString();
                        return new
                        {
                            Version = Version.Parse(tag.TrimStart('v')),
                            IsPrerelease = release.GetProperty("prerelease").GetBoolean(),
                            Json = release
                        };
                    })
                    .Where(x => x.Version.CompareTo(currentVersion) > 0)
                    .Where(x => includePrereleases || (!x.IsPrerelease && x.Version.Revision <= 0))
                    .OrderBy(x => x.Version)
                    .ToList();

                if (releases.Count == 0)
                    return;

                var nextRelease = releases.First();
                var assets = nextRelease.Json.GetProperty("assets");
                string downloadUrl = assets.EnumerateArray()
                    .First(a => a.GetProperty("name").GetString() == "AutoOS.msix")
                    .GetProperty("browser_download_url")
                    .GetString();

                Version nextVersion = nextRelease.Version;

                var confirmDialog = new ContentDialog
                {
                    Title = "Update Available",
                    Content = $"Do you want to update AutoOS from v{currentVersion} to v{nextVersion}?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };

                if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
                    return;

                var msixDialog = new UpdateDialog();

                var msixUpdater = new ContentDialog
                {
                    Title = $"Updating to AutoOS v{nextVersion}...",
                    Content = msixDialog,
                    Resources = new ResourceDictionary
                    {
                        ["ContentDialogMinHeight"] = 0.0,
                        ["ContentDialogMinWidth"] = 500,
                        ["ContentDialogMaxWidth"] = 1000
                    },
                    XamlRoot = XamlRoot
                };

                _ = msixUpdater.ShowAsync();

                await PackageStage.PackageActions(downloadUrl, msixDialog);
            }
            catch { }
        }
    }
}