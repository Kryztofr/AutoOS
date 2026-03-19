using AutoOS.Helpers.Monitor;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using AutoOS.Helpers.Services;
using Microsoft.UI.Xaml.Media;
using AutoOS.Views.Installer.Actions;
using WinRT.Interop;
using Windows.Storage;
using System.Text.Json;

namespace AutoOS.Views.Installer.Stages;

public static partial class GamesStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool Fortnite = ApplicationStage.Fortnite;

        InstallPage.Status.Text = "Configuring Games...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        string fortnitePath = string.Empty;

        string iniPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "GameUserSettings.ini");
        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "GameUserSettings.ini"), iniPath);
        InIHelper iniHelper = new(iniPath);

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // setting fortnite frame rate
            ("Setting Fortnite Frame Rate", async () => iniHelper.AddValue("FrameRateLimit", $"{MonitorHelper.GetMonitors().Max(m => m.RefreshRate)}.000000", "/Script/FortniteGame.FortGameUserSettings"), () => Fortnite == true),
            ("Setting Fortnite Frame Rate", async () => await Task.Delay(1000), () => Fortnite == true),
            
            // import fortnite settings
            ("Importing Fortnite settings", async () => Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(@"%LocalAppData%\FortniteGame\Saved\Config\WindowsClient")), () => Fortnite == true),
            ("Importing Fortnite settings", async () => File.Copy(iniPath, Environment.ExpandEnvironmentVariables(@"%LocalAppData%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"), true), () => Fortnite == true),
            ("Importing Fortnite settings", async () => await Task.Delay(1000), () => Fortnite == true),

            // set gpu preference to high performance for fortnite
            ("Setting GPU Preference to high performance for Fortnite", async () => fortnitePath = JsonDocument.Parse(File.ReadAllText(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat")).RootElement.GetProperty("InstallationList").EnumerateArray().FirstOrDefault(e => e.GetProperty("AppName").GetString() == "Fortnite").GetProperty("InstallLocation").GetString(), () => Fortnite == true),
            ("Setting GPU Preference to high performance for Fortnite", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences", fortnitePath + @"\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe", "SwapEffectUpgradeEnable=1;GpuPreference=2;", RegistryValueKind.String), () => Fortnite == true),
            ("Setting GPU Preference to high performance for Fortnite", async () => await Task.Delay(1000), () => Fortnite == true),

            // install easyanticheat
            ("Installing EasyAntiCheat", async () => await Process.Start(new ProcessStartInfo($@"{fortnitePath}\FortniteGame\Binaries\Win64\EasyAntiCheat\EasyAntiCheat_EOS_Setup.exe", "install 4fe75bbc5a674f4f9b356b5c90567da5") {  WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Fortnite == true),
            ("Installing EasyAntiCheat", async () => await Task.Delay(1000), () => Fortnite == true),
            ("Disabling EasyAntiCheat startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EasyAntiCheat_EOS", "Start", 4, RegistryValueKind.DWord), () => Fortnite == true),
            ("Disabling EasyAntiCheat startup entry", async () => ServicesHelper.StopService("EasyAntiCheat_EOS"), () => Fortnite == true),
            ("Disabling EasyAntiCheat startup entry", async () => await Task.Delay(1000), () => Fortnite == true),
        
            // disable fullscreen optimizations for fortnite
            ("Disabling fullscreen optimizations for Fortnite", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", $@"{fortnitePath}\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe", "~ DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String), () => Fortnite == true),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        int groupedTitleCount = 0;

        List<Func<Task>> currentGroup = [];

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        await ProcessActions.LogError(ex);

                        InstallPage.Info.Title = $"{previousTitle}: {ex.Message}";
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Error);
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;

                        var tcs = new TaskCompletionSource<bool>();

                        RoutedEventHandler resumeHandler = null;
                        resumeHandler = (sender, e) =>
                        {
                            InstallPage.ResumeButton.Click -= resumeHandler;
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                            Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;

                            tcs.TrySetResult(true);
                        };

                        InstallPage.ResumeButton.Click += resumeHandler;
                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title += ": " + ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Error);
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;
                    await ProcessActions.LogError(ex);

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
            Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
        if (filteredActions.Count == 0)
        {
            InstallPage.Progress.Value += stagePercentage;
            Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
    }
}
