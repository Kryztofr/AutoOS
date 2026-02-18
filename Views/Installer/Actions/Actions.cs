using AutoOS.Helpers.Games;
using Downloader;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ValveKeyValue;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Actions;

public static class ProcessActions
{
    public static IntPtr WindowHandle { get; private set; }

    public static async Task RunNsudo(string user, string command)
    {
        string arguments = user switch
        {
            "TrustedInstaller" => $"-U:T -P:E -Wait -ShowWindowMode:Hide {command}",
            "CurrentUser" => $"-U:P -P:E -Wait -ShowWindowMode:Hide {command}",
            _ => throw new ArgumentException("Invalid user specified.", nameof(user))
        };

        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe"), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunRestart()
    {
        InstallPage.Info.Title = "Restarting in 3...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 2...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting in 1...";
        await Task.Delay(1000);
        InstallPage.Info.Title = "Restarting...";
        await Task.Delay(750);
        ProcessStartInfo processStartInfo = new()
        {
            FileName = "cmd.exe",
            Arguments = $"/c shutdown /r /t 0",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(processStartInfo);
    }

    public static async Task RunPowerShell(string command)
    {
        await Process.Start(new ProcessStartInfo("powershell.exe", $"-Command \"{command}\"") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }

    public static async Task RunConnectionCheck()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        InstallPage.Info.Severity = InfoBarSeverity.Warning;
        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Paused);
        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];

        await Task.Delay(1000);

        using (var httpClient = new HttpClient())
        {
            while (true)
            {
                try
                {
                    var response = await httpClient.GetAsync("http://www.google.com");
                    if (response.IsSuccessStatusCode)
                    {
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
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
    }

    public static async Task RunBatchScript(string script, string arguments)
    {
        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RunPowerShellScript(string script, string arguments)
    {
        await Process.Start(new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", script)}\" {arguments}") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExitAsync();
    }

    public static async Task RunApplication(string folderName, string executable, string arguments)
    {
        await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", folderName, executable), arguments) { CreateNoWindow = true })!.WaitForExitAsync();
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
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoOS");

        string json = await client.GetStringAsync("https://api.github.com/repos/obsproject/obs-studio/releases/latest");
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("assets")
            .EnumerateArray()
            .First(a => a.GetProperty("name").GetString().Contains("Windows-x64-Installer.exe"))
            .GetProperty("browser_download_url")
            .GetString();
    }

    public static async Task LogAdvancedNetworkSettings()
    {
        var cpuObj = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor")
                            .Get()
                            .Cast<ManagementObject>()
                            .FirstOrDefault();
        string cpuName = cpuObj?["Name"]?.ToString() ?? "";

        var boardObj = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard")
                          .Get()
                          .Cast<ManagementObject>()
                          .FirstOrDefault();
        string motherboard = boardObj != null ? $"{boardObj["Manufacturer"]} {boardObj["Product"]}" : "";

        var gpuObjs = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController")
                  .Get()
                  .Cast<ManagementObject>();
        string gpus = string.Join(", ", gpuObjs.Select(g => g["Name"]?.ToString() ?? ""));

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        string build = key.GetValue("CurrentBuild")?.ToString() ?? "";
        string ubr = key.GetValue("UBR")?.ToString() ?? "";
        string osVersion = $"{build}.{ubr}";

        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = @"
            Get-NetAdapter | ForEach-Object { 
                $adapter = $_
                Get-NetAdapterAdvancedProperty -Name $adapter.Name | 
                    Select-Object @{Name='Adapter';Expression={$adapter.InterfaceDescription}}, Name, DisplayName, DisplayValue, RegistryKeyword, RegistryValue |
                    Format-Table -Wrap:$false | Out-String -Width 4096
            }",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        string psOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        using var client = new HttpClient();
        using var form = new MultipartFormDataContent
        {
            { new StringContent($"{cpuName}\n{motherboard}\n{gpus}\n{osVersion}\n{ProcessInfoHelper.Version}"), "content" },
            { new ByteArrayContent(Encoding.UTF8.GetBytes(psOutput.TrimStart('\r', '\n'))), "file", "advancednetworksettings.txt" }
        };

        await client.PostAsync("https://discord.com/api/webhooks/1444743232679579779/kY5L3BixE536ykBsk5t4ymdkrBn0EvqN4YAYAkFwi-wDP1uQOkZinTy_HgD__UptnGMM", form);
    }

    public static async Task RemoveAppx(string appx)
    {
        await Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"Get-AppxPackage \"{appx}\" | Remove-AppxPackage", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task RemoveAppxProvisioned(string appx)
    {
        await Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"Remove-AppxProvisionedPackage -PackageName (Get-AppxProvisionedPackage -Online | Where-Object {{ ('{appx}' -contains $_.DisplayName) }}).PackageName -Online -AllUsers", CreateNoWindow = true })!.WaitForExitAsync();
    }

    public static async Task UpdateAppx(string appx)
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "updateappx.ps1")}\" \"{appx}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        using (Process process = Process.Start(processStartInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    InstallPage.ProgressRingControl.IsIndeterminate = false;
                    InstallPage.ProgressRingControl.Value = Convert.ToDouble(line);
                }
            }

