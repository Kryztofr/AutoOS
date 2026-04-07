using AutoOS.Helpers.Device;
using System.Net.Http.Headers;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static IntPtr WindowHandle { get; private set; }

    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        Guid guid = Guid.Empty;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {

        };

        //foreach (var adapter in DeviceHelper.GetDevices(DeviceType.NIC).Where(d => d.NicType == NicDeviceType.WiFi || d.NicType == NicDeviceType.LAN).ToList())
        //{
        //    actions.Add(($@"Optimizing advanced network adapter settings for {adapter.FriendlyName}", async () => await Task.Run(() => Helpers.Network.NetworkHelper.OptimizeAdapter(adapter)), null));
        //    actions.Add(($@"Optimizing advanced network adapter settings for {adapter.FriendlyName}", async () => await Task.Delay(500), null));
        //    actions.Add((@"Restarting " + adapter.FriendlyName, async () => await Task.Run(() => DeviceHelper.RestartDevice(adapter)), null));

        //    if (adapter.IsActive)
        //        actions.Add(("Waiting for internet connection to reestablish", async () => await RunConnectionCheck(dialog), null));
        //}

        return actions;
    }

    public static async Task RunConnectionCheck(UpdateDialog dialog)
    {
        dialog.SetCaution();

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