using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Registry;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
		{
		};

		return actions;
	}
}
