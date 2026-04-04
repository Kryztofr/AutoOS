using AutoOS.Helpers.Device;
using AutoOS.Views.Settings.Power;
using System.Net.Http.Headers;
using WinRT.Interop;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static IntPtr WindowHandle { get; private set; }

    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        Guid guid = Guid.Empty;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // update power plan
            ("Selecting AutoOS Power Plan", async () => guid = PowerApi.GetPlanGuidByName("AutoOS"), null),
            (@"Setting ""Heterogeneous short running thread scheduling policy"" to ""All processors""", async () => PowerApi.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("bae08b81-2d5e-4688-ad6a-13243356654b"), 0), null),
            (@"Setting ""Heterogeneous short running thread scheduling policy"" to ""All processors""", async () => PowerApi.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("bae08b81-2d5e-4688-ad6a-13243356654b"), 0), null),
            ("Applying Changes", async () =>  PowerApi.PowerSetActiveScheme(guid), null)
        };

        foreach (var adapter in DeviceHelper.GetDevices(DeviceType.NIC).Where(d => d.NicType == NicDeviceType.WiFi || d.NicType == NicDeviceType.LAN).ToList())
        {
            actions.Add(($@"Optimizing advanced network adapter settings for {adapter.FriendlyName}", async () => await Task.Run(() => Helpers.Network.NetworkHelper.OptimizeAdapter(adapter)), null));
            actions.Add(($@"Optimizing advanced network adapter settings for {adapter.FriendlyName}", async () => await Task.Delay(500), null));
            actions.Add((@"Restarting " + adapter.FriendlyName, async () => await Task.Run(() => DeviceHelper.RestartDevice(adapter)), null));

            if (adapter.IsActive)
                actions.Add(("Waiting for internet connection to reestablish", async () => await RunConnectionCheck(dialog), null));
        }

        return actions;
    }

    public static async Task RunConnectionCheck(UpdateDialog dialog)
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        dialog.SetCaution();
        Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Paused);

        await Task.Delay(1000);

        while (true)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("AutoOS"));
                var response = await client.GetAsync("http://www.google.com");
                if (response.IsSuccessStatusCode)
                {
                    dialog.ResetProgressColor();
                    Helpers.Taskbar.TaskbarHelper.SetProgressState(WindowHandle, Helpers.Taskbar.TaskbarStates.Normal);
                    dialog.SetStatus("Internet connection successfully established...");
                    await Task.Delay(500);
                    break;
                }
            }
            catch
            {

            }
            await Task.Delay(1000);
        }
    }
}