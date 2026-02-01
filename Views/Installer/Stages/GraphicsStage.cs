using AutoOS.Helpers;
using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using Windows.Storage;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class GraphicsStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool? Intel_6th = PreparingStage.Intel_6th;
        bool? Intel_7th_10th = PreparingStage.Intel_7th_10th;
        bool? Intel_11th_14th = PreparingStage.Intel_11th_14th;
        bool? Intel_Arc = PreparingStage.Intel_Arc;
        bool? NVIDIA_GTX900_GTX10 = PreparingStage.NVIDIA_GTX900_GTX10;
        bool? NVIDIA_GTX16_RTX50 = PreparingStage.NVIDIA_GTX16_RTX50;
        bool? AMD_RX5000_RX9000 = PreparingStage.AMD_RX5000_RX9000;
        bool? HDCP = PreparingStage.HDCP;
        bool? HDMIDPAudio = PreparingStage.HDMIDPAudio;
        bool? AlwaysShowTrayIcons = PreparingStage.AlwaysShowTrayIcons;
        bool? MSI = PreparingStage.MSI;
        bool? CRU = PreparingStage.CRU;

        InstallPage.Status.Text = "Configuring Graphics Cards...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        string obsVersion = "";
        InIHelper iniHelper = new InIHelper(Path.Combine(Path.GetTempPath(), "obs-studio", "basic", "profiles", "Untitled", "basic.ini"));

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download the latest intel driver
            ("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/764512/gfx_win_101.2115.zip", Path.GetTempPath(), "driver.zip"), () => Intel_6th == true),

            // extract the driver
            ("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.zip"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel_6th == true),

            // install the driver
            ("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), () => Intel_6th == true),
            ("Installing the Intel driver", async () => await ProcessActions.RefreshUI(), () => Intel_6th == true),
           
            // download the latest intel driver
            ("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/871509/gfx_win_101.2140.exe", Path.GetTempPath(), "driver.exe"), () => Intel_7th_10th == true),

            // extract the driver
            ("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel_7th_10th == true),

            // install the driver
            ("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), () => Intel_7th_10th == true),
            ("Installing the Intel driver", async () => await ProcessActions.RefreshUI(), () => Intel_7th_10th == true),

            // download the latest intel driver
            ("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/870640/gfx_win_101.7082.exe", Path.GetTempPath(), "driver.exe"), () => Intel_11th_14th == true),

            // extract the driver
            ("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel_11th_14th == true),

            // install the driver
            ("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), () => Intel_11th_14th == true),
            ("Installing the Intel driver", async () => await ProcessActions.RefreshUI(), () => Intel_11th_14th == true),

            // download the latest intel driver
            ("Downloading the latest Intel Driver", async () => await ProcessActions.RunDownload("https://downloadmirror.intel.com/873140/gfx_win_101.8425.exe", Path.GetTempPath(), "driver.exe"), () => Intel_Arc == true),

            // extract the driver
            ("Extracting the Intel driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => Intel_Arc == true),

            // install the driver
            ("Installing the Intel driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Installer.exe"" /silent"), () => Intel_Arc == true),
            ("Installing the Intel driver", async () => await ProcessActions.RefreshUI(), () => Intel_Arc == true),

            // download the latest amd driver
            ("Downloading the latest AMD Driver", async () => await ProcessActions.RunDownload(await ProcessActions.GetLatestAmdDriverUrl(), Path.GetTempPath(), "driver.exe"), () => AMD_RX5000_RX9000 == true),

            // extract the driver
            ("Extracting the AMD driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => AMD_RX5000_RX9000 == true),

            // strip the amd driver
            ("Stripping the AMD driver", async () => await ProcessActions.RunApplication("RadeonSoftwareSlimmer", "RadeonSoftwareSlimmer.exe", $@"--extracted-installer ""{Path.Combine(Path.GetTempPath(), "driver")}"" --config ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "RadeonSoftwareSlimmer", "config.ini")}"""), () => AMD_RX5000_RX9000 == true),

            // install the driver
            ("Installing the AMD driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\Setup.exe"" -install"), () => AMD_RX5000_RX9000 == true),

            // download the latest nvidia driver                                      
            ("Downloading the latest NVIDIA Driver", async () => await ProcessActions.RunDownload("https://uk.download.nvidia.com/Windows/581.80/581.80-desktop-win10-win11-64bit-international-dch-whql.exe", Path.GetTempPath(), "driver.exe"), () => NVIDIA_GTX900_GTX10 == true),

            // download the latest nvidia driver                                                     
            ("Downloading the latest NVIDIA Driver", async () => await ProcessActions.RunDownload((await NvidiaHelper.CheckUpdate()).newestDownloadUrl, Path.GetTempPath(), "driver.exe"), () => NVIDIA_GTX16_RTX50 == true),

            // extract the nvidia driver
            ("Extracting the NVIDIA driver", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "driver.exe"), Path.Combine(Path.GetTempPath(), "driver")), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // strip the driver
            ("Stripping the NVIDIA driver", async () => await ProcessActions.RunNvidiaStrip(), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // install the nvidia driver
            ("Installing the NVIDIA driver", async () => await ProcessActions.RunNsudo("CurrentUser", @"""%TEMP%\driver\setup.exe"" /s"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Installing the NVIDIA driver", async () => await ProcessActions.Sleep(3000), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Installing the NVIDIA driver", async () => await ProcessActions.RefreshUI(), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // system -> display -> graphics -> default graphics settings
            (@"Enabling ""Hardware-accelerated GPU scheduling"" (HAGS)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""HwSchMode"" /t REG_DWORD /d 2 /f"), null),
            (@"Enabling ""Optimizations for windowed games""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences"" /v ""DirectXUserGlobalSettings"" /t REG_SZ /d ""SwapEffectUpgradeEnable=1;"" /f"), null),

            // apply custom resolution utility (cru) profile
            ("Importing Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(1500), () => CRU == true),
            ("Importing Custom Resolution Utility (CRU) profile", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = localSettings.Values["CruProfile"] ?.ToString(), Arguments = "-i" }) !.WaitForExitAsync()), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(1500), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RunApplication("CRU", "restart64.exe", "/q"), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.Sleep(2000), () => CRU == true),
            ("Applying Custom Resolution Utility (CRU) profile", async () => await ProcessActions.RefreshUI(), () => CRU == true),

            // set the highest supported refresh rate for every monitor
            ("Setting the highest supported refresh rate for every monitor", async () => await ProcessActions.Sleep(1000), null),
            ("Setting the highest supported refresh rate for every monitor", async () => await ProcessActions.SetHighestRefreshRates(), null),

            // configure settings
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpApplyAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpBrightness"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpContrast"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpHue"" /t REG_DWORD /d 3221225472 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""ProcAmpSaturation"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SharpnessEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SharpnessFactor"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionAutoDetectEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionEnabledChroma"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""NoiseReductionFactor"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableFMD"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableSTE"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SkinTone"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableTCC"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorRed"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorGreen"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorBlue"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorCyan"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorMagenta"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""SatFactorYellow"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableACE"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""AceLevel"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""InputYUVRangeApplyAlways"" /t REG_DWORD /d 1 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""InputYUVRange"" /t REG_DWORD /d 2 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableIS"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""EnableNLAS"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Display\igfxcui\MediaKeys"" /v ""UISharpnessOptimalEnabledAlways"" /t REG_DWORD /d 0 /f"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            ("Configuring settings", async () => await ProcessActions.RunPowerShellScript("intelsettings.ps1", ""), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            // disable unnecessary services
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igccservice"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop igccservice"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\igfxCUIService2.0.0.0"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop igfxCUIService2.0.0.0"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            // disable high-definition-content-protection (hdcp)
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cphs"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop cphs"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\cplspcon"" /v ""Start"" /t REG_DWORD /d 4 /f & sc stop cplspcon"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),

            // configure amd settings
            ("Configuring AMD settings", async () => await ProcessActions.RunPowerShellScript("amdsettings.ps1", ""), () => AMD_RX5000_RX9000 == true),

            // accept eula
            ("Accepting EULA", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\DisplayOverride"" /v ""EulaAccepted"" /t REG_SZ /d true /f"), () => AMD_RX5000_RX9000 == true),

            // disable issue detection
            ("Disabling issue detection", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\AIM"" /v ""LaunchBugTool"" /t REG_DWORD /d 0 /f"), () => AMD_RX5000_RX9000 == true),

            // disable hotkeys
            ("Disabling hotkeys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\R3DBk"" /v ""HotkeysDisabled"" /t REG_DWORD /d 1 /f"), () => AMD_RX5000_RX9000 == true),

            // disable overlays
            ("Disabling overlays", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN\Performance"" /v ""MetricsOverlayState"" /t REG_DWORD /d 0 /f"), () => AMD_RX5000_RX9000 == true),

            // disable web browser
            ("Disabling web browser", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""AllowWebContent"" /t REG_SZ /d false /f"), () => AMD_RX5000_RX9000 == true),

            // disable system tray
            ("Disabling system tray", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""SystemTray"" /t REG_SZ /d false /f"), () => AMD_RX5000_RX9000 == true),

            // disable tutorials
            ("Disabling tutorials", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""CN_Hide_Tutorials"" /t REG_SZ /d true /f"), () => AMD_RX5000_RX9000 == true),

            // disable advertisements
            ("Disabling advertisements", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""CN_Hide_FeatureData"" /t REG_SZ /d true /f"), () => AMD_RX5000_RX9000 == true),

            // disable toast notifications
            ("Disabling toast notifications", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""CN_Hide_Toast_Notification"" /t REG_SZ /d true /f"), () => AMD_RX5000_RX9000 == true),
            
            // disable animations & effects
            ("Disabling animations & effects", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\AMD\CN"" /v ""AnimationEffect"" /t REG_SZ /d false /f"), () => AMD_RX5000_RX9000 == true),

            // disable unnecessary services
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AMD Crash Defender Service"" /v Start /t REG_DWORD /d 4 /f & sc stop ""AMD Crash Defender Service"""), () => AMD_RX5000_RX9000 == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\AMD External Events Utility"" /v Start /t REG_DWORD /d 4 /f & sc stop ""AMD External Events Utility"""), () => AMD_RX5000_RX9000 == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdfendr"" /v Start /t REG_DWORD /d 4 /f & sc stop ""amdfendr"""), () => AMD_RX5000_RX9000 == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdfendrmgr"" /v Start /t REG_DWORD /d 4 /f & sc stop ""amdfendrmgr"""), () => AMD_RX5000_RX9000 == true),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\amdlog"" /v Start /t REG_DWORD /d 4 /f & sc stop ""amdlog"""), () => AMD_RX5000_RX9000 == true),

            // disable nvidia tray icon
            ("Disabling NVIDIA tray icon", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\NvTray"" /v StartOnLogin /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling NVIDIA tray icon", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""HideXGpuTrayIcon"" /t REG_DWORD /d 1 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling NVIDIA tray icon", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager"" /v ""ShowTrayIcon"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable dlss indicator
            ("Disabling DLSS Indicator", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\NGXCore"" /v ""ShowDlssIndicator"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable automatic updates
            ("Disabling automatic updates", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\CoProcManager"" /v AutoDownload /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable telemetry
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Nvidia Corporation\NvControlPanel2\Client"" /v ""OptInOrOutPreference"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup"" /v ""SendTelemetryData"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID44231 /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID64640 /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v EnableRID66610 /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c cd /d ""C:\Windows\System32\DriverStore\FileRepository\"" & dir NvTelemetry64.dll /a /b /s & del NvTelemetry64.dll /a /s"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable logging
            ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogDisableMasks /t REG_BINARY /d ""00ffff0f01ffff0f02ffff0f03ffff0f04ffff0f05ffff0f06ffff0f07ffff0f08ffff0f09ffff0f0affff0f0bffff0f0cffff0f0dffff0f0effff0f0fffff0f10ffff0f11ffff0f12ffff0f13ffff0f14ffff0f15ffff0f16ffff0f00ffff1f01ffff1f02ffff1f03ffff1f04ffff1f05ffff1f06ffff1f07ffff1f08ffff1f09ffff1f0affff1f0bffff1f0cffff1f0dffff1f0effff1f0fffff1f00ffff2f01ffff2f02ffff2f03ffff2f04ffff2f05ffff2f06ffff2f07ffff2f08ffff2f09ffff2f0affff2f0bffff2f0cffff2f0dffff2f0effff2f0fffff2f00ffff3f01ffff3f02ffff3f03ffff3f04ffff3f05ffff3f06ffff3f07ffff3f"" /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogWarningEntries /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogPagingEntries /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogEventEntries /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling logging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v LogErrorEntries /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // use advanced 3d image settings
            ("Using advanced 3D image settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\NVIDIA Corporation\Global\NVTweak"" /v ""Gestalt"" /t REG_DWORD /d 515 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // import optimized nvidia profile
            ("Importing optimized NVIDIA profile", async () => await ProcessActions.ImportProfile("BaseProfile.nip"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // configure physx to use gpu
            ("Configuring PhysX to use GPU", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""NvCplPhysxAuto"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // configure color settings
            ("Configuring color settings", async () => await ProcessActions.RunPowerShellScript("colorsettings.ps1", ""), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Configuring color settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c for /f ""delims="" %a in ('reg query HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase') do reg add ""%a"" /v ""ColorformatConfig"" /t REG_BINARY /d ""DB02000014000000000A00080000000003010000"" /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable high-definition-content-protection (hdcp)
            ("Disabling high-definition-content-protection (HDCP)", async () => await ProcessActions.RunPowerShellScript("hdcp.ps1", ""), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true && HDCP == false),

            // disable error code correction (ecc)
            ("Disabling error code correction (ECC)", async () => await ProcessActions.RunPowerShellScript("ecc.ps1", ""), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // configure miscellaneous nvidia settings
            ("Configuring miscellaneous NVIDIA settings", async () => await ProcessActions.RunPowerShellScript("nvidiamisc.ps1", ""), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable scaling
            ("Disabling scaling", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c for %i in (Scaling) do for /f ""tokens=*"" %a in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /s /f ""%i""^| findstr ""HKEY""') do reg add ""%a"" /v ""Scaling"" /t REG_DWORD /d 1 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable dynamic p-state
            ("Disabling dynamic P-State", async () => await ProcessActions.RunPowerShellScript("pstate.ps1", ""), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable display power savings
            ("Disabling Display Power Savings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""DisplayPowerSaving"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),
            ("Disabling Display Power Savings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\NVIDIA Corporation\Global\NVTweak"" /v ""DisplayPowerSaving"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable hd audio power savings
            ("Disabling HD Audio Power Savings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm"" /v ""EnableHDAudioD3Cold"" /t REG_DWORD /d 0 /f"), () => NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true),

            // disable high-definition multimedia interface (hdmi)/displayport (dp) audio
            //("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => await ProcessActions.RunPowerShell("Get-PnpDevice | Where-Object { $_.FriendlyName -eq 'High Definition Audio Device' } | Disable-PnpDevice -Confirm:$false"), () => HDMIDPAudio == false && (NVIDIA_GTX16_RTX50 == true  || NVIDIA_GTX900_GTX10 == true || AMD_RX5000_RX9000 == true)),
            //("Disabling High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio", async () => await ProcessActions.RunPowerShell("Get-PnpDevice | Where-Object { $_.FriendlyName -eq 'Intel(R) Display Audio' } | Disable-PnpDevice -Confirm:$false"), () => HDMIDPAudio == false && (Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true)),

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
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "obs_qsv11_v2", "AdvOut"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "obs_qsv11_v2", "AdvOut"), () => Intel_6th == true || Intel_7th_10th == true || Intel_11th_14th == true || Intel_Arc == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("Encoder", "h264_texture_amf", "AdvOut"), () => AMD_RX5000_RX9000 == true),
            ("Installing OBS Studio", async () => iniHelper.AddValue("RecEncoder", "h264_texture_amf", "AdvOut"), () => AMD_RX5000_RX9000 == true),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c move ""C:\Program Files\obs-studio\$APPDATA\obs-studio-hook"" ""%ProgramData%\obs-studio-hook"""), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("CurrentUser", @"cmd /c move ""%TEMP%\obs-studio"" ""%APPDATA%"""), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c rmdir /S /Q ""C:\Program Files\obs-studio\$PLUGINSDIR"" & rmdir /S /Q ""C:\Program Files\obs-studio\$APPDATA"""), null),
            ("Installing OBS Studio", async () => obsVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(@"C:\Program Files\obs-studio\bin\64bit\obs64.exe").ProductVersion), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\OBS Studio"" /v ""DisplayVersion"" /t REG_SZ /d ""{obsVersion}"" /f"), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunNsudo("CurrentUser", $"cmd /c reg import \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "obs.reg")}\""), null),
            ("Installing OBS Studio", async () => await ProcessActions.RunPowerShell(@"$s=New-Object -ComObject WScript.Shell;$sc=$s.CreateShortcut([System.IO.Path]::Combine($env:ProgramData,'Microsoft\Windows\Start Menu\Programs\OBS Studio.lnk'));$sc.TargetPath='C:\Program Files\obs-studio\bin\64bit\obs64.exe';$sc.WorkingDirectory='C:\Program Files\obs-studio\bin\64bit';$sc.Save()"), null)
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