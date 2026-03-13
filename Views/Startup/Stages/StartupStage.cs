using AutoOS.Views.Startup.Actions;
using AutoOS.Helpers.Device;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Windows.Storage;

namespace AutoOS.Views.Startup.Stages;

public static class StartupStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static async Task Run()
    {
        bool MSI = Directory.Exists(@"C:\Program Files (x86)\MSI Afterburner\Profiles\") && Directory.GetFiles(@"C:\Program Files (x86)\MSI Afterburner\Profiles\").Any(f => !f.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase));
        bool OBS = localSettings.Values["OBS"]?.ToString() == "1";
        if (localSettings.Values["XHCIs"] == null)
        {
            var json = new JsonArray();
            foreach (var device in DeviceHelper.GetDevices(DeviceType.XHCI))
                json.Add((JsonNode)new JsonObject { ["PnpDeviceId"] = JsonValue.Create(device.PnpDeviceId), ["IsActive"] = JsonValue.Create(false) });
            localSettings.Values["XHCIs"] = json.ToJsonString();
        }

        bool IMOD = JsonNode.Parse(localSettings.Values["XHCIs"]?.ToString() ?? "[]")?.AsArray()?.Any(x => x?["IsActive"]?.GetValue<bool>() == false) == true;
        bool Discord = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord"));

        string discordVersion = "";

        string previousTitle = string.Empty;

        string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "LowAudioLatency");
        string destinationPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "LowAudioLatency");

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);

            foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));

            foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(sourcePath, destinationPath), overwrite: true);
        }

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // sync time
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "net start w32time"), null),
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "w32tm /resync"), null),
            ("Syncing time", async () => await StartupActions.RunNsudo("CurrentUser", "net stop w32time"), null),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" })), () => MSI == true),

            // disable xhci interrupt moderation (imod)
            ("Disabling XHCI Interrupt Moderation (IMOD)", async () => await Task.Run(() => { foreach (var device in DeviceHelper.GetDevices(DeviceType.XHCI)) if (JsonNode.Parse(localSettings.Values["XHCIs"]?.ToString() ?? "[]")?.AsArray()?.FirstOrDefault(x => x?["PnpDeviceId"]?.ToString() == device.PnpDeviceId)?["IsActive"]?.GetValue<bool>() == false) DeviceHelper.ToggleImod(device, false); }), () => IMOD),

            // disable device power management
            ("Disabling device power management", async () => await StartupActions.RunPowerShellScript("devicepowermanagement.ps1", ""), null),

            // launch lowaudiolatency
            ("Launching LowAudioLatency", async () => await StartupActions.RunApplication("LocalState", "LowAudioLatency", "low_audio_latency_no_console.exe", ""), null),

            // launch obs studio
            ("Launching OBS Studio", async () => await StartupActions.RunNsudo("CurrentUser", @"cmd /c del ""%APPDATA%\obs-studio\.sentinel"" /s /f /q"), () => OBS == true),
            ("Launching OBS Studio", async () => await Task.Run(() => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe", Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = @"C:\Program Files\obs-studio\bin\64bit" })), () => OBS == true),

            // debloat discord
            ("Debloating Discord", async () => await Task.Run(() => { discordVersion = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord")).GetDirectories().FirstOrDefault(d => d.Name.StartsWith("app-"))?.Name[4..]; }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_cloudsync-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_dispatch-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_erlpack-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_game_utils-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_hook-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_overlay2-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_rpc-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_spellcheck-1"), true); } catch { } }), () => Discord == true),
            ("Debloating Discord", async () => await Task.Run(() => { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "app-" + discordVersion, "modules", "discord_zstd-1"), true); } catch { } }), () => Discord == true),

            // clean temp directories
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Logs"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SoftwareDistribution"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\LogFiles\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\SleepStudy\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\sru"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\WDI\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\System32\winevt\Logs\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\SystemTemp\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("TrustedInstaller", @"cmd /c del /s /f /q ""C:\Windows\Temp\*.*"""), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"cmd /c del /s /f /q %temp%\*.*"), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"cmd /c rd /s /q %temp%"), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"cmd /c md %temp%"), null),
            ("Cleaning temp directories", async () => await StartupActions.RunNsudo("CurrentUser", @"sc stop TrustedInstaller"), null),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        int groupedTitleCount = 0;

        List<Func<Task>> currentGroup = [];

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? 100 / (double)groupedTitleCount : 0;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        StartupWindow.Status.Text = ex.Message;
                        StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    }
                }

                StartupWindow.Progress.Value += incrementPerTitle;
                await Task.Delay(150);
                currentGroup.Clear();
            }

            StartupWindow.Status.Text = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    StartupWindow.Status.Text = ex.Message;
                    StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                }
            }
            StartupWindow.Progress.Value += incrementPerTitle;
        }

        StartupWindow.Status.Text = "Done.";
        StartupWindow.Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);

        await Task.Delay(700);

        //Application.Current.Exit();
    }
}