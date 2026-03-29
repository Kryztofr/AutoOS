using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.System.Services;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AutoOS.Helpers.Services;

public static class ServicesHelper
{
    public unsafe static void KillServiceProcess(string baseServiceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_ENUMERATE_SERVICE);
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
        using var scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_CONNECT);
        if (scmHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

        using var serviceHandle = PInvoke.OpenService(scmHandle, serviceName, (uint)PInvoke.SERVICE_START);
        if (serviceHandle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1060) return;
            throw new Win32Exception(error, "OpenService failed");
        }

        if (!PInvoke.StartService(serviceHandle, null))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1056 || error == 1061) return;
            throw new Win32Exception(error, "StartService failed");
        }
    }

    public static void StopService(string serviceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_CONNECT);
        if (scmHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

        using var serviceHandle = PInvoke.OpenService(scmHandle, serviceName, (uint)PInvoke.SERVICE_STOP);
        if (serviceHandle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1060) return;
            throw new Win32Exception(error, "OpenService failed");
        }

        if (!PInvoke.ControlService(serviceHandle, (uint)PInvoke.SERVICE_CONTROL_STOP, out SERVICE_STATUS status))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1062 || error == 1061 || error == 1052) return;
            throw new Win32Exception(error, "ControlService failed");
        }
    }

    internal static unsafe void SetStartupType(string serviceName, SERVICE_START_TYPE startType)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_CONNECT);
        if (scmHandle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        using var serviceHandle = PInvoke.OpenService(scmHandle, serviceName, (uint)PInvoke.SERVICE_CHANGE_CONFIG);
        if (serviceHandle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1060) return;
            throw new Win32Exception(error);
        }

        if (!PInvoke.ChangeServiceConfig(
            (SC_HANDLE)serviceHandle.DangerousGetHandle(),
            (ENUM_SERVICE_TYPE)0xFFFFFFFF,
            startType,
            (SERVICE_ERROR)0xFFFFFFFF,
            null,
            null,
            (uint*)null,
            null,
            null,
            null,
            null))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public unsafe static void DisableFailureActions(string serviceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_CONNECT);
        if (scmHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

        using var serviceHandle = PInvoke.OpenService(scmHandle, serviceName, (uint)PInvoke.SERVICE_ALL_ACCESS);
        if (serviceHandle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1060) return;
            throw new Win32Exception(error, "OpenService failed");
        }

        var actions = stackalloc SC_ACTION[3];
        actions[0] = new SC_ACTION { Type = SC_ACTION_TYPE.SC_ACTION_NONE, Delay = 0 };
        actions[1] = new SC_ACTION { Type = SC_ACTION_TYPE.SC_ACTION_NONE, Delay = 0 };
        actions[2] = new SC_ACTION { Type = SC_ACTION_TYPE.SC_ACTION_NONE, Delay = 0 };

        var failureActions = new SERVICE_FAILURE_ACTIONSW
        {
            dwResetPeriod = 0,
            lpRebootMsg = default,
            lpCommand = default,
            cActions = 3,
            lpsaActions = actions
        };

        if (!PInvoke.ChangeServiceConfig2W(serviceHandle, SERVICE_CONFIG.SERVICE_CONFIG_FAILURE_ACTIONS, &failureActions))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "ChangeServiceConfig2W (Actions) failed");
        }

        var failureFlag = new SERVICE_FAILURE_ACTIONS_FLAG
        {
            fFailureActionsOnNonCrashFailures = false
        };

        if (!PInvoke.ChangeServiceConfig2W(serviceHandle, SERVICE_CONFIG.SERVICE_CONFIG_FAILURE_ACTIONS_FLAG, &failureFlag))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "ChangeServiceConfig2W (Flag) failed");
        }
    }

    public unsafe static void CreateService(string serviceName, string path)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, (uint)PInvoke.SC_MANAGER_CREATE_SERVICE);
        if (scmHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

        fixed (char* pszServiceName = serviceName)
        fixed (char* pszBinPath = path)
        {
            SC_HANDLE rawScmHandle = (SC_HANDLE)scmHandle.DangerousGetHandle();
            SC_HANDLE serviceHandleRaw = PInvoke.CreateService(
                rawScmHandle,
                pszServiceName,
                pszServiceName,
                (uint)PInvoke.SERVICE_ALL_ACCESS,
                ENUM_SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
                SERVICE_START_TYPE.SERVICE_AUTO_START,
                SERVICE_ERROR.SERVICE_ERROR_NORMAL,
                pszBinPath,
                null,
                null,
                null,
                null,
                null);
        }
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
