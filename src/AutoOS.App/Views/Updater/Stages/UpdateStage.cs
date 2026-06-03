using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// disable full screen exclusive (FSE) mode 
            ("Disabling Full Screen Exclusive (FSE) Mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_DSEBehavior", 0, RegistryValueKind.DWord), null),
			("Disabling Full Screen Exclusive (FSE) Mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 0, RegistryValueKind.DWord), null),
			("Disabling Full Screen Exclusive (FSE) Mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_FSEBehavior", 0, RegistryValueKind.DWord), null),
			("Disabling Full Screen Exclusive (FSE) Mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 0, RegistryValueKind.DWord), null),
			("Disabling Full Screen Exclusive (FSE) Mode", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 0, RegistryValueKind.DWord), null)
		};

		return actions;
	}
}