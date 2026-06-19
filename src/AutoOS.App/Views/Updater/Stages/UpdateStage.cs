using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// disable legacy context menu
			("Disable Legacy Context Menu", async () => Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false), null),
			("Disable Legacy Context Menu", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
			("Disable Legacy Context Menu", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "NoXAMLMenus", 0, RegistryValueKind.DWord, true), null),
			("Disable Legacy Context Menu", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), null)
		};

		return actions;
	}
}
