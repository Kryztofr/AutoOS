using AutoOS.Views.Settings.Power;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        return
        [
            //(@"Setting ""Processor efficiency containment concurrency threshold"" to 100", async () => PowerApi.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("69439b22-221b-4830-bd34-f7bcece24583"), 100), null),
            //(@"Setting ""Processor hybrid containment concurrency threshold"" to 100", async () => PowerApi.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("6788488b-1b90-4d11-8fa7-973e470dff47"), 100), null)
        ];
    }
}