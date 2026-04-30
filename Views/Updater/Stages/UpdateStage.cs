using System.Diagnostics;
using Windows.Storage;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"));

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download vencord
            ("Downloading Vencord", async () => await dialog.Download("https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", ApplicationData.Current.TemporaryFolder.Path, "VencordInstallerCli.exe", "Downloading Vencord", dialog.CurrentGroupStart, dialog.CurrentGroupTarget), () => Discord),

            // install vencord
            ("Installing Vencord", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VencordInstallerCli.exe")}"" -install -install-openasar -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(), () => Discord),
            ("Installing Vencord", async () => await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("VencordInstallerCli.exe")).DeleteAsync(), () => Discord),
        };

        return actions;
    }
}