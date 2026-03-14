using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using AutoOS.Helpers.Store;

namespace AutoOS.Views.Installer.Stages;

public static class AppxStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Status.Text = "Configuring AppX Packages...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // onedrive
            ("Uninstalling OneDrive", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c taskkill /f /im ""OneDrive.exe"" & taskkill /f /im ""OneDrive.Sync.Service.exe"""), null),
            ("Uninstalling OneDrive", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c for %a in (""SysWOW64"" ""System32"") do (if exist ""%windir%\%~a\OneDriveSetup.exe"" (""%windir%\%~a\OneDriveSetup.exe"" /uninstall)) && reg delete ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{018D5C66-4533-4307-9B53-224DE2ED1FE6}"" /f"), null),
            ("Uninstalling OneDrive", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c rmdir /s /q ""C:\ProgramData\Microsoft OneDrive"""), null),
            ("Uninstalling OneDrive", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c rmdir /s /q ""%LOCALAPPDATA%\Microsoft\OneDrive"""), null),
            ("Uninstalling OneDrive", async () => await ProcessActions.RunPowerShell(@"Get-ScheduledTask | Where-Object {$_.TaskName -like 'OneDrive Startup Task*'} | Unregister-ScheduledTask -Confirm:$false"), null),
        };

        // add uninstall actions
        var packagesToRemove = new List<string>
        {
            "Clipchamp.Clipchamp_yxz26nhyzhsrt",
            "Microsoft.BingNews_8wekyb3d8bbwe",
            "Microsoft.BingSearch_8wekyb3d8bbwe",
            "Microsoft.BingWeather_8wekyb3d8bbwe",
            "Microsoft.GetHelp_8wekyb3d8bbwe",
            "Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe",
            "Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe",
            "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe",
            "Microsoft.OutlookForWindows_8wekyb3d8bbwe",
            "Microsoft.Paint_8wekyb3d8bbwe",
            "Microsoft.PowerAutomateDesktop_8wekyb3d8bbwe",
            "Microsoft.Todos_8wekyb3d8bbwe",
            "Microsoft.Windows.DevHome_8wekyb3d8bbwe",
            "Microsoft.WindowsAlarms_8wekyb3d8bbwe",
            "Microsoft.WindowsCalculator_8wekyb3d8bbwe",
            "Microsoft.WindowsCamera_8wekyb3d8bbwe",
            "Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe",
            "Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe",
            "Microsoft.WindowsTerminal_8wekyb3d8bbwe",
            "Microsoft.XboxSpeechToTextOverlay_8wekyb3d8bbwe",
            "Microsoft.YourPhone_8wekyb3d8bbwe",
            "Microsoft.ZuneMusic_8wekyb3d8bbwe",
            "MicrosoftCorporationII.MicrosoftFamily_8wekyb3d8bbwe",
            "MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe",
            "MSTeams_8wekyb3d8bbwe",
            "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy"
        };

        foreach (var package in packagesToRemove)
        {
            actions.Add(($"Uninstalling {package}", async () => await StoreHelper.Remove(package), null));
            actions.Add(($"Deprovisioning {package}", async () => await StoreHelper.Deprovision(package), null));
        }

        // add update actions
        var packagesToUpdate = new List<string>
        {
            "Microsoft.StorePurchaseApp_8wekyb3d8bbwe",
            "Microsoft.WindowsStore_8wekyb3d8bbwe",
            "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe",
            "Microsoft.WindowsNotepad_8wekyb3d8bbwe",
            "Microsoft.Windows.Photos_8wekyb3d8bbwe",
            "Microsoft.ScreenSketch_8wekyb3d8bbwe",
            "Microsoft.XboxIdentityProvider_8wekyb3d8bbwe",
            "Microsoft.Xbox.TCUI_8wekyb3d8bbwe",
            "Microsoft.GamingApp_8wekyb3d8bbwe",
            "Microsoft.XboxGamingOverlay_8wekyb3d8bbwe",
            "Microsoft.HEIFImageExtension_8wekyb3d8bbwe",
            "Microsoft.VP9VideoExtensions_8wekyb3d8bbwe",
            "Microsoft.WebMediaExtensions_8wekyb3d8bbwe",
            "Microsoft.WebpImageExtension_8wekyb3d8bbwe",
            "Microsoft.HEVCVideoExtension_8wekyb3d8bbwe",
            "Microsoft.RawImageExtension_8wekyb3d8bbwe",
            "Microsoft.MPEG2VideoExtension_8wekyb3d8bbwe",
            "Microsoft.AV1VideoExtension_8wekyb3d8bbwe",
            "Microsoft.AVCEncoderVideoExtension_8wekyb3d8bbwe",
            "Microsoft.ApplicationCompatibilityEnhancements_8wekyb3d8bbwe",
            "MicrosoftWindows.CrossDevice_cw5n1h2txyewy"
        };

        foreach (var package in packagesToUpdate)
        {
            actions.Add(($"Updating {package}", async () => await StoreHelper.Update(package), null));
        }

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