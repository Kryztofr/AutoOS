using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.System.Services;

namespace AutoOS.Helpers.Services;

public static class ServicesHelper
{
    public unsafe static void KillServiceProcess(string baseServiceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, PInvoke.SERVICE_QUERY_CONFIG);
        if (scmHandle.IsInvalid) return;

        SC_HANDLE rawScmHandle = (SC_HANDLE)scmHandle.DangerousGetHandle();
        uint bytesNeeded = 0;
        uint servicesReturned = 0;
        uint resumeHandle = 0;

        PInvoke.EnumServicesStatusEx(
            rawScmHandle,
            SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
            ENUM_SERVICE_TYPE.SERVICE_WIN32,
            ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
            null,
            0,
            &bytesNeeded,
            &servicesReturned,
            &resumeHandle,
            null);

        if (bytesNeeded == 0) return;

        byte[] buffer = new byte[bytesNeeded];
        fixed (byte* pBuffer = buffer)
        {
            if (PInvoke.EnumServicesStatusEx(
                rawScmHandle,
                SC_ENUM_TYPE.SC_ENUM_PROCESS_INFO,
                ENUM_SERVICE_TYPE.SERVICE_WIN32,
                ENUM_SERVICE_STATE.SERVICE_STATE_ALL,
                pBuffer,
                (uint)buffer.Length,
                &bytesNeeded,
                &servicesReturned,
                &resumeHandle,
                null))
            {
                var services = (ENUM_SERVICE_STATUS_PROCESSW*)pBuffer;
                for (int i = 0; i < servicesReturned; i++)
                {
                    string currentName = services[i].lpServiceName.ToString();
                    if (currentName.StartsWith(baseServiceName, StringComparison.OrdinalIgnoreCase))
                    {
                        int pid = (int)services[i].ServiceStatusProcess.dwProcessId;
                        if (pid != 0)
                        {
                            try
                            {
                                using var proc = Process.GetProcessById(pid);
                                proc.Kill();
                                proc.WaitForExit();
                            }
                            catch { }
                        }
                    }
                }
            }
        }
    }

    public static void StartService(string serviceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, PInvoke.SC_MANAGER_CONNECT);
        if (scmHandle.IsInvalid) return;

        using var serviceHandle = PInvoke.OpenService(scmHandle, serviceName, PInvoke.SERVICE_START);
        if (serviceHandle.IsInvalid) return;

        PInvoke.StartService(serviceHandle, null);
    }

    public static void StopService(string serviceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, PInvoke.SC_MANAGER_CONNECT);
        if (scmHandle.IsInvalid) return;

        using var serviceHandle = PInvoke.OpenService(scmHandle, serviceName, PInvoke.SERVICE_STOP);
        if (serviceHandle.IsInvalid) return;

        PInvoke.ControlService(serviceHandle, PInvoke.SERVICE_CONTROL_STOP, out SERVICE_STATUS status);
    }

    public unsafe static void DisableFailureActions(string serviceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, PInvoke.SC_MANAGER_CONNECT);
        if (scmHandle.IsInvalid) return;

        using var serviceHandle = PInvoke.OpenService(scmHandle, serviceName, PInvoke.SERVICE_QUERY_CONFIG | PInvoke.SERVICE_CHANGE_CONFIG);
        if (serviceHandle.IsInvalid) return;

        var failureActions = new SERVICE_FAILURE_ACTIONSW
        {
            dwResetPeriod = 0,
            lpRebootMsg = null,
            lpCommand = null,
            cActions = 0,
            lpsaActions = null
        };

        PInvoke.ChangeServiceConfig2W(serviceHandle, SERVICE_CONFIG.SERVICE_CONFIG_FAILURE_ACTIONS, &failureActions);

        var failureFlag = new SERVICE_FAILURE_ACTIONS_FLAG
        {
            fFailureActionsOnNonCrashFailures = false
        };

        PInvoke.ChangeServiceConfig2W(serviceHandle, SERVICE_CONFIG.SERVICE_CONFIG_FAILURE_ACTIONS_FLAG, &failureFlag);
    }

    public static void CreateService(string serviceName, string binPath)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, PInvoke.SC_MANAGER_CREATE_SERVICE);
        if (scmHandle.IsInvalid) return;

        uint tagId;
        using var serviceHandle = PInvoke.CreateService(
            scmHandle,
            serviceName,
            serviceName,
            0xF01FF,
            ENUM_SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
            SERVICE_START_TYPE.SERVICE_AUTO_START,
            SERVICE_ERROR.SERVICE_ERROR_NORMAL,
            binPath,
            null,
            out tagId,
            null,
            null,
            null);
    }

    public static void GroupServices()
    {
        string[] services =
        [
            "AppXSvc", "AudioEndpointBuilder", "BITS", "BrokerInfrastructure", "CDPSvc",
            "ClipSVC", "CoreMessagingRegistrar", "DcomLaunch", "DeviceAssociationService",
            "Dhcp", "DispBrokerDesktopSvc", "DisplayEnhancementService", "Dnscache",
            "DPS", "EventLog", "EventSystem", "FDResPub", "FontCache", "hidserv",
            "iphlpsvc", "KeyIso", "LanmanServer", "LanmanWorkstation", "LicenseManager",
            "lmhosts", "LSM", "NcbService", "NcdAutoSetup", "NlaSvc", "nsi", "PcaSvc",
            "Power", "SamSs", "Schedule", "SENS", "ShellHWDetection", "SSDPSRV",
            "SstpSvc", "StorSvc", "SysMain", "SystemEventsBroker", "Themes",
            "TimeBrokerSvc", "TokenBroker", "TrkWks", "UsoSvc", "VaultSvc",
            "WdiSystemHost", "WinHttpAutoProxySvc", "WpnService", "wuauserv"
        ];

        string[] userServices = ["CDPUserSvc_", "OneSyncSvc_", "WpnUserService_"];

        foreach (var service in services)
        {
            RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}", "SvcHostSplitDisable", 1, RegistryValueKind.DWord);
        }

        using var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
        foreach (string subKeyName in baseKey.GetSubKeyNames())
        {
            foreach (var prefix in userServices)
            {
                if (subKeyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{subKeyName}", "SvcHostSplitDisable", 1, RegistryValueKind.DWord);
                }
            }
        }
    }
}
