using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using AutoOS.Helpers.Services;

namespace AutoOS.Views.Installer.Stages;

public static class ServicesStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // group services
            ("Grouping services", async () => ServicesHelper.GroupServices(), null),

            // disable failure actions
            ("Disabling failure actions", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "InactivityShutdownDelay", 4294967295, RegistryValueKind.DWord), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("AudioEndpointBuilder")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("Appinfo")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("AppXSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("CaptureService")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("cbdhsvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("ClipSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("CryptSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("DevicesFlowUserSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("Dhcp")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("DispBrokerDesktopSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("DoSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("DsmSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("gpsvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("InstallService")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("KeyIso")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("lfsvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("msiserver")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("NcbService")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("netprofm")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("nsi")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("ProfSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("sppsvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("StateRepository")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("TrustedInstaller")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("UdkUserSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("WFDSConMgrSvc")), null),
            ("Disabling failure actions", async () => RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, () => ServicesHelper.DisableFailureActions("Wcmsvc")), null)
        };

        return actions;
    }
}
