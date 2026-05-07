using AutoOS.Core.Common;
using AutoOS.Views.Installer;

namespace AutoOS.Common;

public class InstallPageReporter : IStatusReporter
{
    private readonly SynchronizationContext _uiContext;

    private string _capturedTitle;
    private bool _titleHasBeenCaptured;

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
                if (!_titleHasBeenCaptured)
                {
                    _capturedTitle = InstallPage.Info.Title ?? string.Empty;
                    _titleHasBeenCaptured = true;
                }

                if (!string.IsNullOrEmpty(message))
                    InstallPage.Info.Title = string.IsNullOrEmpty(_capturedTitle) ? message : $"{_capturedTitle} ({message})";
                
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
