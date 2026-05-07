namespace AutoOS.Core.Common;

public interface IStatusReporter
{
    void Report(string message = null, double? progress = null, bool? isIndeterminate = null);
}
