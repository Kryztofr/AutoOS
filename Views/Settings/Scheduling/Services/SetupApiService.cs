using System.Runtime.InteropServices;

namespace AutoOS.Views.Settings.Scheduling.Services;

public enum DIGCF : uint
{
    DIGCF_DEFAULT = 0x00000001,
    DIGCF_PRESENT = 0x00000002,
    DIGCF_ALLCLASSES = 0x00000004,
    DIGCF_PROFILE = 0x00000008,
    DIGCF_DEVICEINTERFACE = 0x00000010
}

public enum SPDRP : uint
{
    SPDRP_DEVICEDESC = 0x00000000,
    SPDRP_HARDWAREID = 0x00000001,
    SPDRP_COMPATIBLEIDS = 0x00000002,
    SPDRP_SERVICE = 0x00000004,
    SPDRP_CLASS = 0x00000007,
    SPDRP_CLASSGUID = 0x00000008,
    SPDRP_DRIVER = 0x00000009,
    SPDRP_CONFIGFLAGS = 0x0000000A,
    SPDRP_MFG = 0x0000000B,
    SPDRP_FRIENDLYNAME = 0x0000000C,
    SPDRP_LOCATION_INFORMATION = 0x0000000D,
    SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E,
    SPDRP_CAPABILITIES = 0x0000000F,
    SPDRP_UI_NUMBER = 0x00000010,
    SPDRP_UPPERFILTERS = 0x00000011,
    SPDRP_LOWERFILTERS = 0x00000012,
    SPDRP_BUSTYPEGUID = 0x00000013,
    SPDRP_LEGACYBUSTYPE = 0x00000014,
    SPDRP_BUSNUMBER = 0x00000015,
    SPDRP_ENUMERATOR_NAME = 0x00000016,
    SPDRP_SECURITY = 0x00000017,
    SPDRP_SECURITY_SDS = 0x00000018,
    SPDRP_DEVTYPE = 0x00000019,
    SPDRP_EXCLUSIVE = 0x0000001A,
    SPDRP_CHARACTERISTICS = 0x0000001B,
    SPDRP_ADDRESS = 0x0000001C,
    SPDRP_UI_NUMBER_DESC_FORMAT = 0x0000001D,
    SPDRP_DEVICE_POWER_DATA = 0x0000001E,
    SPDRP_REMOVAL_POLICY = 0x0000001F,
    SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020,
    SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021,
    SPDRP_INSTALL_STATE = 0x00000022,
    SPDRP_LOCATION_PATHS = 0x00000023,
    SPDRP_BASE_CONTAINERID = 0x00000024
}

public enum DI_FUNCTION : uint
{
    DIF_PROPERTYCHANGE = 0x00000012
}

public enum DICS_STATE : uint
{
    DICS_ENABLE = 0x00000001,
    DICS_DISABLE = 0x00000002,
    DICS_PROPCHANGE = 0x00000003,
    DICS_START = 0x00000004,
    DICS_STOP = 0x00000005
}

public enum DICS_FLAG : uint
{
    DICS_FLAG_GLOBAL = 0x00000001,
    DICS_FLAG_CONFIGSPECIFIC = 0x00000002,
    DICS_FLAG_CONFIGGENERAL = 0x00000004
}

public enum DIREG : uint
{
    DIREG_DEV = 0x00000001,
    DIREG_DRV = 0x00000002,
    DIREG_BOTH = 0x00000004
}

[StructLayout(LayoutKind.Sequential)]
public struct SP_CLASSINSTALL_HEADER
{
    public uint cbSize;
    public DI_FUNCTION InstallFunction;
}

[StructLayout(LayoutKind.Sequential)]
public struct SP_PROPCHANGE_PARAMS
{
    public SP_CLASSINSTALL_HEADER ClassInstallHeader;
    public DICS_STATE StateChange;
    public DICS_FLAG Scope;
    public uint HwProfile;
}

[StructLayout(LayoutKind.Sequential)]
public struct SP_DEVINSTALL_PARAMS
{
    public uint cbSize;
    public uint Flags;
    public uint FlagsEx;
    public IntPtr hwndParent;
    public IntPtr InstallMsgHandler;
    public IntPtr InstallMsgHandlerContext;
    public IntPtr FileQueue;
    public IntPtr ClassInstallReserved;
    public uint Reserved;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string DriverPath;
}

public static class SetupApi
{
    public const uint DI_NEEDREBOOT = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    public static DEVPROPKEY DEVPKEY_PciDevice_InterruptMessageMaximum = new DEVPROPKEY
    {
        fmtid = new Guid(0x3ab22e31, 0x8264, 0x4b4e, 0x9a, 0xf5, 0xa8, 0xd2, 0xd8, 0xe3, 0x3e, 0x62),
        pid = 15
    };

    public static DEVPROPKEY DEVPKEY_Device_DriverVersion = new()
    {
        fmtid = new Guid("A8B865DD-2E3D-4094-AD97-E593A70C75D6"),
        pid = 3
    };
}

