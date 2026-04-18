using AutoOS.Helpers.Device;
using AutoOS.Helpers.Registry;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using AutoOS.Helpers.Services;
using System.Text.Json.Nodes;
using Windows.Storage;
using AutoOS.Views.Installer.Actions;
using Windows.Win32.System.Services;
using AutoOS.Helpers.Sound;
using Windows.Win32;
using Windows.Win32.Media.Audio;
using Windows.Win32.Foundation;

namespace AutoOS.Views.Startup.Stages;

public static class StartupStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
    public static async Task Run()
    {
        if (localSettings.Values["XHCIs"] == null)
        {
            var json = new JsonArray();
            foreach (var device in DeviceHelper.GetDevices(DeviceType.XHCI))
                json.Add((JsonNode)new JsonObject { ["PnpDeviceId"] = JsonValue.Create(device.PnpDeviceId), ["IsActive"] = JsonValue.Create(false) });
            localSettings.Values["XHCIs"] = json.ToJsonString();
        }

        bool MSI = Directory.Exists(@"C:\Program Files (x86)\MSI Afterburner\Profiles\") && Directory.GetFiles(@"C:\Program Files (x86)\MSI Afterburner\Profiles\").Any(f => !f.EndsWith("MSIAfterburner.cfg", StringComparison.OrdinalIgnoreCase));
        bool SOUND = JsonNode.Parse(localSettings.Values["Sound"]?.ToString() ?? "[]")?.AsArray()?.Any(x => x?["BufferSize"]?.GetValue<float>() < 10f) == true;
        bool IMOD = JsonNode.Parse(localSettings.Values["XHCIs"]?.ToString() ?? "[]")?.AsArray()?.Any(x => x?["IsActive"]?.GetValue<bool>() == false) == true;
        bool OBS = localSettings.Values["OBS"]?.ToString() == "1" && File.Exists(@"C:\Program Files\obs-studio\bin\64bit\obs64.exe");

        string previousTitle = string.Empty;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // sync time
            ("Syncing time", async () => ServicesHelper.SetStartupType("W32Time", SERVICE_START_TYPE.SERVICE_DEMAND_START), null),
            ("Syncing time", async () => ServicesHelper.StartService("W32Time"), null),
            ("Syncing time", async () => await Process.Start(new ProcessStartInfo("w32tm", "/resync") { CreateNoWindow = true })!.WaitForExitAsync(), null),
            ("Syncing time", async () => ServicesHelper.StopService("W32Time"), null),

            // apply msi afterburner profile
            ("Applying MSI Afterburner profile", async () => await Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files (x86)\MSI Afterburner\MSIAfterburner.exe", Arguments = "/Profile1 /q" })!.WaitForExitAsync(), () => MSI == true),

            // apply sound buffer sizes
            ("Applying sound buffer sizes", async () => SoundHelper.SetBufferSizes(), () => SOUND == true),

            // disable xhci interrupt moderation (imod)
            ("Disabling XHCI Interrupt Moderation (IMOD)", async () => { foreach (var device in DeviceHelper.GetDevices(DeviceType.XHCI)) if (JsonNode.Parse(localSettings.Values["XHCIs"]?.ToString() ?? "[]")?.AsArray()?.FirstOrDefault(x => x?["PnpDeviceId"]?.ToString() == device.PnpDeviceId)?["IsActive"]?.GetValue<bool>() == false) DeviceHelper.ToggleImod(device, false); }, () => IMOD),

            // launch obs studio
            ("Launching OBS Studio", async () => ProcessActions.CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio", ".sentinel")), () => OBS == true),
            ("Launching OBS Studio", async () => Process.Start(new ProcessStartInfo { FileName = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe", Arguments = "--disable-updater --startreplaybuffer --minimize-to-tray", WorkingDirectory = @"C:\Program Files\obs-studio\bin\64bit" }), () => OBS == true),

            // clean temp directories
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\Logs"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\Panther"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\SoftwareDistribution"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\LogFiles"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\SleepStudy"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\sru"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\WDI"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\System32\winevt\Logs"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\SystemTemp"); }), null),
            ("Cleaning temp directories", async () => await RegistryHelper.RunAs(RegistryHelper.Identity.TrustedInstaller, async () => { ProcessActions.CleanDirectory(@"C:\Windows\Temp"); }), null),
            ("Cleaning temp directories", async () => ProcessActions.CleanDirectory(Path.GetTempPath()), null)
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
                        try
                        {
                            await ProcessActions.LogError(ex);
                        }
                        catch { }
                        StartupWindow.Status.Text = ex.Message;
                        StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    }
                }

                StartupWindow.Progress.Value += incrementPerTitle;
                await Task.Delay(100);
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
                    try
                    {
                        await ProcessActions.LogError(ex);
                    }
                    catch { }
                    StartupWindow.Status.Text = ex.Message;
                    StartupWindow.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                }
            }
            StartupWindow.Progress.Value += incrementPerTitle;
        }

        StartupWindow.Status.Text = "Done.";
        StartupWindow.Progress.Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemFillColorSuccess"]);
        await Task.Delay(500);
        Application.Current.Exit();
    }
}