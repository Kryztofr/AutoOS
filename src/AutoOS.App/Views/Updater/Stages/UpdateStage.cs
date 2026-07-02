using AutoOS.Core.Helpers.Power;
using AutoOS.Core.Helpers.Registry;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		string[] services =
		[
			"AppXSvc",
			"AudioEndpointBuilder",
			"BITS",
			"BrokerInfrastructure",
			"CDPSvc",
			"ClipSVC",
			"CoreMessagingRegistrar",
			"DcomLaunch",
			"DeviceAssociationService",
			"Dhcp",
			"DispBrokerDesktopSvc",
			"DisplayEnhancementService",
			"Dnscache",
			"DPS",
			"EventLog",
			"EventSystem",
			"FDResPub",
			"FontCache",
			"hidserv",
			"iphlpsvc",
			"KeyIso",
			"LanmanServer",
			"LanmanWorkstation",
			"LicenseManager",
			"lmhosts",
			"LSM",
			"NcbService",
			"NcdAutoSetup",
			"NlaSvc",
			"nsi",
			"PcaSvc",
			"Power",
			"SamSs",
			"Schedule",
			"SENS",
			"ShellHWDetection",
			"SSDPSRV",
			"SstpSvc",
			"StorSvc",
			"SysMain",
			"SystemEventsBroker",
			"Themes",
			"TimeBrokerSvc",
			"TokenBroker",
			"TrkWks",
			"UsoSvc",
			"VaultSvc",
			"WdiSystemHost",
			"WinHttpAutoProxySvc",
			"WpnService",
			"wuauserv"
		];

		Guid guid = Guid.Empty;

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>()
		{
			// select autoos power plan
            ("Selecting AutoOS Power Plan", async () => guid = PowerHelper.GetPlanGuidByName("AutoOS"), null),

			// set "interrupt steering mode" to "lock interrupt routing"
			(@"Setting ""Interrupt Steering Mode"" to ""Lock Interrupt Routing""", async () => PowerHelper.WriteACValueIndex(guid, new Guid("48672f38-7a9a-4bb2-8bf8-3d85be19de4e"), new Guid("2bfc24f9-5ea2-4801-8213-3dbae01aa39d"), 4), null),
			(@"Setting ""Interrupt Steering Mode"" to ""Lock Interrupt Routing""", async () => PowerHelper.WriteDCValueIndex(guid, new Guid("48672f38-7a9a-4bb2-8bf8-3d85be19de4e"), new Guid("2bfc24f9-5ea2-4801-8213-3dbae01aa39d"), 4), null),
		
			// apply changes
			("Applying Changes", async () =>  PowerHelper.PowerSetActiveScheme(guid), null)
		};

		foreach (var service in services)
		{
			actions.Add(($"Reverting grouping services", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "SvcHostSplitDisable"), null));
		}

		return actions;
	}
}
