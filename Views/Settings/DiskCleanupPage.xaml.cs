using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.FileProperties;
using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Settings;

public sealed partial class DiskCleanupPage : Page
{
    private ObservableCollection<DriveModel> drives = [];

    public DiskCleanupPage()
    {
        InitializeComponent();
        _ = InitializeDrives();
    }

    private async Task InitializeDrives()
    {
        drives = await GetDrives();
        DrivesRepeater.ItemsSource = drives;
    }

    private static string FormatSize(double sizeGiB)
    {
        if (sizeGiB < 1) return $"{sizeGiB * 1024:N2} MiB";
        if (sizeGiB >= 1024) return $"{sizeGiB / 1024:N2} TiB";
        return $"{sizeGiB:N2} GiB";
    }

    private static async Task<ObservableCollection<DriveModel>> GetDrives()
    {
        var result = new ObservableCollection<DriveModel>();

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            double totalGiB = drive.TotalSize / 1073741824d;
            double freeGiB = drive.TotalFreeSpace / 1073741824d;

            var model = new DriveModel
            {
                Name = drive.Name.TrimEnd('\\'),
                Label = $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})",
                Total = totalGiB,
                Free = $"{FormatSize(freeGiB)} free of {FormatSize(totalGiB)}",
                Used = totalGiB - freeGiB
            };

            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(drive.Name);
                using var thumb = await folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 32, ThumbnailOptions.UseCurrentScale);
                if (thumb != null)
                {
                    var bmp = new BitmapImage();
                    await bmp.SetSourceAsync(thumb);
                    model.Icon = bmp;
                }
            }
            catch { }

            result.Add(model);
        }

        return result;
    }

    private void UpdateDrives()
    {
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var model = drives.FirstOrDefault(d => d.Name == drive.Name.TrimEnd('\\'));
            if (model == null) continue;

            double totalGiB = drive.TotalSize / 1073741824d;
            double freeGiB = drive.TotalFreeSpace / 1073741824d;

            model.Total = totalGiB;
            model.Used = totalGiB - freeGiB;
            model.Free = $"{FormatSize(freeGiB)} free of {FormatSize(totalGiB)}";
        }
    }

    private async void RunDiskCleanup_Checked(object sender, RoutedEventArgs e)
    {
        // clean temp directories
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Logs""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Panther""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SoftwareDistribution""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\LogFiles\*.*""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\SleepStudy\*.*""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\sru""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\WDI\*.*""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\winevt\Logs\*.*""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SystemTemp\*.*""");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Temp\*.*""");
        await ProcessActions.RunNsudo("CurrentUser", @"cmd /c del /s /f /q %temp%\*.*");
        await ProcessActions.RunNsudo("CurrentUser", @"cmd /c rd /s /q %temp%");
        await ProcessActions.RunNsudo("CurrentUser", @"cmd /c md %temp%");
        await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c del /f /q ""C:\DumpStack.log""");

        // run disk cleanup
        await Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr", Arguments = "/sagerun:0" })!.WaitForExitAsync();

        CleanDisks.IsChecked = false;

        UpdateDrives();
    }

    private void RunDiskCleanup_Unchecked(object sender, RoutedEventArgs e)
    {
        foreach (var proc in Process.GetProcessesByName("cleanmgr"))
            proc.Kill(true);

        UpdateDrives();
    }
}

public partial class DriveModel : INotifyPropertyChanged
{
    private double total;
    private double used;
    private string free = "";
    private ImageSource icon;

    public string Name { get; set; }
    public string Label { get; set; }

    public double Total
    {
        get => total;
        set { total = value; OnPropertyChanged(nameof(Total)); }
    }

    public double Used
    {
        get => used;
        set { used = value; OnPropertyChanged(nameof(Used)); }
    }

    public ImageSource Icon
    {
        get => icon;
        set { icon = value; OnPropertyChanged(nameof(Icon)); }
    }

    public string Free
    {
        get => free;
        set { free = value; OnPropertyChanged(nameof(Free)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}