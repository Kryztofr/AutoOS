using AutoOS.Helpers.GPU;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        var gpus = GpuHelper.GetGPUs().Where(gpu => gpu.NVIDIA);

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
        };

        foreach (var gpu in gpus)
        {
            // adjust "rmclkslowdown"
            actions.Add((@"Adjusting ""RMClkSlowDown""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMClkSlowDown", 67108864, RegistryValueKind.DWord), null));
        }

        return actions;
    }
}