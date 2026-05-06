using AutoOS.Helpers.Registry;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class CleanupStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // clean temp directories
            ("Cleaning temp directories", async () => await Task.WhenAll(Process.GetProcessesByName("TiWorker").Select(async process => { process.Kill(); await process.WaitForExitAsync(); })), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\Logs")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\Panther")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\SoftwareDistribution")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\System32\LogFiles")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\System32\SleepStudy")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\System32\sru")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\System32\WDI")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\System32\winevt\Logs")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\SystemTemp")), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => ProcessActions.CleanDirectory(@"C:\Windows\Temp")), null),
            ("Cleaning temp directories", async () => ProcessActions.CleanDirectory(ApplicationData.Current.TemporaryFolder.Path), null),
            ("Cleaning temp directories", async () => File.Delete(@"C:\DumpStack.log"), null),

            // run disk cleanup
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\BranchCache", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Content Indexer Cleaner", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Delivery Optimization Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Device Driver Packages", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Diagnostic Data Viewer database files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Feedback Hub Archive log files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Language Pack", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Offline Pages Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\RetailDemo Offline Content", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Update Cleanup", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Upgrade Discarded Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\User file versions", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Defender", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows ESD installation files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Reset Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files", "StateFlags0000", 2, RegistryValueKind.DWord), null),
            ("Running disk cleanup", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Windows\System32\cleanmgr.exe", Arguments = "/sagerun:0", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
        };

        return actions;
    }
}
