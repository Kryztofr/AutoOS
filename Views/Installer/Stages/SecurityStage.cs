using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace AutoOS.Views.Installer.Stages;

public static class SecurityStage
{
    public static IntPtr WindowHandle { get; private set; }
    public static async Task Run()
    {
        WindowHandle = WindowNative.GetWindowHandle(App.MainWindow);
        bool? WindowsDefender = PreparingStage.WindowsDefender;
        bool? UserAccountControl = PreparingStage.UserAccountControl;
        bool? DEP = PreparingStage.DEP;
        bool? MemoryIntegrity = PreparingStage.MemoryIntegrity;
        bool? INTELCPU = PreparingStage.INTELCPU;
        bool? AMDCPU = PreparingStage.AMDCPU;
        bool? SpectreMeltdownMitigations = PreparingStage.SpectreMeltdownMitigations;
        bool? ProcessMitigations = PreparingStage.ProcessMitigations;

        InstallPage.Status.Text = "Configuring Security...";

        string previousTitle = string.Empty;
        int stagePercentage = 5;

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // enable "hide windows security systray" policy
            (@"Enabling ""Hide Windows Security Systray"" policy", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray"" /v HideSystray /t REG_DWORD /d 1 /f"), null),
            
            // remove "securityhealthsystray" from startup
            (@"Removing ""SecurityHealthSystray"" from startup", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v SecurityHealth /f"), null),

            // enable "do not preserve zone information in file attachments"
            (@"Enabling ""Do not preserve zone information in file attachments""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments"" /v SaveZoneInformation /t REG_DWORD /d 1 /f"), null),

            // set "inclusion list for moderate risk file types"" policy to ".bat;.cmd;.vbs;.ps1;.reg;.js;.exe;.msi;"
            (@"Setting ""Inclusion list for moderate risk file types"" policy to "".bat;.cmd;.vbs;.ps1;.reg;.js;.exe;.msi;""", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Associations"" /v ModRiskFileTypes /t REG_SZ /d "".bat;.cmd;.vbs;.reg;.js;.exe;.msi;"" /f"), null),

            // set execution policy to unrestricted
            ("Setting execution policy to unrestricted", async () => await ProcessActions.RunPowerShell("Set-ExecutionPolicy Unrestricted -Force"), null),

            // disable windows defender services & drivers
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"sc stop wscsvc"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender"" /v PassiveMode /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiSpyware /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender"" /v DisableAntiVirus /t REG_DWORD /d 1 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"""C:\Program Files\Windows Defender\MpCmdRun.exe"" -DisableService -HighPriority"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\MsSecCore"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SecurityHealthService"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Sense"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdBoot"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdFilter"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisDrv"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdNisSvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefsvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\webthreatdefusersvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WinDefend"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),
            ("Disabling Windows Defender Services & Drivers", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wscsvc"" /v Start /t REG_DWORD /d 4 /f"), () => WindowsDefender == false),

            // disable smartscreen
            ("Disabling Smartscreen", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c taskkill /f /im smartscreen.exe & ren C:\Windows\System32\smartscreen.exe smartscreen.exee"), null),

            // disable tsx
            ("Disabling Transactional Synchronization Extensions (TSX)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v DisableTsx /t REG_DWORD /d 1 /f"), null),

            // enable windows hardware quality labs (whql) driver enforcement
            ("Enabling Windows Hardware Quality Labs (WHQL) driver enforcement", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CI\Policy"" /v WhqlSettings /t REG_DWORD /d 1 /f"), null),

            // enable uac
            ("Enabling user account control (UAC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v EnableLUA /t REG_DWORD /d 1 /f"), () => UserAccountControl == true),
            ("Enabling user account control (UAC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v PromptOnSecureDesktop /t REG_DWORD /d 1 /f"), () => UserAccountControl == true),
            ("Enabling user account control (UAC)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 5 /f"), () => UserAccountControl == true),

            // disable data execution prevention (dep)
            ("Disabling data execution prevention (DEP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", "bcdedit /set nx AlwaysOff"), () => DEP == false),

            // disable hypervisor enforced code integrity (hvci)
            ("Disabling Hypervisor Enforced Code Integrity (HVCI)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"" /v Enabled /t REG_DWORD /d 0 /f"), () => MemoryIntegrity == false),

            // enable spectre and meltdown mitigations
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 1 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverrideMask"" /t REG_DWORD /d 3 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverride"" /t REG_DWORD /d 64 /f"), () => AMDCPU == true && SpectreMeltdownMitigations == true),
            ("Enabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 0 /f"), () => INTELCPU == true && SpectreMeltdownMitigations == true),

            // disable spectre and meltdown mitigations
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d 1 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverrideMask"" /t REG_DWORD /d 3 /f"), () => SpectreMeltdownMitigations == false),
            ("Disabling Spectre & Meltdown Mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverride"" /t REG_DWORD /d 3 /f"), () => SpectreMeltdownMitigations == false),
            
            // disable process mitigations
            ("Disabling process mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v ""MitigationAuditOptions"" /t REG_BINARY /d 222222222222222222222222222222222222222222222222 /f"), () => ProcessMitigations == false),
            ("Disabling process mitigations", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"" /v ""MitigationOptions"" /t REG_BINARY /d 222222222222222222222222222222222222222222222222 /f"), () => ProcessMitigations == false),
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

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

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
                        InstallPage.Info.Title += ": " + ex.Message;
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Error);
                        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;

                        var tcs = new TaskCompletionSource<bool>();

                        InstallPage.ResumeButton.Click += (sender, e) =>
                        {
                            tcs.TrySetResult(true);
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                            TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
                            InstallPage.ProgressRingControl.Foreground = null;
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                        };

                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
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
                    InstallPage.Info.Title += ": " + ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Error);
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.ClearValue(ProgressBar.ForegroundProperty);
                        TaskbarHelper.SetProgressState(WindowHandle, TaskbarStates.Normal);
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
            TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
        if (filteredActions.Count == 0)
        {
            InstallPage.Progress.Value += stagePercentage;
            TaskbarHelper.SetProgressValue(WindowHandle, InstallPage.Progress.Value, 100);
        }
    }
}