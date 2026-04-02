using AutoOS.Helpers.Games;
using AutoOS.Helpers.Processes;
using AutoOS.Helpers.Services;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using ValveKeyValue;
using Windows.Foundation;
using Windows.Storage;
using WinRT;

namespace AutoOS.Views.Settings.Games;

[TemplatePart(Name = nameof(PART_BackDropImage), Type = typeof(AnimatedImage))]
[TemplatePart(Name = nameof(PART_ScrollViewer), Type = typeof(ScrollViewer))]
[TemplatePart(Name = nameof(PART_ItemsRepeater), Type = typeof(ItemsRepeater))]

public partial class HeaderCarousel : ItemsControl
{
    private const string PART_ScrollViewer = "PART_ScrollViewer";
    private const string PART_BackDropImage = "PART_BackDropImage";
    private const string PART_ItemsRepeater = "PART_ItemsRepeater";
    private ScrollViewer scrollViewer;
    private AnimatedImage backDropImage;
    private ItemsRepeater itemsRepeater;

    private TextBlock PageTitle;
    private SwitchPresenter SwitchPresenter;
    //private TextBlock SwitchPresenter_TextBlock;

    private StackPanel NoGames_StackPanel;

    private Grid MetadataGrid;
    private ScrollViewer Metadata_ScrollViewer;

    private Card Screenshots_Card;
    //private GameGallery Screenshots_Gallery;
    //private ScrollViewer Videos_ScrollViewer;

    private Button Play;
    private Button Update;
    private Button StopProcesses;
    private Button RestartProcesses;

    private bool isInitializingEpicGamesAccounts = true;
    private bool isInitializingSteamAccounts = true;
    private Button EpicGamesButton;
    private ComboBox EpicGamesAccounts;
    private Button AddEpicGamesAccount;
    private Button RemoveEpicGamesAccount;

    private Button SteamButton;
    private ComboBox SteamAccounts;
    private Button AddSteamAccount;
    private Button RemoveSteamAccount;

    private StackPanel EpicGrowl;
    private StackPanel SteamGrowl;

    private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    private TextBlock AgeRatingDescriptionText;
    private TextBlock ElementsText;

    private AutoSuggestBox SearchBox;
    private string currentSortKey = "Title";
    private bool ascending = true;

    private RadioMenuFlyoutItem SortByName, SortByLauncher, SortByRating, SortByTimePlayed, SortByRecentlyPlayed;
    private RadioMenuFlyoutItem SortAscending, SortDescending;

    //public event EventHandler<HeaderCarouselEventArgs> ItemClick;

    private readonly Random random = new();
    private readonly DispatcherTimer selectionTimer = new();
    private readonly DispatcherTimer deselectionTimer = new();
    private readonly List<int> numbers = [];
    private HeaderCarouselItem selectedTile;
    private int currentIndex;

    private BlurEffectManager _blurManager;

    public ObservableCollection<InfoItem> InfoItems { get; } = new ObservableCollection<InfoItem>();

    public HeaderCarousel()
    {
        DefaultStyleKey = typeof(HeaderCarousel);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        selectionTimer.Interval = SelectionDuration;
        deselectionTimer.Interval = DeSelectionDuration;

        scrollViewer = GetTemplateChild(PART_ScrollViewer) as ScrollViewer;
        backDropImage = GetTemplateChild(PART_BackDropImage) as AnimatedImage;
        itemsRepeater = GetTemplateChild("PART_ItemsRepeater") as ItemsRepeater;
        itemsRepeater.ItemsSource = InfoItems;
        itemsRepeater.ElementPrepared += ItemsRepeater_ElementPrepared;

        PageTitle = GetTemplateChild("PageTitle") as TextBlock;

        SearchBox = GetTemplateChild("SearchBox") as AutoSuggestBox;
        SearchBox.TextChanged += SearchBox_TextChanged;
        SearchBox.QuerySubmitted += SearchBox_QuerySubmitted;

        SortByName = GetTemplateChild("SortByName") as RadioMenuFlyoutItem;
        SortByLauncher = GetTemplateChild("SortByLauncher") as RadioMenuFlyoutItem;
        SortByRating = GetTemplateChild("SortByRating") as RadioMenuFlyoutItem;
        SortByTimePlayed = GetTemplateChild("SortByTimePlayed") as RadioMenuFlyoutItem;
        SortByRecentlyPlayed = GetTemplateChild("SortByRecentlyPlayed") as RadioMenuFlyoutItem;
        SortAscending = GetTemplateChild("SortAscending") as RadioMenuFlyoutItem;
        SortDescending = GetTemplateChild("SortDescending") as RadioMenuFlyoutItem;

        SortByName.Click += SortKey_Click;
        SortByLauncher.Click += SortKey_Click;
        SortByRating.Click += SortKey_Click;
        SortByTimePlayed.Click += SortKey_Click;
        SortByRecentlyPlayed.Click += SortKey_Click;
        SortAscending.Click += SortOrder_Click;
        SortDescending.Click += SortOrder_Click;

        EpicGamesButton = GetTemplateChild("EpicGamesButton") as Button;
        EpicGamesAccounts = GetTemplateChild("EpicGamesAccounts") as ComboBox;
        EpicGamesAccounts.SelectionChanged += EpicGamesAccounts_SelectionChanged;
        AddEpicGamesAccount = GetTemplateChild("AddEpicGamesAccount") as Button;
        AddEpicGamesAccount.Click += AddEpicGamesAccount_Click;
        RemoveEpicGamesAccount = GetTemplateChild("RemoveEpicGamesAccount") as Button;
        RemoveEpicGamesAccount.Click += RemoveEpicGamesAccount_Click;
        EpicGrowl = GetTemplateChild("EpicGrowl") as StackPanel;
        Growl.Register("Epic", EpicGrowl);
        LoadEpicGamesAccounts();

        SteamButton = GetTemplateChild("SteamButton") as Button;
        SteamAccounts = GetTemplateChild("SteamAccounts") as ComboBox;
        SteamAccounts.SelectionChanged += SteamAccounts_SelectionChanged;
        AddSteamAccount = GetTemplateChild("AddSteamAccount") as Button;
        AddSteamAccount.Click += AddSteamAccount_Click;
        RemoveSteamAccount = GetTemplateChild("RemoveSteamAccount") as Button;
        RemoveSteamAccount.Click += RemoveSteamAccount_Click;
        SteamGrowl = GetTemplateChild("SteamGrowl") as StackPanel;
        Growl.Register("Steam", SteamGrowl);
        LoadSteamAccounts();

        SwitchPresenter = GetTemplateChild("SwitchPresenter") as SwitchPresenter;
        //SwitchPresenter_TextBlock = GetTemplateChild("SwitchPresenter_TextBlock") as TextBlock;
        NoGames_StackPanel = GetTemplateChild("NoGames_StackPanel") as StackPanel;
        MetadataGrid = GetTemplateChild("MetadataGrid") as Grid;
        Metadata_ScrollViewer = GetTemplateChild("Metadata_ScrollViewer") as ScrollViewer;

        Play = GetTemplateChild("Play") as Button;
        Play.Click += Play_Click;
        Update = GetTemplateChild("Update") as Button;
        Update.Click += Update_Click;
        StopProcesses = GetTemplateChild("StopProcesses") as Button;
        StopProcesses.Click += StopProcesses_Click;
        RestartProcesses = GetTemplateChild("RestartProcesses") as Button;
        RestartProcesses.Click += RestartProcesses_Click;

        AgeRatingDescriptionText = GetTemplateChild("AgeRatingDescriptionText") as TextBlock;
        ElementsText = GetTemplateChild("ElementsText") as TextBlock;

        Screenshots_Card = GetTemplateChild("Screenshots_Card") as Card;
        //Screenshots_Gallery = GetTemplateChild("Screenshots_Gallery") as GameGallery;
        //Videos_ScrollViewer = GetTemplateChild("Videos_ScrollViewer") as ScrollViewer;

        Loaded -= HeaderCarousel_Loaded;
        Loaded += HeaderCarousel_Loaded;
        Unloaded -= HeaderCarousel_Unloaded;
        Unloaded += HeaderCarousel_Unloaded;

        LoadGames();

        if (backDropImage != null)
        {
            _blurManager = new BlurEffectManager(backDropImage);

            ApplyBackdropBlur();
        }
    }

