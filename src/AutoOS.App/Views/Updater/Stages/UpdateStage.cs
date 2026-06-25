using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		bool RiotClient = File.Exists(Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "Riot Games", "Riot Client", "RiotClientServices.exe"));
		bool Valorant = File.Exists(Path.Combine(RiotHelper.RiotGamesMetadataPath, "valorant.live", "valorant.live.product_settings.yaml")) && !string.IsNullOrEmpty(Regex.Match(File.ReadAllText(Path.Combine(RiotHelper.RiotGamesMetadataPath, "valorant.live", "valorant.live.product_settings.yaml")), @"product_install_full_path:\s*(.+)").Groups[1].Value.Trim());
		bool HVCI = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0) is int val && val == 1;
		bool VBS = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello", "Enabled", 0) is int helloVal && helloVal == 1) || (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0) is int vbsVal && vbsVal == 1);

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// disable legacy context menu
			("Disable Legacy Context Menu", async () => Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false), null),
			("Disable Legacy Context Menu", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = $@"load HKU\DefaultUser ""{Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users", "Default", "NTUSER.DAT")}""", CreateNoWindow = true })!.WaitForExitAsync(), null),
			("Disable Legacy Context Menu", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\StartIsBack", "NoXAMLMenus", 0, RegistryValueKind.DWord, true), null),
			("Disable Legacy Context Menu", async () => await Process.Start(new ProcessStartInfo { FileName = "reg.exe", Arguments = @"unload HKU\DefaultUser", CreateNoWindow = true })!.WaitForExitAsync(), null),

			// enable vanguard on demand
			("Enabling Vanguard on demand", async () => { Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games", "Riot Vanguard")); await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games", "Riot Vanguard", "vgtray-settings.json"), "{\"theme\":0,\"language\":0,\"precheck_option\":1}"); }, () => RiotClient == true),

			// enable hypervisor enforced code integrity (hvci)
			("Enabling Hypervisor Enforced Code Integrity (HVCI)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1, RegistryValueKind.DWord), () => Valorant == true && HVCI == false),

			// enable vbs
			("Enabling Virtualization Based Security (VBS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 1, RegistryValueKind.DWord), () => Valorant == true && VBS == false),
		};

		return actions;
	}
}
