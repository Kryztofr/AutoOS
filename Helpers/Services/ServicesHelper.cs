using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.System.Services;

namespace AutoOS.Helpers.Services;

public static class ServicesHelper
{
    public unsafe static void KillServiceProcess(string baseServiceName)
    {
        using var scmHandle = PInvoke.OpenSCManager(null, null, 0x0004);
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
}