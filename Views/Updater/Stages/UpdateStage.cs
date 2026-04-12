using AutoOS.Helpers.CPU;
using AutoOS.Helpers.Device;
using AutoOS.Helpers.Scheduling;
using AutoOS.Helpers.Sound;
using Windows.Win32.Media.Audio;
using System.Diagnostics;
using AutoOS.Helpers.Services;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Storage;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        var cpuSetsInfo = CpuHelper.GetCpuSets();
        var (pCores, _) = CpuHelper.GroupCpuSetsByEfficiencyClass(cpuSetsInfo);
        var PCores = pCores.Count;

        string defaultEndpoint = SoundHelper.GetDefaultAudioEndpointId(EDataFlow.eRender);
        string hdaud = defaultEndpoint != null ? DeviceHelper.GetParentPnpId(defaultEndpoint) : null;
        string controller = hdaud != null ? DeviceHelper.GetParentPnpId(hdaud) : null;
        var audioControllers = DeviceHelper.GetDevices(DeviceType.AudioController);
        var audioController = audioControllers.FirstOrDefault(device => device.PnpDeviceId == controller) ?? audioControllers.FirstOrDefault();
        bool AUDIO_AFFINITY = audioController != null && audioController.DevicePolicy == 4 && audioController.AssignmentSetOverride != 0;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // optimize affinities
            ("Optimizing Affinities", async () => await Task.Delay(1000), () => PCores >= 4),
            ("Optimizing Affinities", async () => await SchedulingHelper.OptimizeAffinities(), () => PCores >= 4),
            ("Optimizing Affinities", async () => await Task.Delay(2000), () => PCores >= 4),

            // revert low buffer size
            ("Reverting Low Buffer Sizes", async () => localSettings.Values.Remove("Sound"), null),

            // apply audio service affinity
            ("Applying Audio Service Affinity", async () => { foreach (var process in Process.GetProcessesByName("audiodg").Concat([Process.GetProcessById(ServicesHelper.GetServicePid("Audiosrv"))])) using (process) PInvoke.SetProcessAffinityMask((HANDLE)process.Handle, (nuint)audioController.AssignmentSetOverride); }, () => AUDIO_AFFINITY),
        };

        return actions;
    }
}