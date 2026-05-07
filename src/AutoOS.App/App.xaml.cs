using AutoOS.Core.Helpers.Logging;
using AutoOS.Views.Installer.Stages;
using AutoOS.Views.Startup;
using AutoOS.Views;
using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace AutoOS
{
	public partial class App : Application
	{
		public new static App Current => (App)Application.Current;
		public static Window MainWindow = Window.Current;
		public static IntPtr Hwnd => WindowNative.GetWindowHandle(MainWindow);
		public JsonNavigationService NavService { get; set; }
		public IThemeService ThemeService { get; set; }
		internal static bool IsInstalled { get; private set; }
		internal static double Scaling { get; set; }
		private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

		public App()
		{
			InitializeComponent();
			NavService = new JsonNavigationService();
			IsInstalled = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\AutoOS", "IsInstalled", 0) as int? ?? 0) == 1 || Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AutoOS")?.GetValue("Stage") as string == "Installed";
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
				AppActivationArguments appActivationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();

				if (appActivationArguments.Kind is ExtendedActivationKind.StartupTask)
				{
					Application.Current.Exit();
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
		}

		private async void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			await ShowErrorMessage(e.Exception);
		}

		private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception ex)
			{
				_ = ShowErrorMessage(ex);
			}
		}

		private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			e.SetObserved();

			if (e.Exception != null && !e.Exception.Message.Contains("Response body is unavailable for redirect responses"))
				_ = ShowErrorMessage(e.Exception);
		}

		internal static async Task ShowErrorMessage(Exception ex)
		{
			try
			{
				await LogHelper.LogError(ex, PreparingStage.GPUs);
			}
			catch { }

			if (MainWindow?.DispatcherQueue != null)
			{
				MainWindow.DispatcherQueue.TryEnqueue(async () =>
				{
					await MessageBox.ShowErrorAsync(MainWindow, ex.Message, "Unexpected Error");
				});
			}
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

			try
			{
				var result = await dialog.ShowAsync();

				if (result == ContentDialogResult.Primary)
				{
					Current.Exit();
				}
			}
			catch { }
		}
	}
}
