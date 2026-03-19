using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using System.Diagnostics;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class RuntimesStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Status.Text = "Configuring Runtimes...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download the visual c++ redistributable
            ("Downloading the Visual C++ Redistributable", async () => await ProcessActions.RunDownload("https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe", ApplicationData.Current.TemporaryFolder.Path, "VisualCppRedist_AIO_x86_x64.exe"), null),

            // install visual c++ redistributable
            ("Installing the Visual C++ Redistributable", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VisualCppRedist_AIO_x86_x64.exe"), Arguments = "/ai /gm2", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("VisualCppRedist_AIO_x86_x64.exe")).DeleteAsync(); }, null),

            // download the microsoft edge webview2 runtime
            ("Downloading the Microsoft Edge WebView2 Runtime", async () => await ProcessActions.RunDownload("https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/7dedb563-79f6-48af-b588-dd8e97f4b73c/MicrosoftEdgeWebView2RuntimeInstallerX64.exe", ApplicationData.Current.TemporaryFolder.Path, "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), null),

            // install microsoft edge webview2 runtime
            ("Installing the Microsoft Edge WebView2 Runtime", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), Arguments = "/silent /install", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("MicrosoftEdgeWebView2RuntimeInstallerX64.exe")).DeleteAsync(); }, null),
            ("Installing the Microsoft Edge WebView2 Runtime", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MicrosoftEdgeUpdate.exe", "Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String), null),

            // download microsoft windows app runtime
            ("Downloading the Microsoft Windows App Runtime", async () => await ProcessActions.RunDownload("https://aka.ms/windowsappsdk/1.6/1.6.250602001/windowsappruntimeinstall-x64.exe", ApplicationData.Current.TemporaryFolder.Path, "WindowsAppRuntimeInstall-x64.exe"), null),

            // install microsoft windows app runtime
            ("Installing the Microsoft Windows App Runtime", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "WindowsAppRuntimeInstall-x64.exe"), Arguments = "--quiet --force --msix --license" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("WindowsAppRuntimeInstall-x64.exe")).DeleteAsync(); }, null),

            // download the directx redistributable
            ("Downloading the DirectX Redistributable", async () => await ProcessActions.RunDownload("https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe", ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist.exe"), null),

            // extract the directx redistributable
            ("Extracting the DirectX Redistributable", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist.exe"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist")); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("directx_Jun2010_redist.exe")).DeleteAsync(); },null),

            // install the directx redistributable
            ("Installing the DirectX Redistributable", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist", "DXSetup.exe"), Arguments = "/silent", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
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
