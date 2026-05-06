using AutoOS.Helpers.Monitor;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using AutoOS.Helpers.Services;
using Windows.Storage;
using System.Text.Json;

namespace AutoOS.Views.Installer.Stages;

public static partial class GamesStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        bool Fortnite = ApplicationStage.Fortnite;

        string fortnitePath = string.Empty;

        string iniPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "GameUserSettings.ini");
        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "GameUserSettings.ini"), iniPath, true);
        InIHelper iniHelper = new(iniPath);

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // setting fortnite frame rate
            ("Setting Fortnite Frame Rate", async () => iniHelper.AddValue("FrameRateLimit", $"{MonitorHelper.GetMonitors().Max(m => m.RefreshRate)}.000000", "/Script/FortniteGame.FortGameUserSettings"), () => Fortnite == true),
            ("Setting Fortnite Frame Rate", async () => await Task.Delay(1000), () => Fortnite == true),
            
            // import fortnite settings
            ("Importing Fortnite settings", async () => Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(@"%LocalAppData%\FortniteGame\Saved\Config\WindowsClient")), () => Fortnite == true),
            ("Importing Fortnite settings", async () => File.Copy(iniPath, Environment.ExpandEnvironmentVariables(@"%LocalAppData%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"), true), () => Fortnite == true),
            ("Importing Fortnite settings", async () => await Task.Delay(1000), () => Fortnite == true),

            // set gpu preference to high performance for fortnite
            ("Setting GPU Preference to high performance for Fortnite", async () => fortnitePath = JsonDocument.Parse(File.ReadAllText(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat")).RootElement.GetProperty("InstallationList").EnumerateArray().FirstOrDefault(e => e.GetProperty("AppName").GetString() == "Fortnite").GetProperty("InstallLocation").GetString(), () => Fortnite == true),
            ("Setting GPU Preference to high performance for Fortnite", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences", fortnitePath + @"\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe", "SwapEffectUpgradeEnable=1;GpuPreference=2;", RegistryValueKind.String), () => Fortnite == true),
            ("Setting GPU Preference to high performance for Fortnite", async () => await Task.Delay(1000), () => Fortnite == true),

            // install easyanticheat
            ("Installing EasyAntiCheat", async () => await Process.Start(new ProcessStartInfo($@"{fortnitePath}\FortniteGame\Binaries\Win64\EasyAntiCheat\EasyAntiCheat_EOS_Setup.exe", "install 4fe75bbc5a674f4f9b356b5c90567da5") {  WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), () => Fortnite == true),
            ("Installing EasyAntiCheat", async () => await Task.Delay(1000), () => Fortnite == true),
            ("Disabling EasyAntiCheat startup entry", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\EasyAntiCheat_EOS", "Start", 4, RegistryValueKind.DWord), () => Fortnite == true),
            ("Disabling EasyAntiCheat startup entry", async () => ServicesHelper.StopService("EasyAntiCheat_EOS"), () => Fortnite == true),
            ("Disabling EasyAntiCheat startup entry", async () => await Task.Delay(1000), () => Fortnite == true),
        
            // disable fullscreen optimizations for fortnite
            ("Disabling fullscreen optimizations for Fortnite", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", $@"{fortnitePath}\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe", "~ DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String), () => Fortnite == true),
        };

        return actions;
    }
}
