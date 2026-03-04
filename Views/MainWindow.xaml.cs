using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT;
using WinRT.Interop;

namespace AutoOS.Views
{
    public sealed partial class MainWindow : Window
    {
        public string TitleBarName { get; set; }
        internal static MainWindow Instance { get; set; }

        public  string AppSubtitle
        {
            get
            {
                var version = new Version(ProcessInfoHelper.Version);
                if (version.Revision > 0)
                {
                    return ProcessInfoHelper.VersionWithPrefix + " - Pre-release";
                }
                return ProcessInfoHelper.VersionWithPrefix;
            }
        }

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            _ = new ModernSystemMenu(this);

            var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
            presenter.PreferredMinimumWidth = 660;
            presenter.PreferredMinimumHeight = 715;

            if (App.IsInstalled)
            {
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsSettings.PageDictionary)
                    .ConfigureDefaultPage(typeof(Settings.HomeLandingPage))
                    .ConfigureSettingsPage(typeof(SettingsPage))
                    .ConfigureJsonFile("Assets/NavViewMenu/Settings.json")
                    .ConfigureTitleBar(AppTitleBar, false)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
                AppTitleBar.Title = "AutoOS Settings";

                NavView.IsSettingsVisible = true;
            }
            else
            {
                App.Current.NavService
                    .Initialize(NavView, NavFrame, NavigationPageMappingsInstaller.PageDictionary)
                    .ConfigureDefaultPage(typeof(Installer.HomeLandingPage))
                    .ConfigureJsonFile("Assets/NavViewMenu/Installer.json")
                    .ConfigureTitleBar(AppTitleBar, false)
                    .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
                AppTitleBar.Title = "AutoOS Installer";

                presenter.Maximize();
            }
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.IsInstalled)
            {
                await Task.Delay(100);
                foreach (var item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
                {
                    item.IsEnabled = false;
                }
            }
        }

        private readonly HashSet<string> _visitedPages = [];
        public IReadOnlyCollection<string> VisitedPages => _visitedPages;

        public readonly string[] AllPages =
        [
            "PersonalizationPage",
            "ApplicationsPage",
            "BrowsersPage",
            "DisplayPage",
            "GraphicsPage",
            "SecurityPage"
        ];

        public void MarkVisited(string pageName)
        {
            _visitedPages.Add(pageName);
        }

        public bool AllPagesVisited()
        {
            return AllPages.All(p => _visitedPages.Contains(p));
        }

        public void CheckAllPagesVisited()
        {
            if (AllPagesVisited())
            {
                var navView = GetNavView();
                foreach (var item in navView.FooterMenuItems.OfType<NavigationViewItem>())
                {
                    item.IsEnabled = true;
                }
            }
        }

        public NavigationView GetNavView()
        {
            return NavView;
        }

        public TitleBar GetTitleBar()
        {
            return AppTitleBar;
        }

        private void AppIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PInvoke.PostMessage((HWND)WindowNative.GetWindowHandle(App.MainWindow), PInvoke.WM_SYSCOMMAND, 0xF090, 0);
        }
    }
}