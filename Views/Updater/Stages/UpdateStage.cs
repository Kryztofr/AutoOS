using AutoOS.Helpers.Registry;
using AutoOS.Helpers.GPU;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();

        var gpus = GpuHelper.GetGPUs().Where(gpu => gpu.NVIDIA);

        foreach (var gpu in gpus)
        {
            // remove "enablemshybrid"
            actions.Add((@"Removing ""EnableMsHybrid""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnableMsHybrid"), null));
        }

        return actions;
    }
}