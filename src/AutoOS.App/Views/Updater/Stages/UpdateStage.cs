using System.Diagnostics;
using AutoOS.Core.Helpers.Services;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// enable ntfs file encryption
			("Enabling NTFS File Encryption", async () => await Process.Start(new ProcessStartInfo { FileName = "fsutil.exe", Arguments = $@"behavior set disableencryption 0", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),

			// enable superfetch
			("Enabling Superfetch", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\SysMain", "Start", 2, RegistryValueKind.DWord), null),
			("Enabling Superfetch", async () => ServicesHelper.StartService("SysMain"), null),
			
			// enable "applicationlaunchprefetching"
			(@"Enabling ""ApplicationLaunchPrefetching""", async () => await ProcessActions.RunPowerShell(@"Enable-MMAgent -ApplicationLaunchPrefetching"), null),

			// enable "applicationprelaunch"
			(@"Enabling ""ApplicationPreLaunch""", async () => await ProcessActions.RunPowerShell(@"Enable-MMAgent -ApplicationPreLaunch"), null),

			// enable "memorycompression"
			(@"Enabling ""MemoryCompression""", async () => await ProcessActions.RunPowerShell(@"Enable-MMAgent -MemoryCompression"), null),

			// enable "operationapi"
			(@"Enabling ""OperationAPI""", async () => await ProcessActions.RunPowerShell(@"Enable-MMAgent -OperationAPI"), null),

			// enable "pagecombining"
			(@"Enabling ""PageCombining""", async () => await ProcessActions.RunPowerShell(@"Enable-MMAgent -PageCombining"), null)
		};

		return actions;
	}
}
