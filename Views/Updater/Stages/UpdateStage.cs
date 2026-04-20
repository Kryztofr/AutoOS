using AutoOS.Helpers.Registry;
using AutoOS.Helpers.GPU;
using Microsoft.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();

        var gpus = GpuHelper.GetGPUs().Where(gpu => gpu.NVIDIA);

        foreach (var gpu in gpus)
        {
            // force "hardware composed: independent flip"
            actions.Add((@"Forcing ""Hardware Composed: Independent Flip""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "enableRS2FlipCollapse", 1, RegistryValueKind.DWord), null));
            actions.Add((@"Forcing ""Hardware Composed: Independent Flip""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "enableRS2ImmediateFlipCompletionReporting", 1, RegistryValueKind.DWord), null));
        }

        return actions;
    }
}