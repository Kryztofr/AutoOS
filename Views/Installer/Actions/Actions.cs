using AutoOS.Helpers.Device;
using AutoOS.Helpers.GPU;
using AutoOS.Helpers.Monitor;
using AutoOS.Helpers.RAM;
using AutoOS.Views.Installer.Stages;
using Downloader;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using WinRT.Interop;
using System.Net.Http.Headers;

namespace AutoOS.Views.Installer.Actions;

public static class ProcessActions
{
    public static IntPtr WindowHandle { get; private set; }
    private static readonly HttpClient httpClient = new(new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    })
    {
        DefaultRequestHeaders =
        {
            UserAgent =
            {
                new ProductInfoHeaderValue("AutoOS", ProcessInfoHelper.Version)
            }
        }
    };

    public static async Task RunPowerShell(string command)
    {
        await Process.Start(new ProcessStartInfo("powershell.exe", @$"-Command ""{command}""") { CreateNoWindow = true, UseShellExecute = false }).WaitForExitAsync();
    }

    public static async Task RunConnectionCheck()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Info.Severity = InfoBarSeverity.Warning;
        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Paused);
        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];

        await Task.Delay(1000);

        while (true)
        {
            try
            {
                var response = await httpClient.GetAsync("http://www.google.com");
                if (response.IsSuccessStatusCode)
                {
                    InstallPage.Info.Severity = InfoBarSeverity.Informational;
                    InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                    Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                    InstallPage.ProgressRingControl.Foreground = null;
                    InstallPage.Info.Title = "Internet connection successfully established...";
                    await Task.Delay(500);
                    break;
                }
            }
            catch
            {

            }
        }
    }

    public static async Task RunPowerShellScript(string script, string arguments)
    {
        await Process.Start(new ProcessStartInfo("powershell.exe", @$"-ExecutionPolicy Bypass -File ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script)}"" {arguments}") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }

    public static async Task RunDownload(string url, string path, string file = null, ProgressButton progressButton = null)
    {
        var uiContext = SynchronizationContext.Current;

        string title = progressButton == null ? InstallPage.Info?.Title ?? string.Empty : string.Empty;

        DownloadBuilder downloadBuilder;
        DownloadConfiguration config;

        if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            using var client = new HttpClient();
            string dest = string.IsNullOrWhiteSpace(file) ? path : Path.Combine(path, file);
            await File.WriteAllTextAsync(dest, await client.GetStringAsync(url), Encoding.UTF8);
            return;
        }

        if (url.Contains("www2.ati.com", StringComparison.OrdinalIgnoreCase))
        {
            config = new DownloadConfiguration
            {
                RequestConfiguration = new RequestConfiguration
                {
                    Headers = new WebHeaderCollection
                {
                    { "Referer", "http://support.amd.com" },
                    { "Accept", "*/*" },
                    { "User-Agent", "AMD Catalyst Install Manager/0.0" },
                    { "Cache-Control", "no-cache" },
                    { "Connection", "Keep-Alive" }
                }
                }
            };

            downloadBuilder = DownloadBuilder.New()
                .WithUrl(url)
                .WithDirectory(path)
                .WithFileName(file)
                .WithConfiguration(config);
        }
        else
        {
            downloadBuilder = DownloadBuilder.New()
                .WithUrl(url)
                .WithDirectory(path)
                .WithFileName(file)
                .WithConfiguration(new DownloadConfiguration());
        }

        var download = downloadBuilder.Build();

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
                if (progressButton != null)
                {
                    progressButton.IsIndeterminate = false;
                    progressButton.Progress = percentage;
                }
                else if (!string.IsNullOrEmpty(title))
                {
                    InstallPage.Info.Title = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";
                    InstallPage.ProgressRingControl.IsIndeterminate = false;
                    InstallPage.ProgressRingControl.Value = percentage;
                }
            }, null);
        };

        download.DownloadFileCompleted += (sender, e) =>
        {
            uiContext?.Post(_ =>
            {
                if (progressButton != null)
                {
                    progressButton.Progress = 100;
                    progressButton.IsIndeterminate = true;
                }
                else if (!string.IsNullOrEmpty(title))
                {
                    InstallPage.Info.Title = $"{title} ({speedMB:F1} MB/s - {totalMB:F2} MB of {totalMB:F2} MB)";
                    InstallPage.ProgressRingControl.Value = 100;
                    InstallPage.ProgressRingControl.IsIndeterminate = true;
                }
            }, null);
        };

        await download.StartAsync();
    }

    public static async Task RunExtract(string inputPath, string outputPath)
    {
        await Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7z.exe"), Arguments = $"x \"{inputPath}\" -y -o\"{outputPath}\"", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task<string> GetLatestObsStudioUrl()
    {
        string json = await httpClient.GetStringAsync("https://api.github.com/repos/obsproject/obs-studio/releases/latest");
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("assets")
            .EnumerateArray()
            .First(a => a.GetProperty("name").GetString().Contains("Windows-x64-Installer.exe"))
            .GetProperty("browser_download_url")
            .GetString();
    }

    public static async Task PatchStartAllBack()
    {
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 0, RegistryValueKind.DWord);

        foreach (var name in new[] { "explorer", "StartAllBackCfg" })
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                process.Kill(); 
                await process.WaitForExitAsync();
            }
        }

        string dll = @"StartAllBack\StartAllBackX64.dll";
        var paths = new[] {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dll),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), dll),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), dll)
        }.Where(File.Exists);

        foreach (var path in paths)
        {
            string bak = path + ".bak";
            string old = path + ".old";

            if (File.Exists(bak))
            {
                try { if (File.Exists(old)) File.Delete(old); File.Move(path, old); } catch { }
                File.Copy(bak, path, true);
            }
            else
            {
                File.Copy(path, bak, true);
                byte[] b = File.ReadAllBytes(path);
                byte[] pt = { 0x48, 0x89, 0x5C, 0x24, 0x08, 0x55, 0x56, 0x57, 0x48, 0x8D, 0xAC, 0x24, 0x70, 0xFF, 0xFF, 0xFF };
                byte[] ph = { 0x67, 0xC7, 0x01, 0x01, 0x00, 0x00, 0x00, 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90, 0x90, 0x90 };

                for (int i = 0; i <= b.Length - pt.Length; i++)
                {
                    if (b.Skip(i).Take(pt.Length).SequenceEqual(pt))
                    {
                        Buffer.BlockCopy(ph, 0, b, i, ph.Length);

                        try
                        {
                            File.WriteAllBytes(path, b);
                        }
                        catch (IOException)
                        {
                            if (File.Exists(old)) File.Delete(old);
                            File.Move(path, old);
                            File.WriteAllBytes(path, b);
                        }
                        break;
                    }
                }
            }
            try 
            { 
                if (File.Exists(old)) 
                    File.Delete(old); 
            } catch { }
        }

        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 1, RegistryValueKind.DWord);
    }

    public static void CleanDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    File.Delete(file);

                foreach (string dir in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
                    Directory.Delete(dir, true);
            }
        }
        catch { }
    }

    public static async Task Log(bool bios = false)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        string installStart = localSettings.Values["Install_Start"]?.ToString() ?? "N/A";
        string installEnd = localSettings.Values["Install_End"]?.ToString() ?? "N/A";

        string cpuName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "")?.ToString() ?? "";

        string manufacturer = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", "")?.ToString() ?? "";

        string product = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";

        string motherboard = $"{manufacturer} {product}".Trim();

        string ram = $"{(RamHelper.GetRam() is var r ? $"{r.CapacityGB:N1} GB {r.DDRVersion} @ {r.MaxSpeedMHz} MHz" : "")}";

        var currentGpus = GpuHelper.GetGPUs();
        string gpus = string.Join(", ", currentGpus.Select(gpu => $"{gpu.DeviceName} (DeviceId: {gpu.DeviceId}, Install: {PreparingStage.GPUs.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId)?.Install ?? true}, {gpu.CurrentVersion})"));

        string monitors = string.Join(", ", MonitorHelper.GetMonitors().Select(m => $"{m.DeviceName} ({m.Resolution.Width}x{m.Resolution.Height} @ {m.RefreshRate} Hz)"));

        var nicsList = DeviceHelper.GetDevices(DeviceType.NIC);
        string nics = nicsList.Count > 0 ? string.Join("\n", nicsList.Select(n => $"{n.FriendlyName} (DeviceId: {n.DeviceId}, Current Version: {n.DriverType} {n.CurrentVersion}, Connected: {n.IsActive})")) : "N/A";

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        string build = key.GetValue("CurrentBuild")?.ToString() ?? "";
        string ubr = key.GetValue("UBR")?.ToString() ?? "";
        string osVersion = $"{build}.{ubr}";

        string discordId = "Failed to get Discord account id";
        string discordUsername = "Failed to get Discord username";

        string discordJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "sentry", "scope_v3.json");
        if (File.Exists(discordJsonPath))
        {
            try
            {
                string jsonText = File.ReadAllText(discordJsonPath);
                using JsonDocument doc = JsonDocument.Parse(jsonText);

                if (doc.RootElement.TryGetProperty("scope", out var scope) &&
                    scope.TryGetProperty("user", out var user))
                {
                    discordId = user.GetProperty("id").GetString() ?? discordId;
                    discordUsername = user.GetProperty("username").GetString() ?? discordUsername;
                }
            }
            catch
            {

            }
        }

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(
                $"<@{discordId}>\n" +
                $"{discordUsername}\n" +
                $"{motherboard}\n" +
                $"{cpuName}\n" +
                $"{ram}\n" +
                $"{gpus}\n" +
                $"{monitors}\n" +
                $"{nics}\n" +
                $"{osVersion}\n" +
                $"Install start: {installStart}\n" +
                $"Install end: {installEnd}\n" +
                $"{ProcessInfoHelper.Version}"
            ), "content" }
        };

        if (bios)
            multipart.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt"))), "file", Path.GetFileName(Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt")));

        string webhook = bios ? "https://discord.com/api/webhooks/1444743392868172016/1kq532maWmIguJEO-rp-X4RHG1idpbjKFWHC7IYwxr6KLEZxjhrJhwftYeeRKfKDYB-a" : "https://discord.com/api/webhooks/1444743483486240860/V_myd24FjH7TNJPruYbNJcnuE9Xany7C-tAScpygDV_FOGnwmuamSuOgXdxlts1Q2MhM";

        await httpClient.PostAsync(webhook, multipart);
    }

    public static async Task LogError(Exception ex)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        string installStart = localSettings.Values["Install_Start"]?.ToString() ?? "N/A";
        string installEnd = localSettings.Values["Install_End"]?.ToString() ?? "N/A";

        string cpuName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "")?.ToString() ?? "";

        string manufacturer = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", "")?.ToString() ?? "";

        string product = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";

        string motherboard = $"{manufacturer} {product}".Trim();

        string ram = $"{(RamHelper.GetRam() is var r ? $"{r.CapacityGB:N1} GB {r.DDRVersion} @ {r.MaxSpeedMHz} MHz" : "")}";

        var currentGpus = GpuHelper.GetGPUs();
        string gpus = string.Join(", ", currentGpus.Select(gpu => $"{gpu.DeviceName} (DeviceId: {gpu.DeviceId}, Install: {PreparingStage.GPUs.FirstOrDefault(x => x.PnPDeviceId == gpu.PnPDeviceId)?.Install ?? true}, {gpu.CurrentVersion})"));

        string monitors = string.Join(", ", MonitorHelper.GetMonitors().Select(m => $"{m.DeviceName} ({m.Resolution.Width}x{m.Resolution.Height} @ {m.RefreshRate} Hz)"));

        var nicsList = DeviceHelper.GetDevices(DeviceType.NIC);
        string nics = nicsList.Count > 0 ? string.Join("\n", nicsList.Select(n => $"{n.FriendlyName} (DeviceId: {n.DeviceId}, VendorId: {n.VendorId}, Current Version: {n.DriverType} {n.CurrentVersion}, Connected: {n.IsActive})")) : "N/A";

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        string build = key.GetValue("CurrentBuild")?.ToString() ?? "";
        string ubr = key.GetValue("UBR")?.ToString() ?? "";
        string osVersion = $"{build}.{ubr}";

        string discordId = "Failed to get Discord account id";
        string discordUsername = "Failed to get Discord username";

        string discordJsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "sentry", "scope_v3.json");
        if (File.Exists(discordJsonPath))
        {
            try
            {
                string jsonText = File.ReadAllText(discordJsonPath);
                using JsonDocument doc = JsonDocument.Parse(jsonText);

                if (doc.RootElement.TryGetProperty("scope", out var scope) &&
                    scope.TryGetProperty("user", out var user))
                {
                    discordId = user.GetProperty("id").GetString() ?? discordId;
                    discordUsername = user.GetProperty("username").GetString() ?? discordUsername;
                }
            }
            catch
            {

            }
        }

        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(
                $"<@{discordId}>\n" +
                $"{discordUsername}\n" +
                $"{motherboard}\n" +
                $"{cpuName}\n" +
                $"{ram}\n" +
                $"{gpus}\n" +
                $"{monitors}\n" +
                $"{nics}\n" +
                $"{osVersion}\n" +
                $"Install start: {installStart}\n" +
                $"Install end: {installEnd}\n" +
                $"{ex.GetType().FullName}\n" +
                $"Message: {ex.Message}\n" +
                $"HResult: 0x{ex.HResult:X}\n" +
                $"Source: {ex.Source}\n" +
                $"StackTrace:\n{ex.StackTrace}\n" +
                (ex.InnerException != null ? $"\nInnerException:\n{ex.InnerException}" : "") +
                $"\n{ProcessInfoHelper.Version}"
            ), "content" }
        };

        await httpClient.PostAsync("https://discord.com/api/webhooks/1474078669596131409/Ha9bZsk1MZQRwuTrGWYpw1nYsL7OiPsi21BrRAaVoNlgjlFUOTtb1g2xgoZEfj6IT-Lc", multipart);
    }
}