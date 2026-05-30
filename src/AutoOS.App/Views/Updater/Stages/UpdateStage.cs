using AutoOS.Core.Helpers.GPU;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;
using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		var gpus = GpuHelper.GetGPUs().Where(gpu => gpu.NVIDIA);

		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// fix msi afterburner shortcut
			("Fixing MSI Afterburner shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\MSI Afterburner\MSI Afterburner.lnk")), null),
			("Fixing MSI Afterburner shortcut", async () => File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\MSI Afterburner\Uninstall.lnk")), null),
			("Fixing MSI Afterburner shortcut", async () => await ProcessActions.RunPowerShell(@"$Shell=New-Object -ComObject WScript.Shell; $msiPath=[System.IO.Path]::Combine(${env:ProgramFiles(x86)}, 'MSI Afterburner'); @(@{P='MSI Afterburner.lnk';T=[System.IO.Path]::Combine($msiPath, 'MSIAfterburner.exe')},@{P='ReadMe.lnk';T=[System.IO.Path]::Combine($msiPath, 'Doc', 'ReadMe.pdf')},@{P='Uninstall.lnk';T=[System.IO.Path]::Combine($msiPath, 'Uninstall.exe')},@{P='SDK\MSI Afterburner localization reference.lnk';T=[System.IO.Path]::Combine($msiPath, 'SDK', 'Doc', 'Localization reference.pdf')},@{P='SDK\MSI Afterburner skin format reference.lnk';T=[System.IO.Path]::Combine($msiPath, 'SDK', 'Doc', 'USF skin format reference.pdf')},@{P='SDK\Samples.lnk';T=[System.IO.Path]::Combine($msiPath, 'SDK', 'Samples\')}) | % {$Shortcut=$Shell.CreateShortcut([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\MSI Afterburner', $_.P)); $Shortcut.TargetPath=$_.T; $Shortcut.Save()}"), null),

		};

		foreach (var gpu in gpus)
		{
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature", 1413829989, RegistryValueKind.DWord), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RmPowerFeature2", 89478485, RegistryValueKind.DWord), null));
			actions.Add(("Configuring Miscellaneous NVIDIA Settings", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, gpu.RegistryPath, "RMElcg", 1431655764, RegistryValueKind.DWord), null));
		}

		return actions;
	}
}