using AutoOS.Common;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Core.Helpers.Services;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;

namespace AutoOS.Views.Installer.Stages;

public static class MemoryManagementStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        bool SSD = PreparingStage.SSD;

        return new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // disable superfetch
            ("Disabling Superfetch", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\SysMain", "Start", 4, RegistryValueKind.DWord), () => SSD == true),
            ("Disabling Superfetch", async () => ServicesHelper.StopService("SysMain"), () => SSD == true),
            
            // disable "applicationlaunchprefetching"
            (@"Disabling ""ApplicationLaunchPrefetching""", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -ApplicationLaunchPrefetching"), () => SSD == true),

            // disable "applicationprelaunch"
            (@"Disabling ""ApplicationPreLaunch""", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -ApplicationPreLaunch"), () => SSD == true),

            // disable "memorycompression"
            (@"Disabling ""MemoryCompression""", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -MemoryCompression"), null),

            // disable "operationapi"
            (@"Disabling ""OperationAPI""", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -OperationAPI"), null),

            // disable "pagecombining"
            (@"Disabling ""PageCombining""", async () => await ProcessActions.RunPowerShell(@"Disable-MMAgent -PageCombining"), null),
        };
    }
}

