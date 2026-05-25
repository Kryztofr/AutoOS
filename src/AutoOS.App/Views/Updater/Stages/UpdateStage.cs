using AutoOS.Core.Helpers.Database;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord"));

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // close discord
			("Closing Discord", async () => { foreach (Process process in Process.GetProcessesByName("Discord")) { if (process.MainWindowHandle != IntPtr.Zero) { PInvoke.PostMessage((HWND)process.MainWindowHandle, PInvoke.WM_CLOSE, default(WPARAM), default(LPARAM)); process.WaitForExit(); } } }, () => Discord == true),
            
			// apply midnight appearance for dark mode
            ("Apply midnight appearance for dark mode", async () => DiscordHelper.SetSystemAppearance(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb")), () => Discord == true)
        };

        return actions;
    }
}
