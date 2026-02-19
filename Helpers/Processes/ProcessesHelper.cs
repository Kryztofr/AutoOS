using System.Text;
using Windows.Wdk.System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using System.Diagnostics;

namespace AutoOS.Helpers.Processes;

public static partial class ProcessesHelper
{
    public static unsafe string GetCommandLine(Process proc)
    {
        HANDLE handle = PInvoke.OpenProcess(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ,
            false,
            (uint)proc.Id);

        if ((IntPtr)handle.Value == IntPtr.Zero) return string.Empty;

        try
        {
            PROCESS_BASIC_INFORMATION pbi = new();
            uint returnLength;
            nuint bytesRead;

            NTSTATUS status = Windows.Wdk.PInvoke.NtQueryInformationProcess(
                handle,
                PROCESSINFOCLASS.ProcessBasicInformation,
                &pbi,
                (uint)sizeof(PROCESS_BASIC_INFORMATION),
                &returnLength);

            if (status.Value != 0) return string.Empty;

            IntPtr pebAddress = (IntPtr)pbi.PebBaseAddress;
            if (pebAddress == IntPtr.Zero) return string.Empty;

            IntPtr processParametersOffset = pebAddress + (IntPtr.Size == 8 ? 0x20 : 0x10);
            IntPtr processParametersPtr = IntPtr.Zero;

            if (!PInvoke.ReadProcessMemory(handle, (void*)processParametersOffset, &processParametersPtr, (uint)IntPtr.Size, &bytesRead))
                return string.Empty;

            IntPtr commandLineUnicodeStringPtr = processParametersPtr + (IntPtr.Size == 8 ? 0x70 : 0x40);

            byte[] unicodeStringHeader = new byte[16];
            fixed (byte* pHeader = unicodeStringHeader)
            {
                if (!PInvoke.ReadProcessMemory(handle, (void*)commandLineUnicodeStringPtr, pHeader, (uint)(IntPtr.Size == 8 ? 16 : 8), &bytesRead))
                    return string.Empty;
            }

            ushort len = BitConverter.ToUInt16(unicodeStringHeader, 0);
            IntPtr bufferPtr = (IntPtr.Size == 8)
                ? (IntPtr)BitConverter.ToInt64(unicodeStringHeader, 8)
                : (IntPtr)BitConverter.ToInt32(unicodeStringHeader, 4);

            if (len == 0 || bufferPtr == IntPtr.Zero) return string.Empty;

            byte[] commandLineBuffer = new byte[len];
            fixed (byte* pCmd = commandLineBuffer)
            {
                if (!PInvoke.ReadProcessMemory(handle, (void*)bufferPtr, pCmd, len, &bytesRead))
                    return string.Empty;
            }

            return Encoding.Unicode.GetString(commandLineBuffer);
        }
        finally
        {
            PInvoke.CloseHandle(handle);
        }
    }
}