    private async void LoadGames()
    {
        // reset
        LoadSortSettings();

        // load games
        var tasks = new List<Task>();

        if (EpicGamesAccounts.SelectedItem is ComboBoxItem item && item.Content?.ToString() != "Not logged in" && EpicGamesButton.Visibility == Visibility.Visible)
        {
            tasks.Add(EpicGamesHelper.LoadGames());
        }

        if ((SteamAccounts.SelectedItem is string && SteamAccounts.SelectedItem.ToString() != "Not logged in") && SteamButton.Visibility == Visibility.Visible)
        {
            tasks.Add(SteamHelper.LoadGames());
        }

        switch (localSettings.Values["SwitchEmulator"] as string ?? "Eden")
        {
            case "Eden":
                tasks.Add(EdenHelper.LoadGames());
                break;

            case "Citron":
                tasks.Add(CitronHelper.LoadGames());
                break;

            case "Ryujinx":
                tasks.Add(RyujinxHelper.LoadGames());
                break;
        }

        //tasks.Add(UbisoftConnectHelper.LoadGames());

        await Task.WhenAll(tasks);

        // sort games
        LoadSortSettings();

        // show games status
        NoGames_StackPanel.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // show games
        SwitchPresenter.Value = false;

        await Task.Delay(700);

        if (Items.Count == 0)
            return;

        if (Items[0] is HeaderCarouselItem tile)
        {
            selectedTile?.IsSelected = false;
            selectedTile = null;

            selectedTile = tile;
            var panel = ItemsPanelRoot;
            if (panel != null)
            {
                GeneralTransform transform = selectedTile.TransformToVisual(panel);
                Point point = transform.TransformPoint(new Point(0, 0));
                scrollViewer.ChangeView(point.X - (scrollViewer.ActualWidth / 2) + (selectedTile.ActualSize.X / 2), null, null);
                SetTileVisuals();
                PageTitle.RequestedTheme = ElementTheme.Dark;
            }
        }

        if (Items.Count > 1)
            selectionTimer.Start();
    }

    private void ApplyBackdropBlur()
    {
        if (_blurManager == null)
            return;

        if (IsBlurEnabled)
        {
            _blurManager.BlurAmount = BlurAmount;

            _blurManager.EnableBlur();
        }
        else
        {
            _blurManager.DisableBlur();
        }
    }

    private void HeaderCarousel_Unloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeToEvents();

        ElementSoundPlayer.State = ElementSoundPlayerState.Off;

