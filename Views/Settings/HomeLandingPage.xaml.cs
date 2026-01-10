using AutoOS.Views.Installer.Actions;
using CommunityToolkit.WinUI.Controls;
using Downloader;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace AutoOS.Views.Settings
{
    public sealed partial class HomeLandingPage : Page
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static readonly HttpClient httpClient = new();
        private readonly TextBlock StatusText = new()
        {
            Margin = new Thickness(0, 12, 0, 0),
            FontSize = 14,
            FontWeight = FontWeights.Medium
        };

        private readonly ProgressBar ProgressBar = new()
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        public HomeLandingPage()
        {
            InitializeComponent();
#if !DEBUG
            Loaded += GetChangeLog;
#endif
        }

        private async void GetChangeLog(object sender, RoutedEventArgs e)
        {
            string storedVersion = localSettings.Values["Version"] as string;
            string currentVersion = ProcessInfoHelper.Version;

            if (storedVersion != currentVersion)
            {
                try
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AutoOS");

                    using var doc = JsonDocument.Parse(await httpClient.GetStringAsync($"https://api.github.com/repos/tinodin/AutoOS/releases/tags/v{currentVersion}"));

                    if (doc.RootElement.TryGetProperty("body", out var body))
                    {
                        string rawChangelog = body.GetString()!;
                        string changelog = rawChangelog.Replace("`", "")[rawChangelog.IndexOf("- ")..];

                        var contentDialog = new ContentDialog
                        {
                            Title = $"What’s new in AutoOS v{currentVersion}",
                            Content = new ScrollViewer
                            {
                                Content = new MarkdownTextBlock
                                {
                                    Text = changelog,
                                    Config = new MarkdownConfig()
                                },
                                Padding = new Thickness(0, 0, 36, 0)
                            },
                            CloseButtonText = "Close",
                            XamlRoot = XamlRoot
                        };

                        contentDialog.Resources["ContentDialogMaxWidth"] = 1000;
                        await contentDialog.ShowAsync();
                    }
                }
                catch
                {

                }

                await Update();

                bool originalServicesState = localSettings.Values["originalServicesState"] is bool b && b;

                if (originalServicesState == false)
                {
                    StatusText.Text = "Update will resume after a restart.";
                    localSettings.Values["originalServicesState"] = true;
                }
                else
                {
                    StatusText.Text = "Update complete.";
                    localSettings.Values["Version"] = currentVersion;
                    await LogDiscordUser();
                }

                ProgressBar.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
                await Task.Delay(1500);
                StatusText.Text = "Restarting in 3...";
                await Task.Delay(1000);
                StatusText.Text = "Restarting in 2...";
                await Task.Delay(1000);
                StatusText.Text = "Restarting in 1...";
                await Task.Delay(1000);
                StatusText.Text = "Restarting...";
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
        }

        private async Task Update()
        {
            var updater = new ContentDialog
            {
                Title = "Updating AutoOS",
                Content = new StackPanel
                {
                    Children =
                    {
                        StatusText,
                        ProgressBar
                    }
                },
                PrimaryButtonText = "Done",
                IsPrimaryButtonEnabled = false,
                Resources = new ResourceDictionary
                {
                    ["ContentDialogMinWidth"] = 500,
                    ["ContentDialogMaxWidth"] = 1000
                },
                XamlRoot = XamlRoot
            };

            _ = updater.ShowAsync();

            string previousTitle = string.Empty;
            bool servicesState = (int)(Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Beep")?.GetValue("Start", 0) ?? 0) == 1;
            if (!localSettings.Values.ContainsKey("originalServicesState"))
            {
                localSettings.Values["originalServicesState"] = servicesState;
            }
            bool originalServicesState = localSettings.Values["originalServicesState"] is bool b && b;
            string list = Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini");
            bool listExists = File.Exists(list);
            string nsudoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "NSudo", "NSudoLC.exe");
            string storedVersion = localSettings.Values["Version"] as string;
            bool INTEL = false;
            bool AMD = false;
            InIHelper iniHelper = new InIHelper(Path.Combine(Path.GetTempPath(), "obs-studio", "basic", "profiles", "Untitled", "basic.ini"));

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString();
                    string version = obj["DriverVersion"]?.ToString();

                    if (name != null)
                    {
                        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                        {
                            AMD = true;
                        }
                        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                        {
                            INTEL = true;
                        }
                    }
                }
            }

            var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
            {
                // enable services & drivers
                ("Enabling Services & Drivers", async () => await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last(), "Services-Enable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync(), () => servicesState == false),

                // enable "do not preserve zone information in file attachments"
                (@"Enabling ""Do not preserve zone information in file attachments""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments"" /v SaveZoneInformation /t REG_DWORD /d 1 /f"), () => servicesState == true),

                // set "inclusion list for moderate risk file types"" policy to ".bat;.cmd;.vbs;.ps1;.reg;.js;.exe;.msi;"
                (@"Setting ""Inclusion list for moderate risk file types"" policy to "".bat;.cmd;.vbs;.ps1;.reg;.js;.exe;.msi;""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Associations"" /v ModRiskFileTypes /t REG_SZ /d "".bat;.cmd;.vbs;.reg;.js;.exe;.msi;"" /f"), () => servicesState == true),

                // set execution policy to unrestricted
                ("Setting execution policy to unrestricted", async () => await ProcessActions.RunPowerShell("Set-ExecutionPolicy Unrestricted -Force"), () => servicesState == true),

                // switch high performance power plan
                ("Switching to the high performance power plan", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), () => servicesState == true),

                // adjust powerplan
                (@"Setting ""Processor idle demote threshold"" to 1", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 4b92d758-5a24-4851-a470-815d78aee119 1"), null),
                (@"Setting ""Processor idle demote threshold"" to 1", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setdcvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 4b92d758-5a24-4851-a470-815d78aee119 1"), null),
                (@"Disabling ""Processor performance autonomous mode""", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 8baa4a8a-14c6-4451-8e8b-14bdbd197537 0"), null),
                (@"Disabling ""Processor performance autonomous mode""", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setdcvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 8baa4a8a-14c6-4451-8e8b-14bdbd197537 0"), null),
                (@"Setting ""Processor performance increase threshold"" to 1", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 06cadf0e-64ed-448a-8927-ce7bf90eb35d 1"), null),
                (@"Setting ""Processor performance increase threshold for Processor Power Efficiency Class 1"" to 1", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 06cadf0e-64ed-448a-8927-ce7bf90eb35e 1"), null),

                // save powerplan
                ("Saving the power plan configuration", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /setactive scheme_current"), () => servicesState == true),

                // enable some drivers
                ("Enabling some drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\dam"" /v Start /t REG_DWORD /d 1 /f"), () => servicesState == true),
                ("Enabling some drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT"" /v Start /t REG_DWORD /d 1 /f"), () => servicesState == true),
                ("Enabling some drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\tcpipreg"" /v Start /t REG_DWORD /d 2 /f"), () => servicesState == true),

                // download obs studio
                ("Downloading OBS Studio Settings", async () => await RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/gkhuws75qnckr63lnfbzn/obs-studio.zip?rlkey=6ziow6s1a85a7s5snrdi7v1x2&st=db3yzo4m&dl=0", Path.GetTempPath(), "obs-studio.zip"), null),

                // update obs studio settings
                ("Updating OBS Studio Settings", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "obs-studio.zip"), Path.Combine(Path.GetTempPath(), "obs-studio")), null),
                ("Updating OBS Studio Settings", async () => iniHelper.AddValue("Encoder", "obs_qsv11_v2", "AdvOut"), () => INTEL == true),
                ("Updating OBS Studio Settings", async () => iniHelper.AddValue("RecEncoder", "obs_qsv11_hevc", "AdvOut"), () => INTEL == true),
                ("Updating OBS Studio Settings", async () => iniHelper.AddValue("Encoder", "h265_texture_amf", "AdvOut"), () => AMD == true),
                ("Updating OBS Studio Settings", async () => iniHelper.AddValue("RecEncoder", "h265_texture_amf", "AdvOut"), () => AMD == true),
                ("Updating OBS Studio Settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c xcopy ""%TEMP%\obs-studio\*"" ""%APPDATA%\obs-studio\"" /s /y /i"), null),

                // remove static ip
                ("Removing static ip", async () => await ProcessActions.RunPowerShell(@"Get-NetIPInterface -AddressFamily IPv4 | ForEach-Object { netsh interface ipv4 set address name=\""$($_.InterfaceAlias)\"" source=dhcp; netsh interface ipv4 set dnsservers name=\""$($_.InterfaceAlias)\"" source=dhcp }"), () => servicesState == true),

                // adjust ethernet adapter advanced settings
                ("Adjusting Ethernet adapter advanced settings", async () => await ProcessActions.RunPowerShellScript("ethernet.ps1", ""), () => servicesState == true),

                // enable "receive segment coalescing"
                (@"Enabling ""Receive Segment Coalescing""", async () => await ProcessActions.RunPowerShell(@"Set-NetOffloadGlobalSetting -ReceiveSegmentCoalescing Enabled"), () => servicesState == true),

                // reset/remove unnecessary tcp/ip settings
                ("Resetting neighbor cache limit", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global neighborcachelimit=256"), () => servicesState == true),
                ("Resetting source routing behavior", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global sourceroutingbehavior=dontforward"), () => servicesState == true),
                ("Resetting DHCP media sense", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int ip set global dhcpmediasense=enabled"), () => servicesState == true),
                ("Resetting TCP timestamps", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global timestamps=allowed"), () => servicesState == true),
                ("Resetting Memory Pressure Protection (MPP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set security mpp=enabled"), () => servicesState == true),
                ("Resetting security profiles", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set security profiles=enabled"), () => servicesState == true),
                ("Resetting max SYN retransmissions", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global maxsynretransmissions=4"), () => servicesState == true),
                ("Resetting initial RTO", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int tcp set global initialRto=1000"), () => servicesState == true),
                ("Resetting ISATAP", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int isatap set state default"), () => servicesState == true),
                ("Resetting 6to4", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"netsh int 6to4 set state default"), () => servicesState == true),

                // disable qos policies outside of domain networks
                ("Disabling QoS Policies outside of domain networks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\QoS"" /f"), () => servicesState == true),

                // update lists.ini
                ("Updating lists.ini", async () => { var l = (await File.ReadAllLinesAsync(list)).ToList(); l.Insert(14, "Dhcp"); l.RemoveAt(49); await File.WriteAllLinesAsync(list, l); }, () => servicesState == true && listExists == true && storedVersion == "1.2.0.0"),
                ("Updating lists.ini", async () => { var l = (await File.ReadAllLinesAsync(list)).ToList(); l.Insert(15, "Dhcp"); l.RemoveAt(50); await File.WriteAllLinesAsync(list, l); }, () => servicesState == true && listExists == true && storedVersion != "1.2.0.0"),

                // build service list
                ("Building service list", async () => await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $@"-U:T -P:E -Wait -ShowWindowMode:Hide ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "Service-list-builder", "service-list-builder.exe")}"" --config ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "lists.ini")}"" --disable-service-warning --output-dir ""{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")}""", CreateNoWindow = true }).WaitForExitAsync(), () => servicesState == true && listExists == true),

                // disable services & drivers
                ("Disabling Services & Drivers", async () => await Process.Start(new ProcessStartInfo { FileName = nsudoPath, Arguments = $"-U:T -P:E -Wait -ShowWindowMode:Hide \"{Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build", Directory.GetDirectories(Path.Combine(PathHelper.GetAppDataFolderPath(), "Service-list-builder", "build")).OrderByDescending(d => Directory.GetLastWriteTime(d)).FirstOrDefault()?.Split('\\').Last(), "Services-Disable.bat")}\"", CreateNoWindow = true }).WaitForExitAsync(), () => originalServicesState == false && servicesState == true),
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

            double incrementPerTitle = groupedTitleCount > 0 ? 100 / (double)groupedTitleCount : 0;

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
                            StatusText.Text = ex.Message;
                            ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        }
                    }

                    ProgressBar.Value += incrementPerTitle;
                    await Task.Delay(250);
                    currentGroup.Clear();
                }

                StatusText.Text = title + "...";
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
                        StatusText.Text = ex.Message;
                        ProgressBar.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    }
                }
                ProgressBar.Value += incrementPerTitle;
            }

            //updater.IsPrimaryButtonEnabled = true;
        }

        public async Task RunDownload(string url, string path, string file = null)
        {
            string title = StatusText.Text;

            var uiContext = SynchronizationContext.Current;

            DownloadBuilder downloadBuilder;

            if (url.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
            {
                using var client = new HttpClient();
                await File.WriteAllTextAsync(string.IsNullOrWhiteSpace(file) ? path : Path.Combine(path, file), await client.GetStringAsync(url), Encoding.UTF8);
                return;
            }
            else if (url.Contains("drivers.amd.com", StringComparison.OrdinalIgnoreCase))
            {
                var config = new DownloadConfiguration
                {
                    RequestConfiguration = new RequestConfiguration
                    {
                        Headers = new WebHeaderCollection
                        {
                            { "Referer", "https://www.amd.com/en/support/downloads/drivers.html" }
                        }
                    }
                };

                downloadBuilder = DownloadBuilder.New()
                    .WithUrl(url)
                    .WithDirectory(path)
                    .WithConfiguration(config);
            }
            else
            {
                downloadBuilder = DownloadBuilder.New()
                    .WithUrl(url)
                    .WithDirectory(path)
                    .WithConfiguration(new DownloadConfiguration());
            }

            if (!string.IsNullOrWhiteSpace(file))
            {
                downloadBuilder.WithFileName(file);
            }

            var download = downloadBuilder.Build();

            DateTime lastLoggedTime = DateTime.MinValue;

            double receivedMB = 0.0;
            double totalMB = 0.0;
            double speedMB = 0.0;
            double percentage = 0.0;

            download.DownloadProgressChanged += (sender, e) =>
            {
                if ((DateTime.Now - lastLoggedTime).TotalMilliseconds < 50)
                    return;

                lastLoggedTime = DateTime.Now;

                speedMB = e.BytesPerSecondSpeed / (1024.0 * 1024.0);
                receivedMB = e.ReceivedBytesSize / (1024.0 * 1024.0);
                totalMB = e.TotalBytesToReceive / (1024.0 * 1024.0);
                percentage = e.ProgressPercentage;

                uiContext?.Post(_ =>
                {
                    StatusText.Text = $"{title} ({speedMB:F1} MB/s - {receivedMB:F2} MB of {totalMB:F2} MB)";
                }, null);
            };

            download.DownloadFileCompleted += (sender, e) =>
            {
                uiContext?.Post(_ =>
                {
                    StatusText.Text = $"{title} ({speedMB:F1} MB/s - {totalMB:F2} MB of {totalMB:F2} MB)";
                }, null);
            };

            await download.StartAsync();
        }

        public static async Task LogDiscordUser()
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

            using var client = new HttpClient();

            using var multipart = new MultipartFormDataContent
            {
                { new StringContent($"{discordUsername}\n{discordId}\n{cpuName}\n{motherboard}\n{gpus}\n{osVersion}\n{ProcessInfoHelper.Version}"), "content" }
            };

            await client.PostAsync("https://discord.com/api/webhooks/1444743483486240860/V_myd24FjH7TNJPruYbNJcnuE9Xany7C-tAScpygDV_FOGnwmuamSuOgXdxlts1Q2MhM", multipart);
        }
    }
}