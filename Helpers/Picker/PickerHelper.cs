using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoOS.Helpers.Picker;

internal static partial class PickerHelper
{
    internal static Windows.Win32.UI.Shell.FILEOPENDIALOGOPTIONS MapPickerOptionsToFOS(PickerOptions options)
    {
        return (Windows.Win32.UI.Shell.FILEOPENDIALOGOPTIONS)options;
    }

    public static string GetKnownFolderPath(Microsoft.Windows.Storage.Pickers.PickerLocationId pickerLocationId)
    {
        Guid folderId;

        switch (pickerLocationId)
        {
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary:
                folderId = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"); // FOLDERID_Documents
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.ComputerFolder:
                folderId = new Guid("0AC0837C-BBF8-452A-850D-79D08E667CA7"); // FOLDERID_ComputerFolder
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.Desktop:
                folderId = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641"); // FOLDERID_Desktop
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.Downloads:
                folderId = new Guid("374DE290-123F-4565-9164-39C4925E467B"); // FOLDERID_Downloads
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.MusicLibrary:
                folderId = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43"); // FOLDERID_MusicLibrary
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.PicturesLibrary:
                folderId = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB"); // FOLDERID_PicturesLibrary
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.VideosLibrary:
                folderId = new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC"); // FOLDERID_VideosLibrary
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.Objects3D:
                folderId = new Guid("31C0DD25-9439-4F12-BF41-7FF4EDA38722"); // FOLDERID_Objects3D
                break;
            case Microsoft.Windows.Storage.Pickers.PickerLocationId.Unspecified:
            default:
                return string.Empty;
        }

        PWSTR pszPath;
        int hr = PInvoke.SHGetKnownFolderPath(in folderId, 0, null, out pszPath);
        if (hr != 0) return string.Empty;

        return pszPath.ToString();
    }
}
[Flags]
public enum PickerOptions : uint
{
    None,
    FOS_OVERWRITEPROMPT = 0x00000002,
    FOS_STRICTFILETYPES = 0x00000004,
    FOS_NOCHANGEDIR = 0x00000008,
    FOS_PICKFOLDERS = 0x00000020,
    FOS_FORCEFILESYSTEM = 0x00000040,
    FOS_ALLNONSTORAGEITEMS = 0x00000080,
    FOS_NOVALIDATE = 0x00000100,
    FOS_ALLOWMULTISELECT = 0x00000200,
    FOS_PATHMUSTEXIST = 0x00000800,
    FOS_FILEMUSTEXIST = 0x00001000,
    FOS_CREATEPROMPT = 0x00002000,
    FOS_SHAREAWARE = 0x00004000,
    FOS_NOREADONLYRETURN = 0x00008000,
    FOS_NOTESTFILECREATE = 0x00010000,
    FOS_HIDEMRUPLACES = 0x00020000,
    FOS_HIDEPINNEDPLACES = 0x00040000,
    FOS_NODEREFERENCELINKS = 0x00100000,
    FOS_OKBUTTONNEEDSINTERACTION = 0x00200000,
    FOS_DONTADDTORECENT = 0x02000000,
    FOS_FORCESHOWHIDDEN = 0x10000000,
    FOS_DEFAULTNOMINIMODE = 0x20000000,
    FOS_FORCEPREVIEWPANEON = 0x40000000,
    FOS_SUPPORTSTREAMABLEITEMS = 0x80000000,
}