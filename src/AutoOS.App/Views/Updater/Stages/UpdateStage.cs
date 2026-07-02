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

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>();

		foreach (var service in services)
		{
			actions.Add(($"Reverting grouping services", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "SvcHostSplitDisable"), null));
		}

		return actions;
	}
}
