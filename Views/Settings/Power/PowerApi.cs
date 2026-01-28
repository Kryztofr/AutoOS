using System.Runtime.InteropServices;
using System.Text;

namespace AutoOS.Views.Settings.Power
{
    internal enum PowerDataAccessor : uint
    {
        AccessScheme = 16,
        AccessSubgroup = 17,
        AccessIndividualSetting = 18,
        AccessPossiblePowerSetting = 22
    }

    internal static partial class PowerApi
    {
        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr LocalFree(IntPtr hMem);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerEnumerate(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            PowerDataAccessor AccessFlags,
            uint Index,
            byte[] Buffer,
            ref uint BufferSize);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint PowerReadFriendlyName(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            byte[] Buffer,
            ref uint BufferSize);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint PowerReadDescription(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            byte[] Buffer,
            ref uint BufferSize);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        internal static partial uint PowerReadPossibleFriendlyName(
            IntPtr RootPowerKey,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            uint PossibleSettingIndex,
            [Out] byte[] Buffer,
            ref uint BufferSize);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        internal static partial uint PowerReadPossibleDescription(
            IntPtr RootPowerKey,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            uint PossibleSettingIndex,
            [Out] byte[] Buffer,
            ref uint BufferSize);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerGetActiveScheme(
            IntPtr UserRootPowerKey,
            out IntPtr ActivePolicyGuid);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerSetActiveScheme(
            IntPtr UserRootPowerKey,
            ref Guid SchemeGuid);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerReadACValueIndex(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            out uint AcValueIndex);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerReadDCValueIndex(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            out uint DcValueIndex);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerWriteACValueIndex(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            uint AcValueIndex);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerWriteDCValueIndex(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            uint DcValueIndex);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint PowerImportPowerScheme(
            IntPtr RootPowerKey,
            string FileName,
            out IntPtr DestinationSchemeGuid);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerReadValueMin(
            IntPtr RootPowerKey,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            out uint ValueMinimum);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerReadValueMax(
            IntPtr RootPowerKey,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            out uint ValueMaximum);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerReadValueIncrement(
            IntPtr RootPowerKey,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            out uint ValueIncrement);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint PowerReadValueUnitsSpecifier(
            IntPtr RootPowerKey,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            byte[] Buffer,
            ref uint BufferSize);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint PowerWriteFriendlyName(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            string Buffer,
            uint BufferSize);

        [LibraryImport("powrprof.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint PowerWriteDescription(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            IntPtr SubGroupGuid,
            IntPtr PowerSettingGuid,
            string Buffer,
            uint BufferSize);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerDeleteScheme(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid);

        public static IntPtr AllocGuid(Guid guid)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
            Marshal.StructureToPtr(guid, ptr, false);
            return ptr;
        }

