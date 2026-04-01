using AutoOS.Views.Settings.Power;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        Guid guid = Guid.Empty;

        return
        [
            //// update power plan
            //("Selecting AutoOS Power Plan", async () => guid = PowerApi.GetPlanGuidByName("AutoOS"), null),
            //(@"Setting ""Heterogeneous short running thread scheduling policy"" to ""All processors""", async () => PowerApi.WriteACValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("bae08b81-2d5e-4688-ad6a-13243356654b"), 0), null),
            //(@"Setting ""Heterogeneous short running thread scheduling policy"" to ""All processors""", async () => PowerApi.WriteDCValueIndex(guid, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("bae08b81-2d5e-4688-ad6a-13243356654b"), 0), null),
            //("Applying Changes", async () =>  PowerApi.PowerSetActiveScheme(guid), null)
        ];
    }
}