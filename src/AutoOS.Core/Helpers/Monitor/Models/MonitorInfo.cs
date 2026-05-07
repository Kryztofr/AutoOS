namespace AutoOS.Core.Helpers.Monitor.Models;

public class MonitorInfo
{
    public string DeviceName { get; set; } = ""; 
    public string DevicePath { get; set; } = "";
    public (uint Width, uint Height) Resolution { get; set; }
    public uint RefreshRate { get; set; }
}
