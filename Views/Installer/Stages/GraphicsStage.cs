using Windows.Storage;
using WinRT.Interop;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Monitor;
using AutoOS.Helpers.GPU;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class GraphicsStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);

        int PCores = PreparingStage.PCores;
        var GPUs = PreparingStage.GPUs;
        bool MSI = PreparingStage.MSI;
        bool CRU = PreparingStage.CRU;
        bool NVIDIA = GPUs.Any(g => g.VendorId == "10de");
        bool AMD = GPUs.Any(g => g.VendorId == "1002");
        bool INTEL = GPUs.Any(g => g.VendorId == "8086");

        InstallPage.Status.Text = "Configuring Graphics Cards...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        string obsVersion = "";
        InIHelper iniHelper = new(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "obs-studio", "basic", "profiles", "Untitled", "basic.ini"));

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // system -> display -> graphics -> default graphics settings
            (@"Enabling ""Hardware-accelerated GPU scheduling"" (HAGS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord), null),
            (@"Enabling ""Optimizations for windowed games""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings", "SwapEffectUpgradeEnable=1;", RegistryValueKind.String), null),

            // apply custom resolution utility (cru) profile
            ("Importing Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
            ("Importing Custom Resolution Utility (CRU) profile", async () => await Process.Start(new ProcessStartInfo { FileName = localSettings.Values["CruProfile"]?.ToString(), Arguments = "-i", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "CRU", "restart64.exe"), "/q") { CreateNoWindow = true })!.WaitForExitAsync(), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(2000), () => CRU == true),

            // set the highest supported refresh rate for every monitor
            ("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(1000), null),
            ("Setting the highest supported refresh rate for every monitor", async () => MonitorHelper.SetHighestRefreshRates(), null),
            ("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(3000), null),

            // download msi afterburner
            ("Downloading MSI Afterburner", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/6dvl62kgm3z38x49752bt/MSI-Afterburner.zip?rlkey=h2m2riyjisrb3ph0i8j0q4eu5&st=l87whmmi&dl=0", ApplicationData.Current.TemporaryFolder.Path, "MSI Afterburner.zip"), null),

            // install msi afterburner
            ("Installing MSI Afterburner", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "MSI Afterburner.zip"), @"C:\Program Files (x86)\MSI Afterburner"); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("MSI Afterburner.zip")).DeleteAsync(); }, null),
            ("Installing MSI Afterburner", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\Redist\vc_redist.x86.exe", Arguments = "/q", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayIcon", @"C:\Program Files (x86)\MSI Afterburner\uninstall.exe", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayName", "MSI Afterburner 4.6.6", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "DisplayVersion", "4.6.6", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "Publisher", "MSI Co., LTD", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner", "UninstallString", @"C:\Program Files (x86)\MSI Afterburner\uninstall.exe", RegistryValueKind.String), null),
            ("Installing MSI Afterburner", async () => { string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\MSI Afterburner"); Directory.CreateDirectory(startMenuPath); Directory.CreateDirectory(Path.Combine(startMenuPath, "SDK")); }, null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunPowerShell(@"$Shell=New-Object -ComObject WScript.Shell; @(@{P='MSI Afterburner.lnk';T='C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe'},@{P='ReadMe.lnk';T='C:\Program Files (x86)\MSI Afterburner\Doc\ReadMe.pdf'},@{P='Uninstall.lnk';T='C:\Program Files (x86)\MSI Afterburner\Uninstall.exe'},@{P='SDK\MSI Afterburner localization reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\Localization reference.pdf'},@{P='SDK\MSI Afterburner skin format reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\USF skin format reference.pdf'},@{P='SDK\Samples.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Samples\'}) | % {$Shortcut=$Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\MSI Afterburner', $_.P)); $Shortcut.TargetPath=$_.T; $Shortcut.Save()}"), null),

            // import msi afterburner profile
            ("Importing MSI Afterburner profile", async () => File.Copy(localSettings.Values["MsiProfile"]?.ToString(), Path.Combine(@"C:\Program Files (x86)\MSI Afterburner\Profiles\", Path.GetFileName(localSettings.Values["MsiProfile"]?.ToString()))), () => MSI == true),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => MSI == true),
        
            // download obs studio
            ("Downloading OBS Studio", async () => await ProcessActions.RunDownload(await ProcessActions.GetLatestObsStudioUrl(), ApplicationData.Current.TemporaryFolder.Path, "OBS-Studio-Windows-x64-Installer.exe"), null),
            ("Downloading OBS Studio settings", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/gkhuws75qnckr63lnfbzn/obs-studio.zip?rlkey=6ziow6s1a85a7s5snrdi7v1x2&st=db3yzo4m&dl=0", ApplicationData.Current.TemporaryFolder.Path, "obs-studio.zip"), null),
            ("Downloading OBS Studio uninstaller", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/k8dboxunne9wk5j955n0u/uninstall.exe?rlkey=4egb9y4mbsg7pboczrrulto98&st=xmldubc2&dl=0", @"C:\Program Files\obs-studio"), null),

            // install obs studio
            ("Installing OBS Studio", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "OBS-Studio-Windows-x64-Installer.exe"), @"C:\Program Files\obs-studio"); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("OBS-Studio-Windows-x64-Installer.exe")).DeleteAsync(); }, null),
            ("Installing OBS Studio", async () => { await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "obs-studio.zip"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "obs-studio")); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("obs-studio.zip")).DeleteAsync(); }, null),
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false && AMD == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false &&  AMD == true),
            ("Installing OBS Studio", async () => Directory.Move(@"C:\Program Files\obs-studio\$APPDATA\obs-studio-hook", Environment.ExpandEnvironmentVariables(@"%ProgramData%\obs-studio-hook")), null),
            ("Installing OBS Studio", async () => { Directory.Move(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "obs-studio"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio")); }, null),
            ("Installing OBS Studio", async () => Directory.Delete(@"C:\Program Files\obs-studio\$PLUGINSDIR", true), null),
            ("Installing OBS Studio", async () => Directory.Delete(@"C:\Program Files\obs-studio\$APPDATA", true), null),
            ("Installing OBS Studio", async () => obsVersion = FileVersionInfo.GetVersionInfo(@"C:\Program Files\obs-studio\bin\64bit\obs64.exe").ProductVersion, null),
            ("Installing OBS Studio", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio", "DisplayVersion", obsVersion, RegistryValueKind.String), null),
            ("Installing OBS Studio", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $"import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "obs.reg")}\"", CreateNoWindow = true })!.WaitForExitAsync(), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([System.IO.Path]::Combine($env:ProgramData,'Microsoft\Windows\Start Menu\Programs\OBS Studio.lnk'));$sc.TargetPath='C:\Program Files\obs-studio\bin\64bit\obs64.exe';$sc.WorkingDirectory='C:\Program Files\obs-studio\bin\64bit';$sc.Save()"), null)
        };

        var gpus = PreparingStage.GPUs.Where(g => g.Install).ToList();

        var latestDrivers = new Dictionary<string, (string Version, string Url)>();
        var intelActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
        var amdActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();
        var nvidiaActions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();

        foreach (var gpu in gpus)
        {
            string newestVersion = "";
            string newestDownloadUrl = "";

            switch (gpu.VendorId)
            {
                case "10de":
                    (newestVersion, newestDownloadUrl) = await NvidiaHelper.CheckUpdate(gpu);
                    break;
                case "1002":
                    (newestVersion, newestDownloadUrl) = await AmdHelper.CheckUpdate(gpu);
                    break;
                case "8086":
                    (newestVersion, newestDownloadUrl) = await IntelHelper.CheckUpdate(gpu);
                    break;
            }

            if (latestDrivers.TryGetValue(gpu.VendorId, out var driver) && driver.Version == newestVersion)
                continue;

            latestDrivers[gpu.VendorId] = (newestVersion, newestDownloadUrl);

            switch (gpu.VendorId)
            {
                case "8086":
                    intelActions = IntelHelper.DriverActions(gpu, newestDownloadUrl);
                    break;

                case "1002":
                    amdActions = AmdHelper.DriverActions(gpu, newestDownloadUrl);
                    break;

                case "10de":
                    nvidiaActions = NvidiaHelper.DriverActions(gpu, newestDownloadUrl);
                    break;
            }
        }

        actions.InsertRange(0, nvidiaActions);
        actions.InsertRange(0, amdActions);
        actions.InsertRange(0, intelActions);

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

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

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
                        await ProcessActions.LogError(ex);

                        InstallPage.Info.Title = $"{previousTitle}: {ex.Message}";
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Error);
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;

                        var tcs = new TaskCompletionSource<bool>();

                        RoutedEventHandler resumeHandler = null;
                        resumeHandler = (sender, e) =>
                        {
                            InstallPage.ResumeButton.Click -= resumeHandler;
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                            Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;

                            tcs.TrySetResult(true);
                        };

                        InstallPage.ResumeButton.Click += resumeHandler;
                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
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
                    InstallPage.Info.Title += ": " + ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Error);
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;
                    await ProcessActions.LogError(ex);

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
            Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
        if (filteredActions.Count == 0)
        {
            InstallPage.Progress.Value += stagePercentage;
            Helpers.Taskbar.TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
    }
}
