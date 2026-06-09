namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		bool Discord = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "Update.exe"));
		
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
			// update vencord settings
			("Updating Vencord settings", async () => File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Scripts", "settings.json"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord", "settings", "settings.json"), true), () => Discord == true)
		};

		return actions;
	}
}
