using System.Diagnostics;
using Windows.Storage;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"));

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download openasar
            ("Downloading OpenAsar", async () => await dialog.Download("https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe", ApplicationData.Current.TemporaryFolder.Path, "VencordInstallerCli.exe", "Downloading Vencord", dialog.CurrentGroupStart, dialog.CurrentGroupTarget), () => Discord),

            // install openasar
            ("Installing OpenAsar", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VencordInstallerCli.exe")}"" -install -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(), () => Discord),
            ("Installing OpenAsar", async () => await Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $@"/c """"{Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VencordInstallerCli.exe")}"" -install-openasar -branch auto""" , CreateNoWindow = true })!.WaitForExitAsync(), () => Discord == true),
            ("Installing OpenAsar", async () => await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("VencordInstallerCli.exe")).DeleteAsync(), () => Discord),

            // set windows start menu layout to list
            (@"Setting Windows Start Menu layout to List", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start", "AllAppsViewMode", 2, RegistryValueKind.DWord), null),
        };

        return actions;
    }
}