        if (!string.IsNullOrEmpty(ArtifactId) && epicGameStartTimes.TryGetValue(ArtifactId, out var startTime))
        {
            EpicGamesHelper.AddPlaytime(ArtifactId, startTime);
            epicGameStartTimes.Remove(ArtifactId);
        }
    }

    private void HeaderCarousel_Loaded(object sender, RoutedEventArgs e)
    {
        if (IsAutoScrollEnabled)
            selectionTimer.Tick += SelectionTimer_Tick;

        ElementSoundPlayer.State = ElementSoundPlayerState.On;
    }

    protected override void OnItemsChanged(object e)
    {
        base.OnItemsChanged(e);
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        foreach (HeaderCarouselItem tile in Items.Cast<HeaderCarouselItem>())
        {
            tile.PointerEntered -= Tile_PointerEntered;
            tile.PointerEntered += Tile_PointerEntered;

            tile.GotFocus -= Tile_GotFocus;
            tile.GotFocus += Tile_GotFocus;

            //tile.Click -= Tile_Click;
            //tile.Click += Tile_Click;
        }
    }

    private void UnsubscribeToEvents()
    {
        selectionTimer.Tick -= SelectionTimer_Tick;
        selectionTimer?.Stop();
        gameWatcherTimer?.Stop();

        foreach (HeaderCarouselItem tile in Items.Cast<HeaderCarouselItem>())
        {
            tile.PointerEntered -= Tile_PointerEntered;
            tile.GotFocus -= Tile_GotFocus;
            //tile.Click -= Tile_Click;
        }
    }

    //private void Tile_Click(object sender, RoutedEventArgs e)
    //{
    //    if (sender is HeaderCarouselItem tile)
    //    {
    //        tile.PointerExited -= Tile_PointerExited;
    //        ItemClick?.Invoke(sender, new HeaderCarouselEventArgs { HeaderCarouselItem = tile });
    //    }
    //}

    private void SelectionTimer_Tick(object sender, object e)
    {
        SelectNextTile();
    }

    private async void SelectNextTile()
    {
        if (Items.Count == 0)
        {
            return;
        }

        if (Items[GetNextUniqueRandom()] is HeaderCarouselItem tile)
        {
            if (selectedTile != null)
            {
                selectedTile.IsSelected = false;
                selectedTile = null;
            }

            selectedTile = tile;
            var panel = ItemsPanelRoot;
            if (panel != null)
            {
                GeneralTransform transform = selectedTile.TransformToVisual(panel);
                Point point = transform.TransformPoint(new Point(0, 0));
                scrollViewer.ChangeView(point.X - (scrollViewer.ActualWidth / 2) + (selectedTile.ActualSize.X / 2), null, null);
                await Task.Delay(500);
                SetTileVisuals();
            }
        }
    }

    private void ResetAndShuffle()
    {
        if (Items.Count == 0)
        {
            return;
        }

        numbers.Clear();
        for (int i = 0; i <= Items.Count - 1; i++)
        {
            numbers.Add(i);
        }

        // Shuffle the list
        for (int i = numbers.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (numbers[j], numbers[i]) = (numbers[i], numbers[j]);
        }

        currentIndex = 0;
    }

    private int GetNextUniqueRandom()
    {
        if (currentIndex >= numbers.Count)
        {
            ResetAndShuffle();
        }

        int nextIndex = numbers[currentIndex++];

        if (selectedTile != null)
        {
            int selectedIndex = Items.IndexOf(selectedTile);
            if (nextIndex == selectedIndex)
            {
                if (currentIndex >= numbers.Count)
                    ResetAndShuffle();
                nextIndex = numbers[currentIndex++];
            }
        }

        return nextIndex;
    }

    private void SetTileVisuals()
    {
        if (selectedTile != null)
        {
            selectedTile.IsSelected = true;

            ElementSoundPlayer.Play(ElementSoundKind.Focus);

            if (selectedTile.BackgroundImageUrl != null && backDropImage.ImageUrl?.ToString() != selectedTile.BackgroundImageUrl)
                backDropImage.ImageUrl = new Uri(selectedTile.BackgroundImageUrl);

            if (MetadataGrid.Visibility == Visibility.Collapsed)
                MetadataGrid.Visibility = Visibility.Visible;

            //Metadata_ScrollViewer.Focus(FocusState.Programmatic);
            Metadata_ScrollViewer.ChangeView(null, 0, null);

            Title = selectedTile?.Title;
            Developers = selectedTile?.Developers;

            UpdateIsAvailable = selectedTile?.UpdateIsAvailable ?? false;

            Rating = selectedTile?.Rating != 0.0 ? selectedTile.Rating : Rating;
            RoundedRating = Math.Round((selectedTile?.Rating ?? 0.0), 1).ToString("0.0", CultureInfo.InvariantCulture);
            PlayTime = selectedTile?.PlayTime;
            AgeRatingUrl = selectedTile?.AgeRatingUrl;
            AgeRatingTitle = selectedTile?.AgeRatingTitle;
            AgeRatingDescription = selectedTile?.AgeRatingDescription;
            AgeRatingDescriptionText.Visibility = string.IsNullOrEmpty(AgeRatingDescription)
                ? Visibility.Collapsed
                : Visibility.Visible;

            Elements = selectedTile?.Elements;
            ElementsText.Visibility = string.IsNullOrEmpty(Elements)
                ? Visibility.Collapsed
                : Visibility.Visible;

            Genres = selectedTile?.Genres;
            Features = selectedTile?.Features;
            Description = selectedTile?.Description;

            InstallLocation = selectedTile?.InstallLocation;

            Launcher = selectedTile?.Launcher;

            CatalogItemId = selectedTile?.CatalogItemId;
            CatalogNamespace = selectedTile?.CatalogNamespace;
            AppName = selectedTile?.AppName;
            LaunchExecutable = selectedTile?.LaunchExecutable;
            LaunchCommand = selectedTile?.LaunchCommand;
            ProcessNames = selectedTile?.ProcessNames;
            ArtifactId = selectedTile?.ArtifactId;

            GameID = selectedTile?.GameID;

            LauncherLocation = selectedTile?.LauncherLocation;
            DataLocation = selectedTile?.DataLocation;
            GameLocation = selectedTile?.GameLocation;

            ReleaseDate = selectedTile?.ReleaseDate;
            Size = selectedTile?.Size;
            Version = selectedTile?.Version;

            InfoItems.Clear();

            InfoItems.Add(new InfoItem
            {
                Label = "Published by",
                Value = Developers,
                PathData = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), Application.Current.Resources["PackageIconPath"]),
                IconType = InfoIconType.PathIcon
            });

            InfoItems.Add(new InfoItem
            {
                Label = "Release date",
                Value = ReleaseDate,
                Glyph = "\uE787",
                IconType = InfoIconType.FontIcon
            });

            InfoItems.Add(new InfoItem
            {
                Label = "Approximate size",
                Value = Size,
                Glyph = "\uECAA",
                IconType = InfoIconType.FontIcon
            });

            if (!string.IsNullOrEmpty(Version))
            {
                InfoItems.Add(new InfoItem
                {
                    Label = "Installed version",
                    Value = Version,
                    PathData = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), Application.Current.Resources["VersionIconPath"]),
                    IconType = InfoIconType.PathIcon
                });
            }

            InfoItems.Add(new InfoItem
            {
                Label = "Install location",
                IsHyperlink = true,
                Hyperlink = InstallLocation,
                Glyph = "\uE8B7",
                IconType = InfoIconType.FontIcon
            });

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
            {
                CheckGameRunning();

                await Task.Delay(100);
                Screenshots = selectedTile?.Screenshots;
                Screenshots_Card.Visibility = (Screenshots?.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
                //Screenshots_Gallery.ResetScrollPosition();

                //Videos = selectedTile?.Videos;
                //Videos_ScrollViewer.Visibility = (Videos?.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            });
        }
    }

    private void Tile_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var tile = (HeaderCarouselItem)sender;
        if (tile != selectedTile)
        {
            selectedTile = tile;
            SelectTile();
        }
        else
        {
            selectionTimer?.Stop();
        }
    }

    private void SelectTile()
    {
        selectionTimer.Stop();

        foreach (HeaderCarouselItem t in Items.Cast<HeaderCarouselItem>())
        {
            t.IsSelected = false;
        }

        SetTileVisuals();
    }

    private void Tile_GotFocus(object sender, RoutedEventArgs e)
    {
        selectedTile = (HeaderCarouselItem)sender;
        SelectTile();
    }

    private void ApplyAutoScroll()
    {
        if (IsAutoScrollEnabled)
        {
            ResetAndShuffle();
            SelectNextTile();
        }
        else
        {
            selectionTimer?.Stop();
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suggestions = Items
                .OfType<HeaderCarouselItem>()
                .Where(g => !string.IsNullOrEmpty(g.Title) && g.Title.Contains(sender.Text, StringComparison.CurrentCultureIgnoreCase))
                .Select(g => g.Title)
                .Distinct()
                .ToList();

            sender.ItemsSource = suggestions;
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(query))
            return;

        SelectTileByTitle(query);
    }

    private async void SelectTileByTitle(string title)
    {
        if (string.IsNullOrEmpty(title) || Items.Count == 0)
            return;

        var tile = Items
            .OfType<HeaderCarouselItem>()
            .FirstOrDefault(t => string.Equals(t.Title, title, StringComparison.CurrentCultureIgnoreCase));

        if (tile == null)
            return;

        if (selectedTile != null && selectedTile != tile)
        {
            selectedTile.IsSelected = false;
            selectedTile = null;
        }

        selectedTile = tile;

        UnsubscribeToEvents();

        var panel = ItemsPanelRoot;
        if (panel != null)
        {
            GeneralTransform transform = selectedTile.TransformToVisual(panel);
            Point point = transform.TransformPoint(new Point(0, 0));
            scrollViewer.ChangeView(point.X - (scrollViewer.ActualWidth / 2) + (selectedTile.ActualSize.X / 2), null, null);
            SetTileVisuals();
        }

        await Task.Delay(500);

        SubscribeToEvents();
    }

    private void SortKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item)
        {
            currentSortKey = item.Name switch
            {
                "SortByName" => "Title",
                "SortByLauncher" => "Launcher",
                "SortByRating" => "Rating",
                "SortByTimePlayed" => "Time Played",
                "SortByRecentlyPlayed" => "Recently Played",
                _ => currentSortKey
            };

            ApplySort();
            SaveSortSettings();
        }
    }

    private void SortOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item)
        {
            ascending = item.Name == "SortAscending";
            ApplySort();
            SaveSortSettings();
        }
    }

    private async void ApplySort()
    {
        var items = Items.OfType<HeaderCarouselItem>().ToList();
        if (items.Count == 0) return;

        IEnumerable<HeaderCarouselItem> result = currentSortKey switch
        {
            "Title" => ascending
                ? items.OrderBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : items.OrderByDescending(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
            "Launcher" => ascending
                ? items.OrderBy(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
                      .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : items.OrderByDescending(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
                      .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
            "Rating" => ascending
                ? items.OrderBy(g => g.Rating)
                .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : items.OrderByDescending(g => g.Rating)
                .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
            "Time Played" => ascending
                ? items.OrderBy(g => ParseMinutes(g.PlayTime))
                      .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : items.OrderByDescending(g => ParseMinutes(g.PlayTime))
                      .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
            "Recently Played" => ascending
                ? items.OrderBy(g =>
                    localSettings.Values.TryGetValue($"LastPlayed_{g.Title}", out var val) && val is long ts ? ts : 0)
                       .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
                : items.OrderByDescending(g =>
                    localSettings.Values.TryGetValue($"LastPlayed_{g.Title}", out var val) && val is long ts ? ts : 0)
                       .ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
            _ => items
        };

        Items.Clear();
        foreach (var item in result)
        {
            Items.Add(item);
        }

        await Task.Delay(100);
        SelectTileByTitle(Title);
    }

    private void SaveSortSettings()
    {
        ApplicationData.Current.LocalSettings.Values["SortKey"] = currentSortKey;
        ApplicationData.Current.LocalSettings.Values["SortAscending"] = ascending;
    }

    private void LoadSortSettings()
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        currentSortKey = settings["SortKey"] as string ?? "Time Played";

        ascending = settings["SortAscending"] as bool? ?? false;

        SortByName.IsChecked = currentSortKey == "Title";
        SortByLauncher.IsChecked = currentSortKey == "Launcher";
        SortByRating.IsChecked = currentSortKey == "Rating";
        SortByTimePlayed.IsChecked = currentSortKey == "Time Played";
        SortByRecentlyPlayed.IsChecked = currentSortKey == "Recently Played";

        SortAscending.IsChecked = ascending;
        SortDescending.IsChecked = !ascending;

        ApplySort();
    }

    [GeneratedRegex(@"(?:(\d+)h)?\s*(\d+)m", RegexOptions.Compiled)]
    private static partial Regex PlayTimeMinutesRegex();
    private static int ParseMinutes(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return 0;

        var match = PlayTimeMinutesRegex().Match(time);
        if (match.Success)
        {
            int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int minutes = int.Parse(match.Groups[2].Value);
            return hours * 60 + minutes;
        }

        return 0;
    }

    public void LoadEpicGamesAccounts()
    {
        if (File.Exists(EpicGamesHelper.EpicGamesPath))
        {
            // get all accounts
            var accounts = EpicGamesHelper.GetEpicGamesAccounts();

            // reset ui elements
            EpicGamesAccounts.Items.Clear();
            EpicGamesAccounts.IsEnabled = accounts.Count > 0;
            RemoveEpicGamesAccount.IsEnabled = accounts.Count > 0;

            // add accounts to combobox
            if (accounts.Count == 0)
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                EpicGamesAccounts.Items.Add(notLoggedIn);
                EpicGamesAccounts.SelectedItem = notLoggedIn;
                EpicGamesAccounts.IsEnabled = false;
                RemoveEpicGamesAccount.IsEnabled = false;
            }
            else if (!accounts.Any(a => a.IsActive))
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                EpicGamesAccounts.Items.Add(notLoggedIn);
                EpicGamesAccounts.SelectedItem = notLoggedIn;
                RemoveEpicGamesAccount.IsEnabled = false;

                foreach (var account in accounts)
                {
                    var item = new ComboBoxItem
                    {
                        Content = account.DisplayName,
                        Tag = account.AccountId
                    };

                    EpicGamesAccounts.Items.Add(item);
                }
            }
            else
            {
                foreach (var account in accounts)
                {
                    var item = new ComboBoxItem
                    {
                        Content = account.DisplayName,
                        Tag = account.AccountId
                    };

                    EpicGamesAccounts.Items.Add(item);

                    if (account.IsActive)
                        EpicGamesAccounts.SelectedItem = item;
                }
            }
        }
        else
        {
            EpicGamesButton.Visibility = Visibility.Collapsed;
        }

        isInitializingEpicGamesAccounts = false;
    }

    private async void EpicGamesAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingEpicGamesAccounts) return;

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // update config before switching
        if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
        {
            var (oldAccountId, _, _, _) = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath);

            string accountDir = Path.Combine(EpicGamesHelper.EpicGamesAccountDir, oldAccountId);
            if (Directory.Exists(accountDir))
                File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(accountDir, "GameUserSettings.ini"), true);
        }

        // get accountId
        string accountId = (EpicGamesAccounts.SelectedItem as ComboBoxItem)?.Tag as string;

        // replace file
        File.Copy(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "GameUserSettings.ini"), EpicGamesHelper.ActiveEpicGamesAccountPath, true);

        // replace accountid
        Process.Start("regedit.exe", $@"/s ""{Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "accountId.reg")}""");

        // update refresh token
        if (await EpicGamesHelper.UpdateEpicGamesToken(EpicGamesHelper.ActiveEpicGamesAccountPath) == null)
        {
            UpdateInvalidEpicGamesToken();
            return;
        }

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // refresh combobox
        isInitializingEpicGamesAccounts = true;
        LoadEpicGamesAccounts();

        // refresh library
        await EpicGamesHelper.LoadGames();
        LoadSortSettings();
    }

    private async void UpdateInvalidEpicGamesToken()
    {
        // add growl
        Growl.Info(new GrowlInfo
        {
            ShowDateTime = false,
            StaysOpen = true,
            IsClosable = false,
            UseBlueColorForInfo = true,
            Title = "The refresh token is no longer valid. Please enter your password again...",
            Token = "Epic"
        });

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // delay
        await Task.Delay(500);

        // launch epic games launcher
        Process.Start(EpicGamesHelper.EpicGamesPath);

        // delay
        await Task.Delay(2000);

        // check when logged in
        while (true)
        {
            if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
                {
                    break;
                }
            }

            await Task.Delay(500);
        }

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // disable tray and notifications
        EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
        EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

        // clear
        Growl.Clear("Epic");

        // add growl
        Growl.Success(new GrowlInfo
        {
            ShowDateTime = false,
            StaysOpen = false,
            IsClosable = false,
            Title = $"Successfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}.",
            Token = "Epic"
        });

        // refresh combobox
        isInitializingEpicGamesAccounts = true;
        LoadEpicGamesAccounts();

        // refresh library
        await EpicGamesHelper.LoadGames();
        LoadSortSettings();
    }

    private async void AddEpicGamesAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add Epic Games Account",
            Content = "Are you sure that you want to add an Epic Games account?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result = await contentDialog.ShowAsync();

        // check result
        if (result == ContentDialogResult.Primary)
        {
            // add growl
            Growl.Info(new GrowlInfo
            {
                ShowDateTime = false,
                StaysOpen = true,
                IsClosable = false,
                UseBlueColorForInfo = true,
                Title = "Please log in to your Epic Games account...",
                Token = "Epic"
            });

            // close epic games launcher
            EpicGamesHelper.CloseEpicGames();

            // delete gameusersettings.ini
            if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                File.Delete(EpicGamesHelper.ActiveEpicGamesAccountPath);
            }

            // delay
            await Task.Delay(500);

            // launch epic games launcher
            Process.Start(EpicGamesHelper.EpicGamesPath);

            // check when logged in
            while (true)
            {
                if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
                {
                    if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
                    {
                        break;
                    }
                }

                await Task.Delay(500);
            }

            // close epic games launcher
            EpicGamesHelper.CloseEpicGames();

            // disable tray and notifications
            EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
            EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

            // clear
            Growl.Clear("Epic");

            // add growl
            Growl.Success(new GrowlInfo
            {
                ShowDateTime = false,
                StaysOpen = false,
                IsClosable = false,
                Title = $"Successfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}.",
                Token = "Epic"
            });

            // refresh combobox
            isInitializingEpicGamesAccounts = true;
            LoadEpicGamesAccounts();

            // refresh library
            await EpicGamesHelper.LoadGames();
            LoadSortSettings();
        }
    }

    private async void RemoveEpicGamesAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Remove Epic Games Account",
            Content = $"Are you sure that you want to remove {(EpicGamesAccounts.SelectedItem as ComboBoxItem).Content}?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        ContentDialogResult result = await contentDialog.ShowAsync();

        // check results
        if (result == ContentDialogResult.Primary)
        {
            // close epic games launcher
            EpicGamesHelper.CloseEpicGames();

            // get accountId
            string accountId = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).AccountId;

            // remove account
            File.Delete(EpicGamesHelper.ActiveEpicGamesAccountPath);
            Directory.Delete(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId), true);

            // add growl
            Growl.Success(new GrowlInfo
            {
                ShowDateTime = false,
                StaysOpen = false,
                IsClosable = false,
                Title = $"Successfully removed {(EpicGamesAccounts.SelectedItem as ComboBoxItem).Content}.",
                Token = "Epic",
            });

            // refresh combobox
            isInitializingEpicGamesAccounts = true;
            LoadEpicGamesAccounts();

            // remove all epic games titles
            foreach (var item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Epic Games").ToList())
                Items.Remove(item);
        }
    }

    public void LoadSteamAccounts()
    {
        if (File.Exists(SteamHelper.SteamPath))
        {
            // get all accounts
            var accounts = SteamHelper.GetSteamAccounts();

            // reset ui elements
            SteamAccounts.Items.Clear();
            SteamAccounts.IsEnabled = true;
            RemoveSteamAccount.IsEnabled = true;

            // add accounts to combobox
            if (accounts.Count == 0)
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                SteamAccounts.Items.Add(notLoggedIn);
                SteamAccounts.SelectedItem = notLoggedIn;
                SteamAccounts.IsEnabled = false;
                RemoveSteamAccount.IsEnabled = false;
            }
            else if (accounts.All(a => !a.MostRecent) || accounts.All(a => !a.AllowAutoLogin))
            {
                var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
                SteamAccounts.Items.Add(notLoggedIn);
                SteamAccounts.SelectedItem = notLoggedIn;
                RemoveSteamAccount.IsEnabled = false;

                foreach (var account in accounts)
                {
                    SteamAccounts.Items.Add(account.AccountName);
                }
            }
            else
            {
                foreach (var account in accounts)
                {
                    SteamAccounts.Items.Add(account.AccountName);
                }

                int selectedIndex = accounts.FindIndex(a => a.MostRecent);
                if (selectedIndex < 0)
                    selectedIndex = accounts.FindIndex(a => a.AllowAutoLogin);

                SteamAccounts.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            }
        }
        else
        {
            SteamButton.Visibility = Visibility.Collapsed;
        }

        isInitializingSteamAccounts = false;
    }

    private async void SteamAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInitializingSteamAccounts) return;

        // close steam
        SteamHelper.CloseSteam();

        // read file
        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))));

        // make all accounts inactive
        foreach (var user in kv.Children)
        {
            if (user.Value["AccountName"]?.ToString() == SteamAccounts.SelectedItem.ToString())
            {
                user.Value["MostRecent"] = "1";
                user.Value["AllowAutoLogin"] = "1";
                user.Value["Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }
            else
            {
                user.Value["MostRecent"] = "0";
                user.Value["AllowAutoLogin"] = "0";
            }
        }

        // write changes
        using var msOut = new MemoryStream();
        KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
        msOut.Position = 0;
        File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

        // update registry key
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = @$"add ""HKEY_CURRENT_USER\Software\Valve\Steam"" /v AutoLoginUser /t REG_SZ /d {SteamAccounts.SelectedItem} /f",
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };
        process.Start();

        // refresh combobox
        isInitializingSteamAccounts = true;
        LoadSteamAccounts();

        // refresh library
        await SteamHelper.LoadGames();
        LoadSortSettings();
    }

    private async void AddSteamAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Add Steam Account",
            Content = "Are you sure that you want to add a Steam account?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result = await contentDialog.ShowAsync();

        // check result
        if (result == ContentDialogResult.Primary)
        {
            // add growl
            Growl.Info(new GrowlInfo
            {
                ShowDateTime = false,
                StaysOpen = true,
                IsClosable = false,
                UseBlueColorForInfo = true,
                Title = "Info",
                Token = "Steam",
                Message = "Please log in to your Steam account..."
            });

            // close steam
            SteamHelper.CloseSteam();

            // read file
            if (File.Exists(SteamHelper.SteamLoginUsersPath))
            {
                var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))));

                // make all accounts inactive
                foreach (var user in kv.Children)
                {
                    user.Value["MostRecent"] = "0";
                    user.Value["AllowAutoLogin"] = "0";
                }

                // write changes
                using var msOut = new MemoryStream();
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
                msOut.Position = 0;
                File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());
            }

            // delay
            await Task.Delay(500);

            // get initial user count
            int initialUserCount = File.Exists(SteamHelper.SteamLoginUsersPath) ? KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath)))).Children.Count() : 0;

            // launch steam
            Process.Start(SteamHelper.SteamPath);

            // check when logged in
            while (true)
            {
                if (File.Exists(SteamHelper.SteamLoginUsersPath))
                {
                    if (KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath)))).Children.Count() > initialUserCount)
                        break;
                }

                await Task.Delay(500);
            }

            // close steam
            SteamHelper.CloseSteam();

            // refresh combobox
            isInitializingSteamAccounts = true;
            LoadSteamAccounts();

            // refresh library
            await SteamHelper.LoadGames();
            LoadSortSettings();

            // clear
            Growl.Clear("Steam");

            // add growl
            Growl.Success(new GrowlInfo
            {
                ShowDateTime = false,
                StaysOpen = false,
                IsClosable = false,
                Title = $"Successfully logged in as {SteamAccounts.SelectedItem}",
                Token = "Steam"
            });
        }
    }

    private async void RemoveSteamAccount_Click(object sender, RoutedEventArgs e)
    {
        // add content dialog
        var contentDialog = new ContentDialog
        {
            Title = "Remove Steam Account",
            Content = $"Are you sure that you want to remove {SteamAccounts.SelectedItem}?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        ContentDialogResult result = await contentDialog.ShowAsync();

        // check results
        if (result == ContentDialogResult.Primary)
        {
            // close steam
            SteamHelper.CloseSteam();

            // read file
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))));

            // remove selected account
            var newChildren = kv.Children.Where(c => c.Key != kv.Children.First(child => child.Value["AccountName"]?.ToString() == SteamAccounts.SelectedItem.ToString()).Key).ToList();
            var newRoot = new KVObject();
            foreach (var child in newChildren)
            {
                newRoot[child.Key] = child.Value;
            }

            // write changes
            using var msOut = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, newRoot);
            msOut.Position = 0;
            File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

            // add growl
            Growl.Success(new GrowlInfo
            {
                ShowDateTime = false,
                StaysOpen = false,
                IsClosable = false,
                Title = $"Successfully removed {SteamAccounts.SelectedItem}.",
                Token = "Epic",
            });

            // refresh combobox
            isInitializingSteamAccounts = true;
            LoadSteamAccounts();

            // remove all steam titles
            foreach (var item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Steam").ToList())
                Items.Remove(item);
        }
    }


    private void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is FrameworkElement container)
        {
            var button = container.FindName("Link") as HyperlinkButton;
            if (button != null)
            {
                button.Click -= Link_Click;
                button.Click += Link_Click;
            }
        }
    }

    private async void Link_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton button && button.DataContext is InfoItem item)
        {
            if (!string.IsNullOrEmpty(item.Hyperlink))
            {
                await Windows.System.Launcher.LaunchFolderPathAsync(item.Hyperlink.Trim());
            }
        }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        selectionTimer?.Stop();

        var tile = selectedTile;
        if (tile == null)
            return;

        var launcher = tile.Launcher;
        var installLocation = tile.InstallLocation;
        var launchExecutable = tile.LaunchExecutable;
        var launchCommand = tile.LaunchCommand;
        var launcherLocation = tile.LauncherLocation;
        var gameLocation = tile.GameLocation;
        var dataLocation = tile.DataLocation;
        var gameId = tile.GameID;
        var appName = tile.AppName;
        var catalogNamespace = tile.CatalogNamespace;
        var catalogItemId = tile.CatalogItemId;
        var artifactId = tile.ArtifactId;

        if (launcher == "Epic Games")
        {
            string exchangeCode = await EpicGamesHelper.Exchange();
            var (accountId, displayName, _, _) = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(installLocation, launchExecutable),
                Arguments = string.Join(" ", new[]
                {
                launchCommand,
                "-AUTH_LOGIN=unused",
                $"-AUTH_PASSWORD={exchangeCode}",
                "-AUTH_TYPE=exchangeCode",
                $"-epicapp={appName}",
                "-epicenv=Prod",
                "-EpicPortal",
                $"-epicusername={displayName}",
                $"-epicuserid={accountId}",
                "-epiclocale=en",
                $"-epicsandboxid={catalogNamespace}"
            }),
                WorkingDirectory = Path.GetDirectoryName(Path.Combine(installLocation, launchExecutable)),
                UseShellExecute = false
            };

            Process.Start(startInfo);
        }
        else if (launcher == "Steam")
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\Steam\steam.exe",
                Arguments = $"-applaunch {gameId} -silent"
            });
        }
        else if (launcher == "Ubisoft Connect")
        {
            //Process.Start(new ProcessStartInfo($"uplay://launch/{gameId}") { UseShellExecute = true });

            //var startInfo = new ProcessStartInfo
            //{
            //    FileName = launcherLocation,
            //    Arguments = string.Join(" ", new[]
            //    {
            //        launchExecutable,
            //        "gamelauncher_wait_handle 1012",
            //        $"-upc_uplay_id {gameId}",
            //        "-upc_game_version 1",
            //        $"-upc_exe_path ",
            //        $"-upc_working_directory",
            //        $"-upc_arguments"
            //    }),
            //    WorkingDirectory = Path.GetDirectoryName(Path.Combine(installLocation, launchExecutable)),
            //    UseShellExecute = false
            //};

            //Process.Start(startInfo);
        }
        else if (launcher == "Eden")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = launcherLocation,
                Arguments = $@"-f -g ""{gameLocation}""",
                CreateNoWindow = true,
            };

            Process.Start(startInfo);
        }
        else if (launcher == "Citron")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = launcherLocation,
                Arguments = $@"-f -g ""{gameLocation}""",
                CreateNoWindow = true,
            };

            Process.Start(startInfo);
        }
        else if (launcher == "Ryujinx")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = launcherLocation,
                Arguments = $@"-r ""{dataLocation}"" -fullscreen ""{gameLocation}""",
                CreateNoWindow = true,
            };

            Process.Start(startInfo);
        }

        localSettings.Values[$"LastPlayed_{tile.Title}"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (currentSortKey == "Recently played")
        {
            LoadSortSettings();
        }
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (Launcher == "Epic Games")
        {
            Process.Start(new ProcessStartInfo($"com.epicgames.launcher://apps/{CatalogNamespace}%3A{CatalogItemId}%3A{AppName}?action=update") { UseShellExecute = true });
        }
    }

    private async void StopProcesses_Click(object sender, RoutedEventArgs e)
    {
        // disable hittestvisible to avoid double-clicking
        StopProcesses.IsHitTestVisible = false;

        await Task.Run(() =>
        {
            // close dllhost processes
            foreach (var proc in Process.GetProcessesByName("dllhost"))
            {
                string cmdLine = ProcessesHelper.GetCommandLine(proc);

                if (cmdLine.Contains("/PROCESSID", StringComparison.OrdinalIgnoreCase))
                {
                    proc.Kill();
                    proc.WaitForExit();
                }
            }

            // close executables
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 0, RegistryValueKind.DWord);

            var processNames = new[]
            {
                "ApplicationFrameHost",
                "CrashReportClient",
                "CrossDeviceResume",
                //"ctfmon",
                "DataExchangeHost",
                "EasyAntiCheat_EOS",
                "EpicGamesLauncher",
                "explorer",
                "Everything",
                //"Files",
                "FortniteClient-Win64-Shipping_EAC_EOS",
                "GameBar",
                "GameBarFTServer",
                "LeagueCrashHandler64",
                "LsaIso",
                "mobsync",
                "NgcIso",
                "RiotClientServices",
                "RiotClientCrashHandler",
                "rundll32",
                "RuntimeBroker",
                "SearchHost",
                "secd",
                "ShellExperienceHost",
                "SpatialAudioLicenseSrv",
                "sppsvc",
                "StartMenuExperienceHost",
                "SystemSettingsBroker",
                "TrustedInstaller",
                "useroobebroker",
                //"WMIADAP",
                //"WmiPrvSE",
                "WUDFHost"
            };

            foreach (var name in processNames)
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch { }
                }
            }

            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 1, RegistryValueKind.DWord);

            // stop services
            var serviceNames = new[]
            {
                "AudioEndpointBuilder",
                "AppXSvc",
                "Appinfo",
                "CaptureService",
                "cbdhsvc",
                "ClipSvc",
                "CryptSvc",
                "DevicesFlowUserSvc",
                "DeviceAssociationService",
                "Dhcp",
                "DispBrokerDesktopSvc",
                //"Dnscache",
                "DoSvc",
                "Everything (1.5a)",
                "gpsvc",
                "InstallService",
                //"KeyIso",
                "LicenseManager",
                "lfsvc",
                "msiserver",
                "Netman",
                "NetSetupSvc",
                "netprofm",
                "NgcCtnrSvc",
                "NgcSvc",
                "nsi",
                "ProfSvc",
                "StateRepository",
                //"TextInputManagementService",
                "TrustedInstaller",
                "UdkUserSvc",
                "UserManager",
                "WFDSConMgrSvc",
                "Windhawk",
                "WinHttpAutoProxySvc",
                //"Winmgmt",
                "Wcmsvc"
            };

            foreach (var serviceName in serviceNames)
            {
                ServicesHelper.KillServiceProcess(serviceName);
            }

            try { new ServiceController("KeyIso").Stop(); } catch { }
            //try { new ServiceController("Winmgmt").Stop(); } catch { }

            if (Process.GetProcessesByName("ClassicWindowSwitcher").Length == 0)
                Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "ClassicWindowSwitcher", "ClassicWindowSwitcher.exe")) { CreateNoWindow = true });
        });

        // re-enable hittestvisible
        StopProcesses.IsHitTestVisible = true;
    }

    private async void RestartProcesses_Click(object sender, RoutedEventArgs e)
    {
        // disable hittestvisible to avoid double-clicking
        StopProcesses.IsHitTestVisible = false;

        await Task.Run(() =>
        {
            Process.GetProcessesByName("ClassicWindowSwitcher").FirstOrDefault()?.Kill();

            // launch explorer
            Process.Start("explorer.exe");

            // start windhawk service
            using var windhawkService = new ServiceController("Windhawk");
            if (windhawkService.Status == ServiceControllerStatus.Stopped)
            {
                windhawkService.Start();
            }

            // restart services
            var serviceNames = new[]
            {
                "AudioEndpointBuilder",
                "AppXSvc",
                "Appinfo",
                "CaptureService",
                "cbdhsvc",
                "ClipSvc",
                "CryptSvc",
                "DevicesFlowUserSvc",
                "DeviceAssociationService",
                "Dhcp",
                "DispBrokerDesktopSvc",
                //"Dnscache",
                "DoSvc",
                "Everything (1.5a)",
                "gpsvc",
                "InstallService",
                "KeyIso",
                "LicenseManager",
                "lfsvc",
                "msiserver",
                "Netman",
                "NetSetupSvc",
                "netprofm",
                "NgcCtnrSvc",
                "NgcSvc",
                "nsi",
                "ProfSvc",
                "StateRepository",
                //"TextInputManagementService",
                "TrustedInstaller",
                "UdkUserSvc",
                "UserManager",
                "WFDSConMgrSvc",
                //"WinHttpAutoProxySvc",
                "Winmgmt",
                //"Wcmsvc"
            };

            foreach (var serviceName in serviceNames)
            {
                try
                {
                    using var sc = new ServiceController(serviceName);

                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                    }
                }
                catch { }
            }

            string filePath = @"C:\Program Files\Everything 1.5a\Everything.exe";

            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = "-startup",
                });
            }
        });

        // re-enable hittestvisible
        StopProcesses.IsHitTestVisible = true;
    }

    private DispatcherTimer gameWatcherTimer;
    private bool? previousGameState = null;
    private bool? previousExplorerState = null;
    private bool servicesState = false;
    private readonly Dictionary<string, DateTime> epicGameStartTimes = new();

    void StartGameWatcher(Func<bool> isGameRunning)
    {
        if (gameWatcherTimer != null)
        {
            gameWatcherTimer.Stop();
            gameWatcherTimer = null;
        }

        previousGameState = null;
        previousExplorerState = null;

        servicesState = new ServiceController("Beep").Status == ServiceControllerStatus.Running;

        gameWatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        void TickHandler()
        {
            bool isRunning = isGameRunning();
            bool explorerRunning = Process.GetProcessesByName("explorer").Length > 0;

            DispatcherQueue.TryEnqueue(() =>
            {
                if (Play != null)
                    Play.IsEnabled = !isRunning;

                if (StopProcesses != null && !servicesState)
                    StopProcesses.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;

                if (RestartProcesses != null && !servicesState && previousExplorerState != (isRunning && !explorerRunning))
                {
                    RestartProcesses.Visibility = (isRunning && !explorerRunning) ? Visibility.Visible : Visibility.Collapsed;
                    previousExplorerState = isRunning && !explorerRunning;
                }

                if (previousGameState == true && isRunning == false && !explorerRunning)
                {
                    RestartProcesses_Click(this, new RoutedEventArgs());
                }

                if (Launcher == "Epic Games")
                {
                    if (isRunning && !epicGameStartTimes.ContainsKey(ArtifactId))
                    {
                        epicGameStartTimes[ArtifactId] = DateTime.UtcNow;
                    }
                    else if (!isRunning && epicGameStartTimes.TryGetValue(ArtifactId, out var startTime))
                    {
                        EpicGamesHelper.AddPlaytime(ArtifactId, startTime);
                        epicGameStartTimes.Remove(ArtifactId);
                    }
                }

                previousGameState = isRunning;
            });
        }

        gameWatcherTimer.Tick += (s, e) => TickHandler();

        TickHandler();
        gameWatcherTimer.Start();
    }

    public void CheckGameRunning()
    {
        previousGameState = null;
        previousExplorerState = null;

        if (Launcher == "Epic Games")
        {
            string offlineExecutable = Path.GetFileNameWithoutExtension(LaunchExecutable);
            string onlineExecutable = Title switch
            {
                "Fortnite" => "FortniteClient-Win64-Shipping",
                "Fall Guys" => "FallGuys_client_game",
                _ => string.Empty
            };
            if (Title == "Fall Guys") offlineExecutable = "FallGuys_client";

            StartGameWatcher(() =>
                (!string.IsNullOrEmpty(offlineExecutable) && Process.GetProcessesByName(Path.GetFileNameWithoutExtension(offlineExecutable)).Length > 0) ||
                (!string.IsNullOrEmpty(onlineExecutable) && Process.GetProcessesByName(Path.GetFileNameWithoutExtension(onlineExecutable)).Length > 0) ||
                (ProcessNames?.Any(process => !string.IsNullOrEmpty(process) && Process.GetProcessesByName(Path.GetFileNameWithoutExtension(process)).Length > 0) ?? false)
            );
        }
        else if (Launcher == "Steam")
        {
            var exeNames = Directory.GetFiles(InstallLocation, "*.exe", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct()
                .ToList();

            if (exeNames.Count == 0) return;

            StartGameWatcher(() =>
                exeNames.Any(name => Process.GetProcessesByName(name).Length > 0)
            );
        }
        else if (Launcher == "Eden")
        {
            StartGameWatcher(() =>
            {
                foreach (var proc in Process.GetProcessesByName(Path.GetFileName(LauncherLocation).Replace(".exe", "")))
                {
                    string cmdLine = ProcessesHelper.GetCommandLine(proc);

                    if (cmdLine.Contains($@"-f -g ""{GameLocation}""", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            });
        }
        else if (Launcher == "Citron")
        {
            StartGameWatcher(() =>
            {
                foreach (var proc in Process.GetProcessesByName(Path.GetFileName(LauncherLocation).Replace(".exe", "")))
                {
                    string cmdLine = ProcessesHelper.GetCommandLine(proc);

                    if (cmdLine.Contains($@"-f -g ""{GameLocation}""", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            });
        }
        else if (Launcher == "Ryujinx")
        {
            StartGameWatcher(() =>
            {
                foreach (var proc in Process.GetProcessesByName(Path.GetFileName(LauncherLocation).Replace(".exe", "")))
                {
                    string cmdLine = ProcessesHelper.GetCommandLine(proc);

                    if (cmdLine.Contains($@"-r ""{DataLocation}"" -fullscreen ""{GameLocation}""", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            });
        }
    }
}

public enum InfoIconType
{
    FontIcon,
    PathIcon
}

[GeneratedBindableCustomProperty]
public partial class InfoItem
{
    public string Label { get; set; }
    public string Value { get; set; }
    public string Glyph { get; set; }
    public Geometry PathData { get; set; }
    public InfoIconType IconType { get; set; }
    public bool IsFontIcon => IconType == InfoIconType.FontIcon;
    public bool IsPathIcon => IconType == InfoIconType.PathIcon;
    public bool IsHyperlink { get; set; } = false;
    public string Hyperlink { get; set; }
}