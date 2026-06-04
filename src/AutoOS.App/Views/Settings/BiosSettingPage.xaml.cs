using AutoOS.Core.Helpers.Logging;
using AutoOS.Core.Helpers.Picker;
using AutoOS.Views.Installer.Actions;
using AutoOS.Views.Installer.Stages;
using AutoOS.Views.Settings.BIOS;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AutoOS.Views.Settings;

public sealed partial class BiosSettingPage : Page, INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private bool _isAnyModified;
	public bool IsAnyModified
	{
		get => _isAnyModified;
		set
		{
			if (_isAnyModified != value)
			{
				_isAnyModified = value;
				OnPropertyChanged();
			}
		}
	}

	private bool _hasRecommendations;
	public bool HasRecommendations
	{
		get => _hasRecommendations;
		set
		{
			if (_hasRecommendations != value)
			{
				_hasRecommendations = value;
				OnPropertyChanged();
			}
		}
	}

	private readonly string nvram = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt");
	private readonly ObservableCollection<BiosSettingModel> biosSettings = [];
	private readonly ObservableCollection<BiosSettingModel> recommendedSettings = [];
	private readonly List<BiosSettingModel> allSettings = [];

	public BiosSettingPage()
	{
		InitializeComponent();

		RecommendedChangesListView.ItemsSource = recommendedSettings;
		SettingsListView.ItemsSource = biosSettings;

		// copy scewin to localstate because of permissions
		string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN");
		string destinationPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN");

		if (!Directory.Exists(destinationPath))
		{
			Directory.CreateDirectory(destinationPath);

			foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
				Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));

			foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
				File.Copy(file, file.Replace(sourcePath, destinationPath), overwrite: true);
		}

		_ = LoadAsync();
	}

	private async Task LoadAsync()
	{
		// show exporting
		SwitchPresenter.Value = "Export";

		// export nvram
		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "SCEWIN_64.exe"),
				Arguments = @$"/o /s ""{nvram}""",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			}
		};

		process.Start();
		string errorOutput = await process.StandardError.ReadToEndAsync();
		string output = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();

		string manufacturer = "Unknown";
		string product = "Unknown";

		using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
		{
			if (key != null)
			{
				manufacturer = key.GetValue("BaseBoardManufacturer")?.ToString().ToLowerInvariant() ?? "unknown";
				product = key.GetValue("BaseBoardProduct")?.ToString().ToUpperInvariant() ?? "unknown";
			}
			else
			{
				manufacturer = "unknown";
				product = "unknown";
			}
		}

		if (output.Contains("AMISCE is not supported on this system.", StringComparison.OrdinalIgnoreCase) || errorOutput.Contains("BIOS not compatible", StringComparison.OrdinalIgnoreCase))
		{
			SwitchPresenter.Value = "Unsupported";
		}
		else if (errorOutput.Contains("WARNING: HII data does not have setup questions information", StringComparison.OrdinalIgnoreCase))
		{
			if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
			{
				var protectedChipsets = new[] { "Z790", "B760", "H770", "X870", "X670", "B650", "A620" };

				if (protectedChipsets.Any(c => product.Contains(c)))
				{
					SwitchPresenter.Value = "HII Resources (Protected)";
				}
				else
				{
					SwitchPresenter.Value = "HII Resources (Regular)";
				}
			}
			else
			{
				SwitchPresenter.Value = "HII Resources (Other)";
			}
		}
		else if (errorOutput.Contains("Platform identification failed.", StringComparison.OrdinalIgnoreCase))
		{
			// export nvram
			using var process2 = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "SCEWIN_64.exe"),
					Arguments = @$"/o /s ""{nvram}"" /d",
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true
				}
			};

			process2.Start();
			errorOutput = await process2.StandardError.ReadToEndAsync();
			output = await process2.StandardOutput.ReadToEndAsync();
			await process2.WaitForExitAsync();
		}

		if (errorOutput.Contains("Script file exported successfully.", StringComparison.OrdinalIgnoreCase))
		{
			if (new FileInfo(nvram).Length > 100 * 1024)
			{
				// reset ui
				RecommendedChanges.Visibility = Visibility.Visible;
				RecommendedChanges.IsExpanded = true;
				AllSettings.IsExpanded = false;
				AllSettingsGrid.MaxHeight = 495;

				// backup nvram.txt
				string backupRoot = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "Backup");

				if (!Directory.Exists(backupRoot))
				{
					try
					{
						_ = LogHelper.Log(PreparingStage.GPUs, true);
					}
					catch
					{ }
				}

				string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
				string backupDir = Path.Combine(backupRoot, timestamp);

				Directory.CreateDirectory(backupDir);
				File.Copy(nvram, Path.Combine(backupDir, "nvram.txt"), false);

				var backupFolders = Directory.GetDirectories(backupRoot)
					.OrderByDescending(dir => dir)
					.Skip(50)
					.ToList();

				foreach (var folder in backupFolders)
					Directory.Delete(folder, true);

				// parse nvram.txt
				List<BiosSettingModel> parsedList;

				using var stream = File.OpenRead(nvram);
				parsedList = await Task.Run(() =>
				{
					var settings = BiosSettingParser.ParseFromStream(stream).ToList();

					foreach (var setting in settings)
					{
						foreach (var option in setting.Options)
							option.Parent = setting;

						setting.InitializeSelectedOption();

						if (setting.HasValueField)
							setting.OriginalValue = setting.Value;

						if (setting.HasOptions)
							setting.OriginalSelectedOption = setting.SelectedOption;

						var matchingRules = BiosSettingRecommendationsList.Rules
							.Where(r => string.Equals(r.SetupQuestion?.Trim(), setting.SetupQuestion?.Trim(), StringComparison.OrdinalIgnoreCase))
							.Where(r => r.Condition == null || r.Condition(settings))
							.OrderByDescending(r => r.Condition != null)
							.ToList();

						foreach (var rule in matchingRules)
						{
							string recommendedLabel = rule.RecommendedOption?.Trim().ToLowerInvariant();
							bool ruleApplicable = false;

							if ((rule.Type?.Equals("Option", StringComparison.OrdinalIgnoreCase) ?? false) && setting.HasOptions)
							{
								string selectedLabel = setting.SelectedOption?.Label?.Trim().ToLowerInvariant();

								var recommended = setting.Options
									.FirstOrDefault(o => o.Label?.Trim().ToLowerInvariant() == recommendedLabel);

								if (recommended != null)
								{
									ruleApplicable = true;
									if (selectedLabel != recommended.Label?.ToLowerInvariant())
									{
										setting.RecommendedOption = recommended;
										setting.IsRecommended = true;
									}
								}
							}

							if ((rule.Type?.Equals("Value", StringComparison.OrdinalIgnoreCase) ?? false) && setting.HasValueField)
							{
								string currentValue = setting.Value?.Trim().ToLowerInvariant();
								ruleApplicable = true;

								if (!string.IsNullOrEmpty(currentValue) && currentValue != recommendedLabel)
								{
									setting.IsRecommended = true;
									setting.RecommendedValue = rule.RecommendedOption;
								}
							}

							if (ruleApplicable)
							{
								break;
							}
						}

						setting.MarkLoaded();
					}

					return settings;
				});

				var ruleOrder = BiosSettingRecommendationsList.Rules
					.Select((r, i) => new { r.SetupQuestion, r.RecommendedOption, Index = i })
					.GroupBy(x => (x.SetupQuestion?.ToLowerInvariant(), x.RecommendedOption?.ToLowerInvariant()))
					.ToDictionary(
						g => g.Key,
						g => g.First().Index
					);

				var sortedRecommended = parsedList
					.Where(s => s.IsRecommended)
					.OrderBy(s =>
						ruleOrder.TryGetValue(
							(s.SetupQuestion.ToLowerInvariant(),
							 (s.RecommendedOption?.Label ?? s.RecommendedValue ?? string.Empty).ToLowerInvariant()),
							out var index) ? index : int.MaxValue)
					.ThenBy(s => s.SetupQuestion, StringComparer.OrdinalIgnoreCase)
					.ToList();

				biosSettings.Clear();
				recommendedSettings.Clear();
				allSettings.Clear();

				foreach (var setting in parsedList)
				{
					biosSettings.Add(setting);
					allSettings.Add(setting);

					setting.ModifiedChanged += (s, e) =>
					{
						IsAnyModified = allSettings.Any(x => x.IsModified);
					};
				}

				foreach (var setting in sortedRecommended)
				{
					recommendedSettings.Add(setting);
				}

				HasRecommendations = false;
				HasRecommendations = parsedList.Any(s => s.IsRecommended);

				// show settings
				SwitchPresenter.Value = "Loaded";
				Search.IsEnabled = true;
				Backup.IsEnabled = true;
			}
			else
			{
				if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
				{
					var protectedChipsets = new[] { "Z790", "B760", "H770", "X870", "X670", "B650", "A620" };

					if (protectedChipsets.Any(c => product.Contains(c)))
					{
						SwitchPresenter.Value = "HII Resources (Protected)";
					}
					else
					{
						SwitchPresenter.Value = "HII Resources (Regular)";
					}
				}
				else
				{
					SwitchPresenter.Value = "HII Resources (Other)";
				}
			}
		}
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (sender is DevWinUI.TextBox tb)
		{
			string query = tb.Text.Trim().ToLower();

			biosSettings.Clear();
			RecommendedChanges.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;
			AllSettings.IsExpanded = !string.IsNullOrEmpty(query);
			AllSettingsGrid.MaxHeight = string.IsNullOrEmpty(query) ? 495 : 555;

			foreach (var setting in allSettings)
			{
				if (string.IsNullOrEmpty(query) || setting.SetupQuestion?.ToLower().Contains(query, StringComparison.CurrentCultureIgnoreCase) == true)
				{
					biosSettings.Add(setting);
				}
			}
		}
	}

	private void MergeNext_Click(object sender, RoutedEventArgs e)
	{
		BiosSettingUpdater.IsBatchUpdating = true;

		var toMerge = recommendedSettings.Take((int)MergeCountBox.Value).ToList();

		foreach (var setting in toMerge)
		{
			setting.OriginalValue ??= setting.Value;
			setting.OriginalSelectedOption ??= setting.SelectedOption;

			if (setting.RecommendedOption != null)
			{
				foreach (var option in setting.Options)
					option.IsSelected = option == setting.RecommendedOption;
			}
			else if (!string.IsNullOrEmpty(setting.RecommendedValue))
			{
				setting.Value = setting.RecommendedValue;
			}
		}

		BiosSettingUpdater.IsBatchUpdating = false;

		var modifiedSettings = toMerge
			.Where(s =>
				(s.OriginalValue != null && s.Value != s.OriginalValue) ||
				(s.SelectedOption != null && s.SelectedOption != s.OriginalSelectedOption) ||
				(s.SelectedOption == null && s.OriginalSelectedOption != null)
			)
			.ToList();

		if (modifiedSettings.Count > 0)
		{
			BiosSettingUpdater.SaveAllSettings(modifiedSettings);
		}
	}

	private void MergeAll_Click(object sender, RoutedEventArgs e)
	{
		BiosSettingUpdater.IsBatchUpdating = true;

		foreach (var setting in recommendedSettings)
		{
			setting.OriginalValue ??= setting.Value;
			setting.OriginalSelectedOption ??= setting.SelectedOption;

			if (setting.RecommendedOption != null)
			{
				foreach (var option in setting.Options)
					option.IsSelected = option == setting.RecommendedOption;
			}
			else if (!string.IsNullOrEmpty(setting.RecommendedValue))
			{
				setting.Value = setting.RecommendedValue;
			}
		}

		BiosSettingUpdater.IsBatchUpdating = false;

		var modifiedSettings = recommendedSettings
			.Where(s =>
				(s.OriginalValue != null && s.Value != s.OriginalValue) ||
				(s.SelectedOption != null && s.SelectedOption != s.OriginalSelectedOption) ||
				(s.SelectedOption == null && s.OriginalSelectedOption != null)
			)
			.ToList();

		if (modifiedSettings.Count > 0)
		{
			BiosSettingUpdater.SaveAllSettings(modifiedSettings);
		}
	}

	private async void Backup_Click(object sender, RoutedEventArgs e)
	{
		// disable the button to avoid double-clicking
		var senderButton = sender as Button;
		senderButton.IsEnabled = false;

		// launch file picker
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = false,
			InitialDirectory = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "Backup")
		};
		picker.FileTypeChoices.Add("NVRAM", ["*.txt"]);
		var file = await picker.PickSingleFileAsync();

		if (file != null)
		{
			if (file.Name == "nvram.txt")
			{
				// show importing
				SwitchPresenter.Value = "Import";
				Search.Text = string.Empty;

				// import nvram
				using var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN", "SCEWIN_64.exe"),
						Arguments = @$"/i /s ""{file.Path}""",
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardError = true,
						RedirectStandardOutput = true
					}
				};

				process.Start();
				string errorOutput = await process.StandardError.ReadToEndAsync();
				string output = await process.StandardOutput.ReadToEndAsync();
				await process.WaitForExitAsync();

				string manufacturer = "Unknown";
				string product = "Unknown";

				using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
				{
					if (key != null)
					{
						manufacturer = key.GetValue("BaseBoardManufacturer")?.ToString().ToLowerInvariant() ?? "unknown";
						product = key.GetValue("BaseBoardProduct")?.ToString().ToUpperInvariant() ?? "unknown";
					}
					else
					{
						manufacturer = "unknown";
						product = "unknown";
					}
				}

				if (errorOutput.Contains("Warning: Error in writing variable", StringComparison.OrdinalIgnoreCase))
				{
					if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
					{
						SwitchPresenter.Value = "Write Protected (ASUS)";
					}
					else if (manufacturer.Contains("asrock"))
					{
						SwitchPresenter.Value = "Write Protected (ASRock)";
					}
					else
					{
						SwitchPresenter.Value = "Write Protected (Other)";
					}
				}
				else if (errorOutput.Contains("Script file imported successfully.", StringComparison.OrdinalIgnoreCase))
				{
					await LoadAsync();
				}
				else if (errorOutput.Contains("System configuration not modified.", StringComparison.OrdinalIgnoreCase))
				{
					await LoadAsync();
				}
			}
			else
			{
				var dialog = new ContentDialog
				{
					Title = "Invalid File",
					Content = "Please select a valid nvram.txt file.",
					DefaultButton = ContentDialogButton.Close,
					CloseButtonText = "OK",
					XamlRoot = App.MainWindow.Content.XamlRoot
				};
				await dialog.ShowAsync();

				senderButton.IsEnabled = true;
			}
		}
		else
		{
			senderButton.IsEnabled = true;
		}
	}

	private async void Import_Click(object sender, RoutedEventArgs e)
	{
		// show importing
		SwitchPresenter.Value = "Import";
		Search.Text = string.Empty;
		IsAnyModified = false;

		// import nvram
		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN", "SCEWIN_64.exe"),
				Arguments = @$"/i /s ""{nvram}""",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			}
		};

		process.Start();
		string errorOutput = await process.StandardError.ReadToEndAsync();
		string output = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();

		string manufacturer = "Unknown";
		string product = "Unknown";

		using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
		{
			if (key != null)
			{
				manufacturer = key.GetValue("BaseBoardManufacturer")?.ToString().ToLowerInvariant() ?? "unknown";
				product = key.GetValue("BaseBoardProduct")?.ToString().ToUpperInvariant() ?? "unknown";
			}
			else
			{
				manufacturer = "unknown";
				product = "unknown";
			}
		}

		if ((errorOutput.Contains("WARNING : Cannot update protected variable", StringComparison.OrdinalIgnoreCase) ||
			 errorOutput.Contains("WARNING : Error in writing variable", StringComparison.OrdinalIgnoreCase)) &&
			!errorOutput.Contains("Script file imported successfully.", StringComparison.OrdinalIgnoreCase))
		{
			if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
			{
				SwitchPresenter.Value = "Write Protected (ASUS)";
			}
			else if (manufacturer.Contains("asrock"))
			{
				SwitchPresenter.Value = "Write Protected (ASRock)";
			}
			else
			{
				SwitchPresenter.Value = "Write Protected (Other)";
			}
		}
		else
		{
			await LoadAsync();
		}
	}
}
