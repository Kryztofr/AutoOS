using AutoOS.Helpers.Scheduling;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.Views.Installer.Stages;

public static class SchedulingStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        int PCores = PreparingStage.PCores;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // optimize affinities
            ("Optimizing Affinities", async () => await Task.Delay(1000), () => PCores >= 4),
            ("Optimizing Affinities", async () => await SchedulingHelper.OptimizeAffinities(), () => PCores >= 4),
            ("Optimizing Affinities", async () => await Task.Delay(2000), () => PCores >= 4),

            // disable interrupt steering
            ("Disabling interrupt steering", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "InterruptSteeringFlags", 1, RegistryValueKind.DWord), () => PCores >= 4),
        
            // disable thread dpcs
            ("Disabling Thread DPCs", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\kernel", "ThreadDpcEnable", 0, RegistryValueKind.DWord), null),
        };

        return actions;
    }
}
