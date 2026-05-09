using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord"));

		var gpus = GpuHelper.GetGPUs().Where(gpu => gpu.NVIDIA);

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			 // optimize discord settings
             ("Optimizing Discord settings", async () => await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord", "settings.json"), "{\n  \"enableHardwareAcceleration\": false,\n  \"OPEN_ON_STARTUP\": false,\n  \"MINIMIZE_TO_TRAY\": false,\n  \"debugLogging\": false,\n  \"openasar\": {\n    \"setup\": true,\n    \"noTrack\": false\n  }\n}"), () => Discord == true),

			 // change account display name to "autoos"
             (@"Changing Account Display Name to ""AutoOS""", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.CurrentUser, new ProcessStartInfo("net.exe", @"user user /fullname:""AutoOS""") { CreateNoWindow = true }), null),
		};

		foreach (var gpu in gpus)
		{
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature", 1073741888, RegistryValueKind.DWord), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature2", 64, RegistryValueKind.DWord), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "EnablePerformanceMode", 1, RegistryValueKind.DWord), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPerfLimitsOverride"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmSkipHdcp22Init"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNoECCFuseCheck"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMGuestECCState"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "MaxPerfWithPerfMon"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMAERRForceDisable"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMBlcg"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMCtxswLog"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDebugSetSMCMode"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDeepL1EntryLatencyUsec"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMDisableFeatureDisablement"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMElpg"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableEventTracer"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMFspg"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMHotPlugSupportDisable"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMIntrDetailedLogs"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNativePcieL1WarFlags"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNvLinkControlLinkPM"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNvlinkUPHYInitControl"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMNvLog"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMPcieLtrOverride"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMResetPerfMonD4"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMSlcg"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmBreak"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmBreakonRC"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableInforomNvlink"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisablePreosapps"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmDisableRegistryCaching"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmEnableI2CNanny"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmFbsrPagedDMA"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmHulkDisableFeatures"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmIgnoreHulkErrors"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmLogonRC"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmRcWatchdog"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmSec2EnableApm"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "SetPanelRefreshRate"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "SkipSwStateErrChecks"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmOverrideSupportChipsetAspm"), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMEnableASPMDT"), null));
		}

		return actions;
	}
}