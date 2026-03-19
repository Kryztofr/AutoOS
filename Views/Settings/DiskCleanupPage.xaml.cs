using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.FileProperties;
using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Registry;

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
        await Task.WhenAll(Process.GetProcessesByName("TiWorker").Select(async process => { process.Kill(); await process.WaitForExitAsync(); }));

        RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () =>
        {
            ProcessActions.CleanDirectory(@"C:\Windows\Logs");
            ProcessActions.CleanDirectory(@"C:\Windows\Panther");
            ProcessActions.CleanDirectory(@"C:\Windows\SoftwareDistribution");
            ProcessActions.CleanDirectory(@"C:\Windows\System32\LogFiles");
            ProcessActions.CleanDirectory(@"C:\Windows\System32\SleepStudy");
            ProcessActions.CleanDirectory(@"C:\Windows\System32\sru");
            ProcessActions.CleanDirectory(@"C:\Windows\System32\WDI");
            ProcessActions.CleanDirectory(@"C:\Windows\System32\winevt\Logs");
            ProcessActions.CleanDirectory(@"C:\Windows\SystemTemp");
            ProcessActions.CleanDirectory(@"C:\Windows\Temp");
            ProcessActions.CleanDirectory(Path.GetTempPath());
            File.Delete(@"C:\DumpStack.log");
        });

        // run disk cleanup
        await Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr.exe", Arguments = "/sagerun:0", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync();

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