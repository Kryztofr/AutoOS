using AutoOS.Core.Helpers.Device.Models;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class DeviceStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        return new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // properties -> policies -> write-caching policy
            (@"Enabling ""Enable write caching on the device""", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg add ""%a\Device Parameters\Disk"" /v ""UserWriteCacheSetting"" /t REG_DWORD /d 1 /f") { CreateNoWindow = true }), null),
            (@"Enabling ""Turn off Windows write-cache buffer flushing on the device""", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\SCSI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg add ""%a\Device Parameters\Disk"" /v ""CacheIsPowerProtected"" /t REG_DWORD /d 1 /f") { CreateNoWindow = true }), null),

            // disable drive powersaving features
            ("Disabling drive powersaving features", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for %a in (EnableHIPM EnableDIPM EnableHDDParking) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f") { CreateNoWindow = true }), null),
            ("Disabling drive powersaving features", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for /f ""tokens=*"" %%s in ('reg query ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Enum"" /S /F ""StorPort"" ^| findstr /e ""StorPort""') do reg add ""%%s"" /v ""EnableIdlePowerManagement"" /t REG_DWORD /d ""0"" /f") { CreateNoWindow = true }), null),
            ("Disabling drive powersaving features", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for %a in (EnhancedPowerManagementEnabled AllowIdleIrpInD3 EnableSelectiveSuspend DeviceSelectiveSuspended SelectiveSuspendEnabled SelectiveSuspendOn EnumerationRetryCount ExtPropDescSemaphore WaitWakeEnabled D3ColdSupported WdfDirectedPowerTransitionEnable EnableIdlePowerManagement IdleInWorkingState IdleTimeoutInMS MinimumIdleTimeoutInMS WakeEnabled) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f") { CreateNoWindow = true }), null),
            ("Disabling drive powersaving features", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for %a in (DisableIdlePowerManagement DisableRuntimePowerManagement) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 1 /f") { CreateNoWindow = true }), null),

            // disable dma remapping
            ("Disabling DMA remapping", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for %a in (DmaRemappingCompatible) do for /f ""delims="" %b in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services"" /s /f ""%a"" ^| findstr ""HKEY""') do reg add ""%b"" /v ""%a"" /t REG_DWORD /d 0 /f") { CreateNoWindow = true }), null),

            // enable msi mode for supported devices
            ("Enabling MSI mode for supported devices", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\PCI"" ^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i"" ^| findstr ""HKEY""') do @for /f ""tokens=3"" %v in ('reg query ""%a\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v MSISupported 2^>nul ^| findstr MSISupported') do @if ""%v""==""0x0"" reg add ""%a\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v MSISupported /t REG_DWORD /d 1 /f") { CreateNoWindow = true }), null),

            // set msi mode to undefined for all devices
            ("Setting MSI mode to undefined for all devices", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("cmd.exe", @"/c for /f ""tokens=*"" %i in ('reg query ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\PCI""^| findstr ""HKEY""') do for /f ""tokens=*"" %a in ('reg query ""%i""^| findstr ""HKEY""') do reg delete ""%a\Device Parameters\Interrupt Management\Affinity Policy"" /v ""DevicePriority"" /f") { CreateNoWindow = true }), null),

            // disable asmedia usb controllers
            ("Disabling ASMedia USB controllers", async () => await ProcessActions.RunPowerShell(@"Get-PnpDevice -FriendlyName ""*ASMedia USB*"" | Disable-PnpDevice -Confirm:$false"), null),

            // disable xhci interrupt moderation (imod)
            ("Disabling XHCI Interrupt Moderation (IMOD)", async () => { foreach (var device in DeviceHelper.GetDevices(DeviceType.XHCI)) DeviceHelper.ToggleImod(device, false); }, null),
            
            // disable reserved storage
            ("Disabling reserved storage", async () => await ProcessActions.RunPowerShell(@"DISM /Online /Set-ReservedStorageState /State:Disabled"), null),

            // optimize raw mouse throttling
            ("Setting raw mouse throttle duration to 20 ms", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\Mouse", "RawMouseThrottleDuration", 20, RegistryValueKind.DWord), null),
            ("Setting raw mouse throttle leeway to 0ms", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Control Panel\Mouse", "RawMouseThrottleLeeway", 0, RegistryValueKind.DWord), null)
        };
    }
}

