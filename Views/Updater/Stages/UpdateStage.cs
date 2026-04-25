using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // optimize xbox gaming overlay settings
            ("Optimizing Xbox Gaming Overlay settings", async () => await ProcessActions.RunPowerShellScript("xboxgamingoverlay.ps1", ""), null),
        };

        return actions;
    }
}