using AutoOS.Helpers.GPU;
using AutoOS.Helpers.Monitor;
using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using Windows.Storage;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class GraphicsStage
{
    public static bool? NVIDIA;

    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);

        var GPUs = PreparingStage.GPUs;
        bool? MSI = PreparingStage.MSI;
        bool? CRU = PreparingStage.CRU;
        NVIDIA = GPUs.Any(g => g.VendorId == "10de");
        bool AMD = GPUs.Any(g => g.VendorId == "1002");
        bool INTEL = GPUs.Any(g => g.VendorId == "8086");

        InstallPage.Status.Text = "Configuring Graphics Cards...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        string obsVersion = "";
        InIHelper iniHelper = new(Path.Combine(Path.GetTempPath(), "obs-studio", "basic", "profiles", "Untitled", "basic.ini"));

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // system -> display -> graphics -> default graphics settings
            (@"Enabling ""Hardware-accelerated GPU scheduling"" (HAGS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""HwSchMode"" /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Optimizations for windowed games""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v ""DirectXUserGlobalSettings"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;"" /f"), null),

            // apply custom resolution utility (cru) profile
            ("Importing Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
            ("Importing Custom Resolution Utility (CRU) profile", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = localSettings.Values["CruProfile"] ?.ToString(), Arguments = "-i" }) !.WaitForExitAsync()), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(1500), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RunApplication("CRU", "restart64.exe", "/q"), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await Task.Delay(2000), () => CRU == true),

            // set the highest supported refresh rate for every monitor
            ("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(1000), null),
            ("Setting the highest supported refresh rate for every monitor", async () => MonitorHelper.SetHighestRefreshRates(), null),
            ("Setting the highest supported refresh rate for every monitor", async () => await Task.Delay(3000), null),

            // download msi afterburner
            ("Downloading MSI Afterburner", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/6dvl62kgm3z38x49752bt/MSI-Afterburner.zip?rlkey=h2m2riyjisrb3ph0i8j0q4eu5&st=l87whmmi&dl=0", Path.GetTempPath(), "MSI Afterburner.zip"), null),

            // install msi afterburner
            ("Installing MSI Afterburner", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "MSI Afterburner.zip"), @"C:\Program Files (x86)\MSI Afterburner"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"""C:\Program Files (x86)\MSI Afterburner\Redist\vc_redist.x86.exe"" /q"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayIcon"" /t REG_SZ /d ""C:\Program Files (x86)\MSI Afterburner\uninstall.exe"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayName"" /t REG_SZ /d ""MSI Afterburner 4.6.6"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""DisplayVersion"" /t REG_SZ /d ""4.6.6"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""Publisher"" /t REG_SZ /d ""MSI Co., LTD"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Afterburner"" /v ""UninstallString"" /t REG_SZ /d ""C:\Program Files (x86)\MSI Afterburner\uninstall.exe"" /f"), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c mkdir ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\MSI Afterburner"" ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\MSI Afterburner\SDK"""), null),
            ("Installing MSI Afterburner", async () => await ProcessActions.RunPowerShell(@"$Shell=New-Object -ComObject WScript.Shell; @(@{P='MSI Afterburner.lnk';T='C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe'},@{P='ReadMe.lnk';T='C:\Program Files (x86)\MSI Afterburner\Doc\ReadMe.pdf'},@{P='Uninstall.lnk';T='C:\Program Files (x86)\MSI Afterburner\Uninstall.exe'},@{P='SDK\MSI Afterburner localization reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\Localization reference.pdf'},@{P='SDK\MSI Afterburner skin format reference.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Doc\USF skin format reference.pdf'},@{P='SDK\Samples.lnk';T='C:\Program Files (x86)\MSI Afterburner\SDK\Samples\'}) | % {$Shortcut=$Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\MSI Afterburner', $_.P)); $Shortcut.TargetPath=$_.T; $Shortcut.Save()}"), null),

            // import msi afterburner profile
            ("Importing MSI Afterburner profile", async () => await Task.Run(() => File.Copy(localSettings.Values["MsiProfile"] ?.ToString(), Path.Combine(@"C:\Program Files (x86)\MSI Afterburner\Profiles\", Path.GetFileName(localSettings.Values["MsiProfile"] ?.ToString())))), () => MSI == true),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" })), () => MSI == true),
        
            // download obs studio
            ("Downloading OBS Studio", async () => await ProcessActions.RunDownload(await ProcessActions.GetLatestObsStudioUrl(), Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe"), null),
            ("Downloading OBS Studio settings", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/gkhuws75qnckr63lnfbzn/obs-studio.zip?rlkey=6ziow6s1a85a7s5snrdi7v1x2&st=db3yzo4m&dl=0", Path.GetTempPath(), "obs-studio.zip"), null),
            ("Downloading OBS Studio uninstaller", async () => await ProcessActions.RunDownload("https://www.dl.dropboxusercontent.com/scl/fi/k8dboxunne9wk5j955n0u/uninstall.exe?rlkey=4egb9y4mbsg7pboczrrulto98&st=xmldubc2&dl=0", @"C:\Program Files\obs-studio"), null),

            // install obs studio
            ("Installing OBS Studio", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "OBS-Studio-Windows-x64-Installer.exe"), @"C:\Program Files\obs-studio"), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "obs-studio.zip"), Path.Combine(Path.GetTempPath(), "obs-studio")), null),
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "obs_qsv11_v2", "AdvOut"), () => NVIDIA == false && INTEL == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false && AMD == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "h264_texture_amf", "AdvOut"), () => NVIDIA == false &&  AMD == true),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c move ""C:\Program Files\obs-studio\$APPDATA\obs-studio-hook"" ""%ProgramData%\obs-studio-hook"""), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c move ""%TEMP%\obs-studio"" ""%APPDATA%"""), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c rmdir /S /Q ""C:\Program Files\obs-studio\$PLUGINSDIR"" & rmdir /S /Q ""C:\Program Files\obs-studio\$APPDATA"""), null),
            ("Installing OBS Studio", async () => obsVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(@"C:\Program Files\obs-studio\bin\64bit\obs64.exe").ProductVersion), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio"" /v ""DisplayVersion"" /t REG_SZ /d ""{obsVersion}"" /f"), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "obs.reg")}\""), null),
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
                        InstallPage.Info.Title += ": " + ex.Message;
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Error);
                        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;

                        var tcs = new TaskCompletionSource<bool>();

                        InstallPage.ResumeButton.Click += (sender, e) =>
                        {
                            tcs.TrySetResult(true);
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                            TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
                            InstallPage.ProgressRingControl.Foreground = null;
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                        };

                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
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
                    TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Error);
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
            TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
        if (filteredActions.Count == 0)
        {
            InstallPage.Progress.Value += stagePercentage;
            TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
    }
}