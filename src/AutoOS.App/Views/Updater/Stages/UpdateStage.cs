using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Registry;
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
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature", 1073741888, RegistryValueKind.DWord), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature2", 64, RegistryValueKind.DWord), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnablePerformanceMode", 1, RegistryValueKind.DWord), null));
			actions.Add((@"Removing ""RmPerfLimitsOverride""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPerfLimitsOverride"), null));
			actions.Add((@"Removing ""RmSkipHdcp22Init""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmSkipHdcp22Init"), null));
			actions.Add((@"Removing ""RMNoECCFuseCheck""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNoECCFuseCheck"), null));
			actions.Add((@"Removing ""RMGuestECCState""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMGuestECCState"), null));
			actions.Add((@"Removing ""MaxPerfWithPerfMon""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "MaxPerfWithPerfMon"), null));
			actions.Add((@"Removing ""RMAERRForceDisable""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMAERRForceDisable"), null));
			actions.Add((@"Removing ""RMBlcg""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMBlcg"), null));
			actions.Add((@"Removing ""RMCtxswLog""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMCtxswLog"), null));
			actions.Add((@"Removing ""RMDebugSetSMCMode""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDebugSetSMCMode"), null));
			actions.Add((@"Removing ""RMDeepL1EntryLatencyUsec""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDeepL1EntryLatencyUsec"), null));
			actions.Add((@"Removing ""RMDisableFeatureDisablement""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableFeatureDisablement"), null));
			actions.Add((@"Removing ""RMElpg""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMElpg"), null));
			actions.Add((@"Removing ""RMEnableEventTracer""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableEventTracer"), null));
			actions.Add((@"Removing ""RMFspg""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMFspg"), null));
			actions.Add((@"Removing ""RMHotPlugSupportDisable""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMHotPlugSupportDisable"), null));
			actions.Add((@"Removing ""RMIntrDetailedLogs""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMIntrDetailedLogs"), null));
			actions.Add((@"Removing ""RMNativePcieL1WarFlags""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNativePcieL1WarFlags"), null));
			actions.Add((@"Removing ""RMNvLinkControlLinkPM""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNvLinkControlLinkPM"), null));
			actions.Add((@"Removing ""RMNvlinkUPHYInitControl""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNvlinkUPHYInitControl"), null));
			actions.Add((@"Removing ""RMNvLog""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNvLog"), null));
			actions.Add((@"Removing ""RMPcieLtrOverride""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMPcieLtrOverride"), null));
			actions.Add((@"Removing ""RMResetPerfMonD4""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMResetPerfMonD4"), null));
			actions.Add((@"Removing ""RMSlcg""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMSlcg"), null));
			actions.Add((@"Removing ""RmBreak""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmBreak"), null));
			actions.Add((@"Removing ""RmBreakonRC""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmBreakonRC"), null));
			actions.Add((@"Removing ""RmDisableInforomNvlink""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableInforomNvlink"), null));
			actions.Add((@"Removing ""RmDisablePreosapps""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisablePreosapps"), null));
			actions.Add((@"Removing ""RmDisableRegistryCaching""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableRegistryCaching"), null));
			actions.Add((@"Removing ""RmEnableI2CNanny""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmEnableI2CNanny"), null));
			actions.Add((@"Removing ""RmFbsrPagedDMA""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmFbsrPagedDMA"), null));
			actions.Add((@"Removing ""RmHulkDisableFeatures""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmHulkDisableFeatures"), null));
			actions.Add((@"Removing ""RmIgnoreHulkErrors""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmIgnoreHulkErrors"), null));
			actions.Add((@"Removing ""RmLogonRC""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLogonRC"), null));
			actions.Add((@"Removing ""RmRcWatchdog""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmRcWatchdog"), null));
			actions.Add((@"Removing ""RmSec2EnableApm""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmSec2EnableApm"), null));
			actions.Add((@"Removing ""SetPanelRefreshRate""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "SetPanelRefreshRate"), null));
			actions.Add((@"Removing ""SkipSwStateErrChecks""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "SkipSwStateErrChecks"), null));
			actions.Add((@"Removing ""RmOverrideSupportChipsetAspm""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmOverrideSupportChipsetAspm"), null));
			actions.Add((@"Removing ""RMEnableASPMDT""", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableASPMDT"), null));
		}

		return actions;
	}
}