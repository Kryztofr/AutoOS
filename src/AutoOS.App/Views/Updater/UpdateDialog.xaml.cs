using Microsoft.UI.Xaml.Media;
using Downloader;

namespace AutoOS.Views.Updater;

public sealed partial class UpdateDialog : UserControl
{
    public double CurrentGroupStart { get; set; }
    public double CurrentGroupTarget { get; set; }

    public UpdateDialog()
    {
        InitializeComponent();
    }

    public void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    public void SetProgress(double value)
    {
        ProgressBar.IsIndeterminate = false;
        ProgressBar.Value = value;
    }

    public void SetSuccess()
    {
        ProgressBar.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
    }

    public void SetError()
    {
        ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
    }

    public void SetCaution()
    {
        ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
    }

    public void ResetProgressColor()
    {
        ProgressBar.ClearValue(ProgressBar.ForegroundProperty);
    }

    public async Task RunActions(List<(string Title, Func<Task> Action, Func<bool> Condition)> actions)
    {
        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();

        string previousTitle = string.Empty;
        int groupedTitleCount = 0;
        List<Func<Task>> currentGroup = [];

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? 100.0 / (double)groupedTitleCount : 0;

        ProgressBar.IsIndeterminate = false;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                CurrentGroupStart = ProgressBar.Value;
                CurrentGroupTarget = CurrentGroupStart + incrementPerTitle;

                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        StatusText.Text = ex.Message;
                        SetError();
                    }
                }

                ProgressBar.Value = CurrentGroupTarget;
                await Task.Delay(500);
                currentGroup.Clear();
            }

            StatusText.Text = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            CurrentGroupStart = ProgressBar.Value;
            CurrentGroupTarget = CurrentGroupStart + incrementPerTitle;

            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    StatusText.Text = ex.Message;
                    SetError();
                }
            }
            ProgressBar.Value = CurrentGroupTarget;
        }
    }

    public async Task Download(string url, string path, string file, string displayTitle, double startValue, double targetValue)
    {
        SetStatus(displayTitle + "...");

        var uiContext = SynchronizationContext.Current;

        var download = DownloadBuilder.New()
            .WithUrl(url)
            .WithDirectory(path)
            .WithFileName(file)
            .WithConfiguration(new DownloadConfiguration())
            .Build();

        double speedMB = 0.0;
        double receivedMB = 0.0;
        double totalMB = 0.0;
        double percentage = 0.0;
        DateTime lastLoggedTime = DateTime.MinValue;

        download.DownloadProgressChanged += (sender, e) =>
        {
            if ((DateTime.Now - lastLoggedTime).TotalMilliseconds < 50) return;
            lastLoggedTime = DateTime.Now;

            speedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
            receivedMB = e.ReceivedBytesSize / (1024.0 * 1024.0);
            totalMB = e.TotalBytesToReceive / (1024.0 * 1024.0);
            percentage = e.ProgressPercentage;

            uiContext?.Post(_ =>
            {
                StatusText.Text = $"{displayTitle} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = startValue + (percentage / 100.0 * (targetValue - startValue));
            }, null);
        };

        download.DownloadFileCompleted += (sender, e) =>
        {
            uiContext?.Post(_ =>
            {
                StatusText.Text = $"{displayTitle} ({speedMB:F1} MB/s - {totalMB:F2} MB of {totalMB:F2} MB)";
                ProgressBar.Value = targetValue;
            }, null);
        };

        await download.StartAsync();
    }
}
