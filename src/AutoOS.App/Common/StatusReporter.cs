using AutoOS.Core.Common;
using AutoOS.Views.Installer;
using AutoOS.Views.Updater;

namespace AutoOS.Common;

public class InstallPageReporter : IStatusReporter
{
    private readonly SynchronizationContext _uiContext;

    private string _capturedTitle = string.Empty;
    private string _lastSetTitle;

    public InstallPageReporter()
    {
        _uiContext = SynchronizationContext.Current;
    }

    public void Report(string message = null, double? progress = null, bool? isIndeterminate = null)
    {
        _uiContext?.Post(_ =>
        {
            if (InstallPage.Info != null)
            {
                string currentTitle = InstallPage.Info.Title ?? string.Empty;

                if (_lastSetTitle == null || currentTitle != _lastSetTitle)
                {
                    _capturedTitle = currentTitle;
                }

                if (!string.IsNullOrEmpty(message))
                {
                    _lastSetTitle = string.IsNullOrEmpty(_capturedTitle) ? message : $"{_capturedTitle} ({message})";
                    InstallPage.Info.Title = _lastSetTitle;
                }
                
                if (progress.HasValue)
                    InstallPage.ProgressRingControl.Value = progress.Value;
                
                if (isIndeterminate.HasValue)
                    InstallPage.ProgressRingControl.IsIndeterminate = isIndeterminate.Value;
            }
        }, null);
    }
}

public class ProgressButtonReporter(ProgressButton progressButton) : IStatusReporter
{
    private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
    private readonly ProgressButton _progressButton = progressButton;

	public void Report(string message = null, double? progress = null, bool? isIndeterminate = null)
    {
        _uiContext?.Post(_ =>
        {
            if (progress.HasValue)
                _progressButton.Progress = progress.Value;
            
            if (isIndeterminate.HasValue)
                _progressButton.IsIndeterminate = isIndeterminate.Value;
        }, null);
    }
}

public class UpdateDialogReporter(UpdateDialog updateDialog) : IStatusReporter
{
    private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
    private readonly UpdateDialog _updateDialog = updateDialog;

    public void Report(string message = null, double? progress = null, bool? isIndeterminate = null)
    {
        _uiContext?.Post(_ =>
        {
            string title = _updateDialog.CurrentTitle ?? _updateDialog.GetStatus()?.TrimEnd('.') ?? string.Empty;

            if (!string.IsNullOrEmpty(message))
            {
                _updateDialog.SetStatus($"{title} ({message})");
            }

            if (progress.HasValue)
                _updateDialog.SetProgress(progress.Value);

        }, null);
    }
}
