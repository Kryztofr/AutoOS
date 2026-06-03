using System.Diagnostics;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// enable ntfs file encryption
			("Enabling NTFS File Encryption", async () => await Process.Start(new ProcessStartInfo { FileName = "fsutil.exe", Arguments = $@"behavior set disableencryption 0", UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync(), null),
		};

		return actions;
	}
}