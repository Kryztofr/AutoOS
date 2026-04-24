using Microsoft.Win32;
using AutoOS.Helpers.Registry;
using AutoOS.Helpers.TaskScheduler;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // fix wrong registry key type
            ("Fixing wrong registry key type", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "NoExplorer", 1, RegistryValueKind.String), null),
            ("Fixing wrong registry key type", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", "NoExplorer", 1, RegistryValueKind.String), null),

            // disable "\microsoft\windows\hotpatch\monitoring"
            (@$"Disabling ""\Microsoft\Windows\Hotpatch\Monitoring""", async () => TaskSchedulerHelper.Toggle(@"\Microsoft\Windows\Hotpatch\Monitoring", false), null)
        };

        return actions;
    }
}