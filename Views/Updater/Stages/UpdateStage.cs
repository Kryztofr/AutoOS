using AutoOS.Views.Installer.Actions;

namespace AutoOS.Views.Updater.Stages;

public static class UpdateStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
    {
        return
        [
            // disable new windows start menu layout
            ("Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""EnabledState"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""EnabledStateOptions"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""Variant"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""VariantPayload"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling new Windows Start Menu Layout", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FeatureManagement\Overrides\8\3036241548"" /v ""VariantPayloadKind"" /t REG_DWORD /d 0 /f"), null),
        
            // stop windhawk
            ("Stopping Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc stop Windhawk"), null),
            ("Stopping Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c taskkill /f /im Windhawk.exe"), null),
            ("Stopping Windhawk", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c rmdir /s /q ""C:\ProgramData\Windhawk"""), null),

            // download windhawk
            ("Downloading Windhawk", async () => await dialog.Download("https://www.dl.dropboxusercontent.com/scl/fi/omk2gg29v8yguskw4jhng/Windhawk.zip?rlkey=tljvtfus2tq57d3y5mzdt8ges&st=5h7z80ir&dl=0", Path.GetTempPath(), "Windhawk.zip", "Downloading Windhawk", dialog.CurrentGroupStart, dialog.CurrentGroupTarget), null),
        
            // extract windawhk
            ("Extracting Windhawk", async () => await ProcessActions.RunExtract(Path.Combine(Path.GetTempPath(), "Windhawk.zip"), Path.Combine(Path.GetTempPath(), "Windhawk")), null),

            // update windhawk mods
            ("Updating Windhawk Mods", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c robocopy ""%TEMP%\Windhawk\Windhawk"" ""C:\ProgramData\Windhawk"" /MOVE /E"), null),
            ("Updating Windhawk Mods", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher"" /v LibraryFileName /t REG_SZ /d ""auto-theme-switcher_1.2.0_921892.dll"" /f"), null),
            ("Updating Windhawk Mods", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Windhawk\Engine\Mods\auto-theme-switcher"" /v Version /t REG_SZ /d ""1.2.0"" /f"), null),
            ("Updating Windhawk Mods", async () => await ProcessActions.RunPowerShell("Stop-Process -Name explorer -Force"), null),
            ("Updating Windhawk Mods", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc start Windhawk"), null)

        ];
    }
}