        internal static string ReadFriendlyName(Guid scheme, Guid? subgroup, Guid? setting)
        {
            IntPtr schemePtr = AllocGuid(scheme);
            IntPtr subgroupPtr = subgroup.HasValue ? AllocGuid(subgroup.Value) : IntPtr.Zero;
            IntPtr settingPtr = setting.HasValue ? AllocGuid(setting.Value) : IntPtr.Zero;

            try
            {
                uint size = 0;
                PowerReadFriendlyName(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, null, ref size);
                if (size == 0) return string.Empty;

                byte[] buffer = new byte[size];
                uint res = PowerReadFriendlyName(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, buffer, ref size);
                return res == 0 ? Encoding.Unicode.GetString(buffer).TrimEnd('\0') : string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(schemePtr);
                if (subgroupPtr != IntPtr.Zero) Marshal.FreeHGlobal(subgroupPtr);
                if (settingPtr != IntPtr.Zero) Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static string ReadDescription(Guid scheme, Guid? subgroup = null, Guid? setting = null)
        {
            IntPtr schemePtr = AllocGuid(scheme);
            IntPtr subgroupPtr = subgroup.HasValue ? AllocGuid(subgroup.Value) : IntPtr.Zero;
            IntPtr settingPtr = setting.HasValue ? AllocGuid(setting.Value) : IntPtr.Zero;

            try
            {
                uint size = 0;
                PowerReadDescription(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, null, ref size);
                if (size == 0) return string.Empty;

                byte[] buffer = new byte[size];
                uint res = PowerReadDescription(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, buffer, ref size);
                return res == 0 ? Encoding.Unicode.GetString(buffer).TrimEnd('\0') : string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(schemePtr);
                if (subgroupPtr != IntPtr.Zero) Marshal.FreeHGlobal(subgroupPtr);
                if (settingPtr != IntPtr.Zero) Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static string ReadPossibleFriendlyName(Guid subgroup, Guid setting, uint index)
        {
            uint size = 0;
            PowerReadPossibleFriendlyName(IntPtr.Zero, ref subgroup, ref setting, index, null, ref size);
            if (size == 0) return string.Empty;

            byte[] buffer = new byte[size];
            uint res = PowerReadPossibleFriendlyName(IntPtr.Zero, ref subgroup, ref setting, index, buffer, ref size);
            return res == 0 ? Encoding.Unicode.GetString(buffer).TrimEnd('\0') : string.Empty;
        }

        internal static string ReadPossibleDescription(Guid subgroup, Guid setting, uint index)
        {
            uint size = 0;
            PowerReadPossibleDescription(IntPtr.Zero, ref subgroup, ref setting, index, null, ref size);
            if (size == 0) return string.Empty;

            byte[] buffer = new byte[size];
            uint res = PowerReadPossibleDescription(IntPtr.Zero, ref subgroup, ref setting, index, buffer, ref size);
            return res == 0 ? Encoding.Unicode.GetString(buffer).TrimEnd('\0') : string.Empty;
        }

        internal static uint ReadAcValueIndex(Guid scheme, Guid subgroup, Guid setting)
        {
            IntPtr schemePtr = AllocGuid(scheme);
            IntPtr subgroupPtr = AllocGuid(subgroup);
            IntPtr settingPtr = AllocGuid(setting);

            try
            {
                return PowerReadACValueIndex(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, out var value) == 0 ? value : 0;
            }
            finally
            {
                Marshal.FreeHGlobal(schemePtr);
                Marshal.FreeHGlobal(subgroupPtr);
                Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static uint ReadDcValueIndex(Guid scheme, Guid subgroup, Guid setting)
        {
            IntPtr schemePtr = AllocGuid(scheme);
            IntPtr subgroupPtr = AllocGuid(subgroup);
            IntPtr settingPtr = AllocGuid(setting);

            try
            {
                return PowerReadDCValueIndex(IntPtr.Zero, schemePtr, subgroupPtr, settingPtr, out var value) == 0 ? value : 0;
            }
            finally
            {
                Marshal.FreeHGlobal(schemePtr);
                Marshal.FreeHGlobal(subgroupPtr);
                Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static uint ReadValueMin(Guid subgroup, Guid setting)
        {
            IntPtr subgroupPtr = AllocGuid(subgroup);
            IntPtr settingPtr = AllocGuid(setting);

            try
            {
                return PowerReadValueMin(IntPtr.Zero, subgroupPtr, settingPtr, out var value) == 0 ? value : 0;
            }
            finally
            {
                Marshal.FreeHGlobal(subgroupPtr);
                Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static uint ReadValueMax(Guid subgroup, Guid setting)
        {
            IntPtr subgroupPtr = AllocGuid(subgroup);
            IntPtr settingPtr = AllocGuid(setting);

            try
            {
                return PowerReadValueMax(IntPtr.Zero, subgroupPtr, settingPtr, out var value) == 0 ? value : 0;
            }
            finally
            {
                Marshal.FreeHGlobal(subgroupPtr);
                Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static uint ReadValueIncrement(Guid subgroup, Guid setting)
        {
            IntPtr subgroupPtr = AllocGuid(subgroup);
            IntPtr settingPtr = AllocGuid(setting);

            try
            {
                return PowerReadValueIncrement(IntPtr.Zero, subgroupPtr, settingPtr, out var value) == 0 ? value : 0;
            }
            finally
            {
                Marshal.FreeHGlobal(subgroupPtr);
                Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static string ReadValueUnitsSpecifier(Guid subgroup, Guid setting)
        {
            IntPtr subgroupPtr = AllocGuid(subgroup);
            IntPtr settingPtr = AllocGuid(setting);

            try
            {
                uint size = 0;
                PowerReadValueUnitsSpecifier(IntPtr.Zero, subgroupPtr, settingPtr, null, ref size);
                if (size < 2) return string.Empty;

                byte[] buffer = new byte[size];
                uint res = PowerReadValueUnitsSpecifier(IntPtr.Zero, subgroupPtr, settingPtr, buffer, ref size);
                return res == 0 ? Encoding.Unicode.GetString(buffer, 0, (int)size - 2) : string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(subgroupPtr);
                Marshal.FreeHGlobal(settingPtr);
            }
        }

        internal static bool WriteSchemeFriendlyName(Guid scheme, string name)
        {
            IntPtr schemePtr = AllocGuid(scheme);

            try
            {
                uint size = (uint)((name?.Length ?? 0) + 1) * 2;
                return PowerWriteFriendlyName(IntPtr.Zero, schemePtr, IntPtr.Zero, IntPtr.Zero, name ?? string.Empty, size) == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(schemePtr);
            }
        }

        internal static bool WriteSchemeDescription(Guid scheme, string description)
        {
            IntPtr schemePtr = AllocGuid(scheme);

            try
            {
                uint size = (uint)((description?.Length ?? 0) + 1) * 2;
                return PowerWriteDescription(IntPtr.Zero, schemePtr, IntPtr.Zero, IntPtr.Zero, description ?? string.Empty, size) == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(schemePtr);
            }
        }

        internal static bool DeleteScheme(Guid scheme)
        {
            IntPtr schemePtr = AllocGuid(scheme);

            try
            {
                return PowerDeleteScheme(IntPtr.Zero, schemePtr) == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(schemePtr);
            }
        }
    }
}
