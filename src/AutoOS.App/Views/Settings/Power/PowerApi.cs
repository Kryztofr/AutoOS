using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using Windows.Win32;

namespace AutoOS.Views.Settings.Power
{
    internal static unsafe class PowerApi
    {
        public static IntPtr AllocGuid(Guid guid)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
            Marshal.StructureToPtr(guid, ptr, false);
            return ptr;
        }

        internal static string ReadFriendlyName(Guid scheme, Guid? subgroup, Guid? setting)
        {
            uint size = 512;
            byte* pBuffer = stackalloc byte[512];
            uint res = (uint)PInvoke.PowerReadFriendlyName(default, scheme, subgroup, setting, new Span<byte>(pBuffer, 512), ref size);

            if (res == (uint)WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
            {
                byte* pLargeBuffer = stackalloc byte[(int)size];
                res = (uint)PInvoke.PowerReadFriendlyName(default, scheme, subgroup, setting, new Span<byte>(pLargeBuffer, (int)size), ref size);
                return res == 0 ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
            }

            return res == 0 ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        internal static string ReadDescription(Guid scheme, Guid? subgroup = null, Guid? setting = null)
        {
            uint size = 512;
            byte* pBuffer = stackalloc byte[512];
            uint res = (uint)PInvoke.PowerReadDescription(default, scheme, subgroup, setting, new Span<byte>(pBuffer, 512), ref size);

            if (res == (uint)WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
            {
                byte* pLargeBuffer = stackalloc byte[(int)size];
                res = (uint)PInvoke.PowerReadDescription(default, scheme, subgroup, setting, new Span<byte>(pLargeBuffer, (int)size), ref size);
                return res == 0 ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
            }

            return res == 0 ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        internal static string ReadPossibleFriendlyName(Guid subgroup, Guid setting, uint index)
        {
            uint size = 512;
            byte* pBuffer = stackalloc byte[512];
            WIN32_ERROR res = PInvoke.PowerReadPossibleFriendlyName(default, subgroup, setting, index, new Span<byte>(pBuffer, 512), ref size);

            if (res == WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
            {
                byte* pLargeBuffer = stackalloc byte[(int)size];
                res = PInvoke.PowerReadPossibleFriendlyName(default, subgroup, setting, index, new Span<byte>(pLargeBuffer, (int)size), ref size);
                return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
            }

            return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        internal static string ReadPossibleDescription(Guid subgroup, Guid setting, uint index)
        {
            uint size = 512;
            byte* pBuffer = stackalloc byte[512];
            WIN32_ERROR res = PInvoke.PowerReadPossibleDescription(default, subgroup, setting, index, new Span<byte>(pBuffer, 512), ref size);

            if (res == WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
            {
                byte* pLargeBuffer = stackalloc byte[(int)size];
                res = PInvoke.PowerReadPossibleDescription(default, subgroup, setting, index, new Span<byte>(pLargeBuffer, (int)size), ref size);
                return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pLargeBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
            }

            return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pBuffer, 0, (int)size, Encoding.Unicode).TrimEnd('\0') : string.Empty;
        }

        internal static uint ReadAcValueIndex(Guid scheme, Guid subgroup, Guid setting)
        {
            uint value;
            return PInvoke.PowerReadACValueIndex(default, scheme, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS ? value : 0;
        }

        internal static uint ReadDcValueIndex(Guid scheme, Guid subgroup, Guid setting)
        {
            uint value;
            return (WIN32_ERROR)PInvoke.PowerReadDCValueIndex(default, scheme, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS ? value : 0;
        }

        internal static uint WriteACValueIndex(Guid scheme, Guid subgroup, Guid setting, uint value)
        {
            return (uint)PInvoke.PowerWriteACValueIndex(default, &scheme, &subgroup, &setting, value);
        }

        internal static uint WriteDCValueIndex(Guid scheme, Guid subgroup, Guid setting, uint value)
        {
            return (uint)PInvoke.PowerWriteDCValueIndex(default, &scheme, &subgroup, &setting, value);
        }

        internal static void PowerSetActiveScheme(Guid scheme)
        {
            PInvoke.PowerSetActiveScheme(default, scheme);
        }

        internal static uint RestoreDefaultPowerSchemes()
        {
            return (uint)PInvoke.PowerRestoreDefaultPowerSchemes();
        }

        internal static uint ReadValueMin(Guid subgroup, Guid setting)
        {
            uint value;
            return PInvoke.PowerReadValueMin(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS ? value : 0;
        }

        internal static uint ReadValueMax(Guid subgroup, Guid setting)
        {
            uint value;
            return PInvoke.PowerReadValueMax(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS ? value : 0;
        }

        internal static uint ReadValueIncrement(Guid subgroup, Guid setting)
        {
            uint value;
            return PInvoke.PowerReadValueIncrement(default, subgroup, setting, out value) == WIN32_ERROR.ERROR_SUCCESS ? value : 0;
        }

        internal static string ReadValueUnitsSpecifier(Guid subgroup, Guid setting)
        {
            uint size = 512;
            byte* pBuffer = stackalloc byte[512];
            WIN32_ERROR res = PInvoke.PowerReadValueUnitsSpecifier(default, subgroup, setting, new Span<byte>(pBuffer, 512), ref size);

            if (res == WIN32_ERROR.ERROR_MORE_DATA && size > 512 && size <= 4096)
            {
                byte* pLargeBuffer = stackalloc byte[(int)size];
                res = PInvoke.PowerReadValueUnitsSpecifier(default, subgroup, setting, new Span<byte>(pLargeBuffer, (int)size), ref size);
                return res == WIN32_ERROR.ERROR_SUCCESS ? new string((sbyte*)pLargeBuffer, 0, (int)size - 2, Encoding.Unicode) : string.Empty;
            }

            return (res == WIN32_ERROR.ERROR_SUCCESS && size >= 2) ? new string((sbyte*)pBuffer, 0, (int)size - 2, Encoding.Unicode) : string.Empty;
        }

        internal static bool WriteSchemeFriendlyName(Guid scheme, string name)
        {
            string content = (name ?? string.Empty) + "\0";
            uint size = (uint)content.Length * 2;
            byte[] bytes = Encoding.Unicode.GetBytes(content);
            fixed (byte* pBytes = bytes)
            {
                return (uint)PInvoke.PowerWriteFriendlyName(default, &scheme, null, null, pBytes, size) == 0;
            }
        }

        internal static bool WriteSchemeDescription(Guid scheme, string description)
        {
            string content = (description ?? string.Empty) + "\0";
            uint size = (uint)content.Length * 2;
            byte[] bytes = Encoding.Unicode.GetBytes(content);
            fixed (byte* pBytes = bytes)
            {
                return (uint)PInvoke.PowerWriteDescription(default, &scheme, null, null, pBytes, size) == 0;
            }
        }

        internal static Guid DuplicateScheme(Guid guid, string name, string description)
        {
            Guid* pDestGuid = null;
            PInvoke.PowerDuplicateScheme(default, guid, ref pDestGuid);
            if (pDestGuid == null) return Guid.Empty;
            
            Guid newGuid = *pDestGuid;
            PInvoke.LocalFree((HLOCAL)pDestGuid);
            WriteSchemeFriendlyName(newGuid, name);
            WriteSchemeDescription(newGuid, description);

            return newGuid;
        }

        internal static bool DeleteScheme(Guid scheme)
        {
            return (uint)PInvoke.PowerDeleteScheme(default, scheme) == 0;
        }

        internal static Guid GetPlanGuidByName(string name)
        {
            uint index = 0;
            uint size = (uint)sizeof(Guid);
            byte* pBuffer = stackalloc byte[(int)size];

            while (true)
            {
                uint res = (uint)PInvoke.PowerEnumerate(default, null, null, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index++, new Span<byte>(pBuffer, (int)size), ref size);
                if (res != 0) break;

                Guid schemeGuid = new(new ReadOnlySpan<byte>(pBuffer, (int)size));
                if (string.Equals(ReadFriendlyName(schemeGuid, null, null), name, StringComparison.OrdinalIgnoreCase))
                    return schemeGuid;
            }
            return Guid.Empty;
        }
    }
}
