using AutoOS.Views.Installer.Actions;
using CommunityToolkit.WinUI;
using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;
using AutoOS.Views.Startup;

namespace AutoOS
{
    public partial class App : Application
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public new static App Current => (App)Application.Current;
        public static Window MainWindow = Window.Current;
        public JsonNavigationService NavService { get; set; }
        public IThemeService ThemeService { get; set; }
        internal static bool IsInstalled { get; private set; }
        internal static double Scaling { get; set; }

        public App()
        {
            //Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\AutoOS", "Stage", "Installed", RegistryValueKind.String);
            //Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS", true)?.DeleteValue("Stage", false);

            InitializeComponent();
            NavService = new JsonNavigationService();

            IsInstalled = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage") as string == "Installed";

            Application.Current.UnhandledException += Current_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (IsInstalled)
            {
                AppActivationArguments appActivationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();

                if (appActivationArguments.Kind is ExtendedActivationKind.StartupTask)
                {
                    MainWindow = new StartupWindow();
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Startup";
                    MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

                    Window window = MainWindow;
                    var monitor = DisplayMonitorHelper.GetMonitorInfo(window);
                    int X = (int)monitor.RectMonitor.Width;
                    int Y = (int)monitor.RectMonitor.Height;

                    int windowWidth = (int)(340 * Scaling);
                    int windowHeight = (int)(130 * Scaling);

                    int posX = X - windowWidth - (int)(10 * Scaling);
                    int posY = Y - windowHeight - (int)(53 * Scaling);

                    MainWindow.AppWindow.MoveAndResize(new RectInt32(posX, posY, windowWidth, windowHeight));

                    if (!localSettings.Values.TryGetValue("LaunchMinimized", out object value) || (int)value == 0)
                    {
                        MainWindow.Activate();
                    }
                }
                else
                {
                    MainWindow = new MainWindow();
                    MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Settings";
                    MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
                    ThemeService = new ThemeService().Initialize(MainWindow);

                    WindowHelper.ResizeAndCenterWindowToPercentageOfWorkArea(MainWindow, 92);

                    MainWindow.Activate();
                }
            }
            else
            {
                MainWindow = new MainWindow();
                MainWindow.Title = MainWindow.AppWindow.Title = "AutoOS Installer";
                MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

                AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(MainWindow))).Closing += AppWindow_Closing;

                ThemeService = new ThemeService().Initialize(MainWindow);

                MainWindow.Activate();
            }
        }

        private async void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            await ShowCrashDialogAsync(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                _ = ShowCrashDialogAsync(ex);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();

            if (e.Exception != null && !e.Exception.Message.Contains("Response body is unavailable for redirect responses"))
                _ = ShowCrashDialogAsync(e.Exception);
        }

        internal static async Task ShowCrashDialogAsync(Exception ex)
        {
            await ProcessActions.LogError(ex);
            await MainWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Unexpected Error",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = MainWindow.Content.XamlRoot
                };

                return dialog.ShowAsync().AsTask();
            });
        }

        private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;

            var dialog = new ContentDialog
            {
                Title = "Close AutoOS?",
                Content = "Are you sure that you want to close AutoOS?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = MainWindow.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Current.Exit();
            }
        }
    }
}