using Windows.Storage;
using WinRT;

namespace AutoOS.Views.Installer;

public sealed partial class ApplicationsPage : Page
{
    private bool isInitializingOfficeState = true;
    private bool isInitializingDevelopmentState = true;
    private bool isInitializingMusicState = true;
    private bool isInitializingMessagingState = true;
    private bool isInitializingLaunchersState = true;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public ApplicationsPage()
    {
        InitializeComponent();
        GetItems();
        GetOffice();
        GetDevelopment();
        GetMusic();
        GetMessaging();
        GetLaunchers();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance.MarkVisited(nameof(ApplicationsPage));
        MainWindow.Instance.CheckAllPagesVisited();
    }

    private void GetItems()
    {
        Office.ItemsSource = new List<GridViewItem>
        {
            new() { Text = "Word", ImageSource = "ms-appx:///Assets/Fluent/Word.png" },
            new() { Text = "Excel", ImageSource = "ms-appx:///Assets/Fluent/Excel.png" },
            new() { Text = "PowerPoint", ImageSource = "ms-appx:///Assets/Fluent/Powerpoint.png" },
            new() { Text = "OneNote", ImageSource = "ms-appx:///Assets/Fluent/OneNote.png" },
            new() { Text = "Teams", ImageSource = "ms-appx:///Assets/Fluent/Teams.png" },
            new() { Text = "Outlook", ImageSource = "ms-appx:///Assets/Fluent/Outlook.png" },
            new() { Text = "OneDrive", ImageSource = "ms-appx:///Assets/Fluent/OneDrive.png" }
        };

        Development.ItemsSource = new List<GridViewItem>
        {
            new() { Text = "Visual Studio", ImageSource = "ms-appx:///Assets/Fluent/VisualStudio.png" },
            new() { Text = "Visual Studio Code", ImageSource = "ms-appx:///Assets/Fluent/VisualStudioCode.png" },
            new() { Text = "Git", ImageSource = "ms-appx:///Assets/Fluent/Git.png" },
            new() { Text = "Python", ImageSource = "ms-appx:///Assets/Fluent/Python.png" },
            new() { Text = "Node.js", ImageSource = "ms-appx:///Assets/Fluent/Nodejs.png" },
            new() { Text = "Trello", ImageSource = "ms-appx:///Assets/Fluent/Trello.png" }
        };

        Music.ItemsSource = new List<GridViewItem>
        {
            new() { Text = "Apple Music", ImageSource = "ms-appx:///Assets/Fluent/AppleMusic.png" },
            new() { Text = "TIDAL", ImageSource = "ms-appx:///Assets/Fluent/Tidal.png" },
            new() { Text = "Qobuz", ImageSource = "ms-appx:///Assets/Fluent/Qobuz.png" },
            new() { Text = "Amazon Music", ImageSource = "ms-appx:///Assets/Fluent/AmazonMusic.png" },
            new() { Text = "Deezer Music", ImageSource = "ms-appx:///Assets/Fluent/DeezerMusic.png" },
            new() { Text = "Spotify", ImageSource = "ms-appx:///Assets/Fluent/Spotify.png" }
        };

        Messaging.ItemsSource = new List<GridViewItem>
        {
            new() { Text = "Discord", ImageSource = "ms-appx:///Assets/Fluent/Discord.png" },
            new() { Text = "WhatsApp", ImageSource = "ms-appx:///Assets/Fluent/Whatsapp.png" }
        };

        Launchers.ItemsSource = new List<GridViewItem>
        {
            new() { Text = "Epic Games", ImageSource = "ms-appx:///Assets/Fluent/EpicGames.png" },
            new() { Text = "Steam", ImageSource = "ms-appx:///Assets/Fluent/Steam.png" },
            new() { Text = "Riot Client", ImageSource = "ms-appx:///Assets/Fluent/RiotClient.png" },
            new() { Text = "EA", ImageSource = "ms-appx:///Assets/Fluent/EA.png" },
            new() { Text = "Ubisoft Connect", ImageSource = "ms-appx:///Assets/Fluent/UbisoftConnect.png" },
            new() { Text = "Battle.Net", ImageSource = "ms-appx:///Assets/Fluent/BattleNet.png" },
            new() { Text = "Minecraft Launcher", ImageSource = "ms-appx:///Assets/Fluent/MinecraftLauncher.png" },
            new() { Text = "Rockstar Games Launcher", ImageSource = "ms-appx:///Assets/Fluent/RockstarGamesLauncher.png" },
            new() { Text = "FiveM", ImageSource = "ms-appx:///Assets/Fluent/FiveM.jpg" },
            new() { Text = "FACEIT", ImageSource = "ms-appx:///Assets/Fluent/FACEIT.png" }
        };
    }

    private void GetOffice()
    {
        var selectedOffice = localSettings.Values["Office"] as string;
        var oficeItems = Office.ItemsSource as List<GridViewItem>;
        Office.SelectedItems.AddRange(
            selectedOffice?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => oficeItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
        );

        isInitializingOfficeState = false;
    }

    private void GetDevelopment()
    {
        var selectedDevelopment = localSettings.Values["Development"] as string;
        var developmentItems = Development.ItemsSource as List<GridViewItem>;
        Development.SelectedItems.AddRange(
            selectedDevelopment?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => developmentItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
        );

        isInitializingDevelopmentState = false;
    }

    private void GetMusic()
    {
        var selectedMusic = localSettings.Values["Music"] as string;
        var musicItems = Music.ItemsSource as List<GridViewItem>;
        Music.SelectedItems.AddRange(
            selectedMusic?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => musicItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
        );

        isInitializingMusicState = false;
    }

    private void GetMessaging()
    {
        var selectedMessaging = localSettings.Values["Messaging"] as string;
        var messagingItems = Messaging.ItemsSource as List<GridViewItem>;
        Messaging.SelectedItems.AddRange(
            selectedMessaging?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => messagingItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
        );

        isInitializingMessagingState = false;
    }

    private void GetLaunchers()
    {
        var selectedLaunchers = localSettings.Values["Launchers"] as string;
        var launcherItems = Launchers.ItemsSource as List<GridViewItem>;
        Launchers.SelectedItems.AddRange(
            selectedLaunchers?.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => launcherItems?.FirstOrDefault(ext => ext.Text == e))
            .Where(ext => ext != null) ?? Enumerable.Empty<GridViewItem>()
        );

        isInitializingLaunchersState = false;
    }

    private void Office_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingOfficeState) return;

        var selectedOffice = Office.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["Office"] = string.Join(", ", selectedOffice);
    }

    private void Development_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingDevelopmentState) return;

        var selectedDevelopment = Development.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["Development"] = string.Join(", ", selectedDevelopment);
    }

    private void Music_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingMusicState) return;

        var selectedMusic = Music.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["Music"] = string.Join(", ", selectedMusic);
    }

    private void Messaging_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingMessagingState) return;

        var selectedMessaging = Messaging.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["Messaging"] = string.Join(", ", selectedMessaging);
    }

    private void Launchers_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingLaunchersState) return;

        var selectedLaunchers = Launchers.SelectedItems
            .Cast<GridViewItem>()
            .Select(item => item.Text)
            .ToArray();

        localSettings.Values["Launchers"] = string.Join(", ", selectedLaunchers);
    }
}

[GeneratedBindableCustomProperty]
public partial class GridViewItem
{
    public string Text { get; set; }
    public string ImageSource { get; set; }
}
