using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions()
    {
        return
        [
            // disable new windows start menu layout
            (@"Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""EnabledState"" /t REG_DWORD /d 1 /f"), null),
            (@"Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""EnabledStateOptions"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""Variant"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""VariantPayload"" /t REG_DWORD /d 0 /f"), null),
            (@"Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""VariantPayloadKind"" /t REG_DWORD /d 0 /f"), null),
        ];
    }
}
