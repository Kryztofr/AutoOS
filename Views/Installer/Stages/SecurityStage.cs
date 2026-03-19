using AutoOS.Views.Installer.Actions;
using AutoOS.Helpers.Registry;
using System.Diagnostics;
using AutoOS.Helpers.Services;
using Microsoft.Win32;
using System.ServiceProcess;

namespace AutoOS.Views.Installer.Stages;

public static class SecurityStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        bool WindowsDefender = PreparingStage.WindowsDefender;
        bool UserAccountControl = PreparingStage.UserAccountControl;
        bool DEP = PreparingStage.DEP;
        bool MemoryIntegrity = PreparingStage.MemoryIntegrity;
        bool VirtualizationBasedSecurity = PreparingStage.VirtualizationBasedSecurity;
        bool INTELCPU = PreparingStage.INTELCPU;
        bool AMDCPU = PreparingStage.AMDCPU;
        bool SpectreMeltdownMitigations = PreparingStage.SpectreMeltdownMitigations;
        bool ProcessMitigations = PreparingStage.ProcessMitigations;

        return new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // enable "hide windows security systray" policy
            (@"Enabling ""Hide Windows Security Systray"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray", "HideSystray", 1, RegistryValueKind.DWord), null),
            
            // remove "securityhealthsystray" from startup
            (@"Removing ""SecurityHealthSystray"" from startup", async () => RegistryHelper.DeleteValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth"), null),

            // enable "do not preserve zone information in file attachments"
            (@"Enabling ""Do not preserve zone information in file attachments""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation", 1, RegistryValueKind.DWord), null),
            
            // set "inclusion list for moderate risk file types"" policy to ".bat;.cmd;.vbs;.ps1;.reg;.js;.exe;.msi;"
            (@"Setting ""Inclusion list for moderate risk file types"" policy to "".bat;.cmd;.vbs;.ps1;.reg;.js;.exe;.msi;""", async () => RegistryHelper.SetValue(RegistryHelper.Identity.CurrentUser, @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Associations", "ModRiskFileTypes", ".bat;.cmd;.vbs;.reg;.js;.exe;.msi;", RegistryValueKind.String), null),

            // set execution policy to unrestricted
            ("Setting execution policy to unrestricted", async () => await ProcessActions.RunPowerShell("Set-ExecutionPolicy Unrestricted -Force"), null),

            // disable windows defender
            ("Disabling Windows Defender", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.StopService("wscsvc")), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender", "PassiveMode", 1, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiVirus", 1, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo(@"C:\Program Files\Windows Defender\MpCmdRun.exe", "-DisableService -HighPriority") { CreateNoWindow = true }), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => { while (new ServiceController("WdFilter").Status != ServiceControllerStatus.Stopped) await Task.Delay(100); }, () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),
            ("Disabling Windows Defender", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc", "Start", 4, RegistryValueKind.DWord), () => WindowsDefender == false),

            // disable "turn on telemetry for defender core service"
            (@"Disabling ""Turn on telemetry for Defender core service"" policy", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender\Features", "DisableCoreService1DSTelemetry", 1, RegistryValueKind.DWord), null),

            // disable smartscreen
            ("Disabling Smartscreen", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { foreach (var p in Process.GetProcessesByName("smartscreen")) p.Kill(); File.Move(@"C:\Windows\System32\smartscreen.exe", @"C:\Windows\System32\smartscreen.exee"); }), null),

            // enable windows hardware quality labs (whql) driver enforcement
            ("Enabling Windows Hardware Quality Labs (WHQL) driver enforcement", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CI\Policy", "WhqlSettings", 1, RegistryValueKind.DWord), null),

            // enable uac
            ("Enabling user account control (UAC)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 1, RegistryValueKind.DWord), () => UserAccountControl == true),
            ("Enabling user account control (UAC)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "PromptOnSecureDesktop", 1, RegistryValueKind.DWord), () => UserAccountControl == true),
            ("Enabling user account control (UAC)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin", 5, RegistryValueKind.DWord), () => UserAccountControl == true),

            // disable data execution prevention (dep)
            ("Disabling data execution prevention (DEP)", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, new ProcessStartInfo("bcdedit", "/set nx AlwaysOff") { CreateNoWindow = true }), () => DEP == false),

            // disable hypervisor enforced code integrity (hvci)
            ("Disabling Hypervisor Enforced Code Integrity (HVCI)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0, RegistryValueKind.DWord), () => MemoryIntegrity == false),
            
            // disable virtualization-based security (VBS)
            ("Disabling Virtualization-based Security (VBS)", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord), () => VirtualizationBasedSecurity == false),

            // enable spectre and meltdown mitigations
            ("Enabling Spectre & Meltdown Mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1, RegistryValueKind.DWord), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 64, RegistryValueKind.DWord), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 0, RegistryValueKind.DWord), () => INTELCPU == true && SpectreMeltdownMitigations == true),

            // disable spectre and meltdown mitigations
            ("Disabling Spectre & Meltdown Mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1, RegistryValueKind.DWord), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3, RegistryValueKind.DWord), () => SpectreMeltdownMitigations == false),
            
            // disable process mitigations
            ("Disabling process mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "MitigationAuditOptions", Enumerable.Repeat((byte)0x22, 24).ToArray(), RegistryValueKind.Binary), () => ProcessMitigations == false),
            ("Disabling process mitigations", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "MitigationOptions", Enumerable.Repeat((byte)0x22, 24).ToArray(), RegistryValueKind.Binary), () => ProcessMitigations == false),
        };
    }
}
