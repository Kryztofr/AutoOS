using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class OptionalFeatureStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Status.Text = "Configuring Optional Features...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable optional features
            (@"Disabling ""WorkFolders-Client"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName WorkFolders-Client -Online -NoRestart -ErrorAction Stop"), null),
            (@"Disabling ""WCF-Services45"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName WCF-Services45 -Online -NoRestart -ErrorAction Stop"), null),
            (@"Disabling ""WCF-TCP-PortSharing45"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName WCF-TCP-PortSharing45 -Online -NoRestart -ErrorAction Stop"), null),
            (@"Disabling ""MediaPlayback"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName MediaPlayback -Online -NoRestart -ErrorAction Stop"), null),
            (@"Disabling ""WindowsMediaPlayer"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName WindowsMediaPlayer -Online -NoRestart -ErrorAction Stop"), null),
            (@"Disabling ""SmbDirect"" optional feature", async () => await ProcessActions.RunPowerShell(@"Disable-WindowsOptionalFeature -FeatureName SmbDirect -Online -NoRestart -ErrorAction Stop"), null),

            // remove capabilities 
            (@"Removing ""App.StepsRecorder"" capability", async () => await ProcessActions.RunPowerShell(@"Remove-WindowsCapability -Online -Name (Get-WindowsCapability -Online | Where Name -like ""App.StepsRecorder*"").Name"), null),
            (@"Removing ""Browser.InternetExplorer"" capability", async () => await ProcessActions.RunPowerShell(@"Remove-WindowsCapability -Online -Name (Get-WindowsCapability -Online | Where Name -like ""Browser.InternetExplorer*"").Name"), null),
            (@"Removing ""Media.WindowsMediaPlayer"" capability", async () => await ProcessActions.RunPowerShell(@"Remove-WindowsCapability -Online -Name (Get-WindowsCapability -Online | Where Name -like ""Media.WindowsMediaPlayer*"").Name"), null),
            (@"Removing ""Microsoft.Windows.PowerShell.ISE"" capability", async () => await ProcessActions.RunPowerShell(@"Remove-WindowsCapability -Online -Name (Get-WindowsCapability -Online | Where Name -like ""Microsoft.Windows.PowerShell.ISE*"").Name"), null),
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