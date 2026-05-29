using AutoOS.Core.Helpers.Registry;
using AutoOS.Views.Settings.Power;
using Microsoft.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		Guid guid = Guid.Empty;

		return
		[
            // update power plan
            ("Selecting AutoOS Power Plan", async () => guid = PowerApi.GetPlanGuidByName("AutoOS"), null),
			(@"Disabling ""Hetero containment policy.""", async () => PowerApi.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("60fbe21b-efd9-49f2-b066-8674d8e9f423"), 0), null),
			(@"Disabling ""Hetero containment policy.""", async () => PowerApi.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("60fbe21b-efd9-49f2-b066-8674d8e9f423"), 0), null),
			("Applying Changes", async () =>  PowerApi.PowerSetActiveScheme(guid), null),

			// optimize multimedia class scheduler service(mmcss)
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Priority When Yielded", 13, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio", "Priority When Yielded", 13, RegistryValueKind.DWord), null),
			("Optimizing Multimedia Class Scheduler Service (MMCSS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Playback", "Priority When Yielded", 13, RegistryValueKind.DWord), null),

		];
	}
}