            await process.WaitForExitAsync();
            InstallPage.ProgressRingControl.IsIndeterminate = true;
            InstallPage.ProgressRingControl.Value = 0;
        }
    }

    public static async Task DisableScheduledTasks()
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "disablescheduledtasks.ps1")}\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe")}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        using (Process process = Process.Start(processStartInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (double.TryParse(line, out double progress))
                    {
                        InstallPage.ProgressRingControl.IsIndeterminate = false;
                        InstallPage.ProgressRingControl.Value = progress;
                    }
                }
            }

            await process.WaitForExitAsync();
            InstallPage.ProgressRingControl.IsIndeterminate = true;
            InstallPage.ProgressRingControl.Value = 0;
        }
    }

    public static async Task RunMicrosoftStoreDownload(string productFamilyName, string catalogId, string fileType, int index, bool dependencies)
    {
        string title = InstallPage.Info.Title;
        string output = "";
        string errorOutput = "";
        var uiContext = SynchronizationContext.Current;

        while (true)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo("powershell.exe", @$"-ExecutionPolicy Bypass -File ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "getmicrosoftstorelink.ps1")}"" {productFamilyName} {catalogId} {fileType} {index}{(dependencies ? " -Dependencies" : "")}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(stdOutTask, stdErrTask);
                output = stdOutTask.Result;
                errorOutput = stdErrTask.Result;

                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(errorOutput))
                {
                    uiContext?.Post(_ => InstallPage.Info.Title = title, null);
                    break;
                }
            }
            catch
            { }

            for (int i = 30; i >= 0; i--)
            {
                int secondsLeft = i;
                uiContext?.Post(_ => InstallPage.Info.Title = $"{title} - Download failed, retrying in {secondsLeft}s", null);
                await Task.Delay(1000);
            }

            uiContext?.Post(_ => InstallPage.Info.Title = $"{title} - Retrying now...", null);
        }

        uiContext?.Post(_ => InstallPage.Info.Title = title, null);

        string folderName = $"{productFamilyName} {(dependencies ? "(Dependencies)" : "(Package)")}";
        string downloadFolder = Path.Combine(Path.GetTempPath(), folderName);
        Directory.CreateDirectory(downloadFolder);

        if (dependencies)
        {
            string[] urls = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            foreach (string url in urls)
            {
				InstallPage.Info.Title = title;
				await RunDownload(url.Trim(), downloadFolder);
			}
		}
        else
        {
            string url = output.Trim();
            await RunDownload(url, downloadFolder);
        }
    }

    public static async Task RunImportEpicGamesLauncherAccount()
    {
        // get all configs from other drives
        var foundFiles = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
            .SelectMany(d =>
            {
                string usersPath = Path.Combine(d.Name, "Users");
                if (!Directory.Exists(usersPath)) return Array.Empty<string>();

                return Directory.GetDirectories(usersPath)
                    .Select(userDir =>
                        File.Exists(Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "WindowsEditor", "GameUserSettings.ini"))
                        ? Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "WindowsEditor", "GameUserSettings.ini")
                        : Path.Combine(userDir, "AppData", "Local", "EpicGamesLauncher", "Saved", "Config", "Windows", "GameUserSettings.ini")
                    )
                    .Where(File.Exists);
            })
            .Select(path => new FileInfo(path))
            .ToList();

        string newestFilePath = null;

        // check if files are valid
        foreach (var file in foundFiles)
        {
            string configContent = await File.ReadAllTextAsync(file.FullName);
            Match dataMatch = Regex.Match(configContent, @"Data=([^\r\n]+)");

            if (EpicGamesHelper.ValidateData(file.FullName))
            {
                // use the latest one
                if (newestFilePath == null || file.LastWriteTime > new FileInfo(newestFilePath).LastWriteTime)
                {
                    newestFilePath = file.FullName;

                    // copy the file
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EpicGamesLauncher\Saved\Config\WindowsEditor"));
                    //Directory.CreateDirectory(EpicGamesHelper.EpicGamesAccountDir);
                    File.Copy(newestFilePath, EpicGamesHelper.ActiveEpicGamesAccountPath, true);

                    // disable tray and notifications
                    EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
                    EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

                    // get accountId
                    string accountId = EpicGamesHelper.GetAccountData(file.FullName).AccountId;

                    // create backup folder
                    Directory.CreateDirectory(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId));

                    // copy config
                    File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);

                    // create reg file
                    File.WriteAllText(Path.Combine(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId), "accountId.reg"), $"Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\Software\\Epic Games\\Unreal Engine\\Identifiers]\r\n\"AccountId\"=\"{accountId}\"");

                    // update refresh token
                    await EpicGamesHelper.UpdateEpicGamesToken(EpicGamesHelper.ActiveEpicGamesAccountPath);

                    // update the backed up config
                    File.Copy(file.FullName, Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId, "GameUserSettings.ini"), true);

                    InstallPage.Info.Title = $"Succesfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}...";

                    await Task.Delay(1000);

                    return;
                }
            }
        }
    }

    public static async Task EpicGamesLogin()
    {
        // launch epic games launcher
        Process.Start(EpicGamesHelper.EpicGamesPath);

        // check when logged in
        while (true)
        {
            if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
                {
                    await Task.Delay(1000);

                    // close epic games launcher
                    EpicGamesHelper.CloseEpicGames();

                    // disable tray and notifications
                    EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
                    EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

                    InstallPage.Info.Title = $"Succesfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}...";
                    break;
                }
            }

            if (Process.GetProcessesByName("EpicGamesLauncher").Length == 0)
            {
                // disable tray and notifications
                EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
                EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);
                break;
            }

            await Task.Delay(500);
        }

        await Task.Delay(1000);
    }

    public static async Task UpdateInvalidEpicGamesToken()
    {
        InstallPage.Info.Title = "The refresh token is no longer valid. Please enter your password again...";

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // delay
        await Task.Delay(500);

        // launch epic games launcher
        Process.Start(EpicGamesHelper.EpicGamesPath);

        // check when logged in
        while (true)
        {
            if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
                {
                    break;
                }
            }

            await Task.Delay(500);
        }

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // disable tray and notifications
        EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
        EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

        InstallPage.Info.Title = $"Succesfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}...";

        await Task.Delay(1000);
    }

    public static async Task RunImportEpicGamesLauncherGames()
    {
        // get all install lists from other drives
        var foundFiles = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
            .Select(d => Path.Combine(d.Name, "ProgramData", "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat"))
            .Where(File.Exists)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        if (foundFiles.Count == 0)
            return;

        FileInfo newestFile = foundFiles.First();

        var jsonContent = await File.ReadAllTextAsync(newestFile.FullName);

        if (string.IsNullOrWhiteSpace(jsonContent))
            return;

        var jsonObject = JsonNode.Parse(jsonContent);

        // return if install list is empty
        if (jsonObject?["InstallationList"] is not JsonArray installationList || installationList.Count == 0)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(EpicGamesHelper.EpicGamesInstalledGamesPath)!);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // set new game paths
        foreach (var game in installationList)
        {
            if (game is JsonObject gameObj && gameObj.ContainsKey("InstallLocation"))
            {
                string originalPath = gameObj["InstallLocation"]!.ToString();
                string originalDrive = Path.GetPathRoot(originalPath) ?? "";
                string relativePath = originalPath[originalDrive.Length..];

                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\"))
                {
                    string testPath = Path.Combine(drive.Name, relativePath);
                    if (Directory.Exists(testPath))
                    {
                        gameObj["InstallLocation"] = testPath.Replace('\\', '/');
                        break;
                    }
                }
            }
        }

        await File.WriteAllTextAsync(EpicGamesHelper.EpicGamesInstalledGamesPath, jsonObject.ToJsonString(jsonOptions));

        // copy over the manifest folder
        string sourceManifestsFolder = Path.Combine(Path.GetPathRoot(newestFile.FullName)!, "ProgramData", "Epic", "EpicGamesLauncher", "Data", "Manifests");

        if (!Directory.Exists(sourceManifestsFolder))
            return;

        Directory.CreateDirectory(EpicGamesHelper.EpicGamesManifestDir);

        foreach (var directory in Directory.GetDirectories(sourceManifestsFolder, "*", SearchOption.AllDirectories))
        {
            string subDirPath = directory.Replace(sourceManifestsFolder, EpicGamesHelper.EpicGamesManifestDir);
            Directory.CreateDirectory(subDirPath);
        }

        foreach (var file in Directory.GetFiles(sourceManifestsFolder, "*.*", SearchOption.AllDirectories))
        {
            string destFilePath = file.Replace(sourceManifestsFolder, EpicGamesHelper.EpicGamesManifestDir);
            File.Copy(file, destFilePath, true);
        }

        // set new game paths
        foreach (var file in Directory.GetFiles(EpicGamesHelper.EpicGamesManifestDir, "*.item", SearchOption.AllDirectories))
        {
            var itemJson = JsonNode.Parse(await File.ReadAllTextAsync(file));

            if (itemJson is JsonObject itemObj && itemObj.ContainsKey("InstallLocation"))
            {
                string originalInstallLocation = itemObj["InstallLocation"]!.ToString().Replace('\\', '/');
                string originalDrive = Path.GetPathRoot(originalInstallLocation)?.Replace('\\', '/') ?? "";
                string relativePath = originalInstallLocation.Substring(originalDrive.Length);

                string? newInstallLocation = null;

                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\"))
                {
                    string testPath = Path.Combine(drive.Name, relativePath);
                    if (Directory.Exists(testPath))
                    {
                        newInstallLocation = testPath.Replace('\\', '/');
                        break;
                    }
                }

                if (newInstallLocation != null)
                {
                    itemObj["InstallLocation"] = newInstallLocation;

                    string oldRoot = originalDrive;
                    string newRoot = Path.GetPathRoot(newInstallLocation)!.Replace('\\', '/');

                    if (itemObj.ContainsKey("ManifestLocation"))
                    {
                        string manifest = itemObj["ManifestLocation"]!.ToString().Replace('\\', '/');
                        itemObj["ManifestLocation"] = manifest.Replace(oldRoot, newRoot);
                    }

                    if (itemObj.ContainsKey("StagingLocation"))
                    {
                        string staging = itemObj["StagingLocation"]!.ToString().Replace('\\', '/');
                        itemObj["StagingLocation"] = staging.Replace(oldRoot, newRoot);
                    }

                    await File.WriteAllTextAsync(file, itemObj.ToJsonString(new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }));
                }
            }
        }

        // launch epic games to get new token
        await Task.Run(() => Process.Start(new ProcessStartInfo(EpicGamesHelper.EpicGamesPath) { WindowStyle = ProcessWindowStyle.Hidden }));

        // wait for token to get used
        while (true)
        {
            await Task.Delay(100);

            if (!EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                await UpdateInvalidEpicGamesToken();
                return;
            }

            if (EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).TokenUseCount == 1)
                break;
        }

        // wait for new token
        while (true)
        {
            await Task.Delay(100);

            if (!EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
            {
                await UpdateInvalidEpicGamesToken();
                return;
            }

            if (EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).TokenUseCount == 0)
                break;
        }

        // close epic games launcher
        EpicGamesHelper.CloseEpicGames();

        // update the backed up config
        File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(EpicGamesHelper.EpicGamesAccountDir, EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).AccountId, "GameUserSettings.ini"), true);
    }

    public static async Task SteamLogin()
    {
        // launch steam
        Process.Start(SteamHelper.SteamPath);

        // check when logged in
        while (true)
        {
            if (File.Exists(SteamHelper.SteamLoginUsersPath))
            {
                if (KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath)))).Children.Count() > 0)
                {
                    // close steam
                    SteamHelper.CloseSteam();

                    var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                                         .Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(SteamHelper.SteamLoginUsersPath))));

                    InstallPage.Info.Title = $"Successfully logged in as {kv.Children.Select(c => c["AccountName"]?.ToString()).FirstOrDefault(name => !string.IsNullOrEmpty(name))}...";
                    break;
                }
                    
            }

            if (Process.GetProcessesByName("steam").Length == 0)
            {
                break;
            }

            await Task.Delay(500);
        }

        
        await Task.Delay(1000);
    }

    public static async Task RunImportSteamGames()
    {
        var foundFiles = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
            .Select(d => Path.Combine(d.Name, "Program Files (x86)", "Steam", "steamapps", "libraryfolders.vdf"))
            .Where(File.Exists)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        if (foundFiles.Count == 0)
            return;

        var newestFile = foundFiles.First();

        string sourceDrive = Path.GetPathRoot(newestFile.FullName)?.TrimEnd('\\') ?? "";
        string targetDrive = @"C:\";

        string sourceCacheDir = SteamHelper.SteamLibraryCacheDir.Replace(Path.GetPathRoot(SteamHelper.SteamLibraryCacheDir) ?? "", sourceDrive + @"\");
        string targetCacheDir = SteamHelper.SteamLibraryCacheDir.Replace(Path.GetPathRoot(SteamHelper.SteamLibraryCacheDir) ?? "", targetDrive);

        Directory.CreateDirectory(targetCacheDir);

        foreach (var dir in Directory.GetDirectories(sourceCacheDir, "*", SearchOption.AllDirectories))
        {
            var targetDir = dir.Replace(sourceCacheDir, targetCacheDir);
            Directory.CreateDirectory(targetDir);
        }

        foreach (var file in Directory.GetFiles(sourceCacheDir, "*.*", SearchOption.AllDirectories))
        {
            var targetFile = file.Replace(sourceCacheDir, targetCacheDir);
            File.Copy(file, targetFile, true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(SteamHelper.SteamLibraryPath));

        var libraryFolderData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(File.OpenRead(newestFile.FullName));

        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.Name != @"C:\")
            .Select(d => d.Name.TrimEnd('\\'))
            .ToList();

        List<KVObject> newFolders = [.. libraryFolderData.Children.Select(folder =>
        {
            var children = folder.Children.ToList();

            var pathNode = children.FirstOrDefault(c => c.Name == "path");
            if (pathNode != null)
            {
                children.Remove(pathNode);
                children.Insert(0, pathNode);

				string pathValue = pathNode.Value?.ToString() ?? "";

                string folderSuffix = (pathValue.Length > 2 && pathValue[1] == ':') ? pathValue.Substring(2) : "";

	            string foundPath = drives.FirstOrDefault(drive => Directory.Exists(drive + folderSuffix)) is string fPath ? fPath + folderSuffix : null;

	            if (foundPath != null)
		            children[0] = new KVObject("path", foundPath);
			}

            return new KVObject(folder.Name, children);
        })];

        using var msOut = new MemoryStream();
        KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, new KVObject(libraryFolderData.Name, newFolders));
        msOut.Position = 0;
        File.WriteAllText(SteamHelper.SteamLibraryPath, new StreamReader(msOut).ReadToEnd());

        await Task.Delay(1000);
    }
}