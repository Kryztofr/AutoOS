using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // add windows defender exclusion
            (@"Adding Windows Defender Exclusion", async () => await ProcessActions.RunPowerShell(@"Add-MpPreference -ExclusionProcess 'AutoOS.exe'"), null),
        };

        return actions;
    }
}