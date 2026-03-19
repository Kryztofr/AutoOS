using AutoOS.Views.Installer.Actions;
using System.Diagnostics;
using AutoOS.Helpers.Registry;
using Microsoft.Win32;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class RuntimesStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        return new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download the visual c++ redistributable
            ("Downloading the Visual C++ Redistributable", async () => await ProcessActions.RunDownload("https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe", ApplicationData.Current.TemporaryFolder.Path, "VisualCppRedist_AIO_x86_x64.exe"), null),

            // install visual c++ redistributable
            ("Installing the Visual C++ Redistributable", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VisualCppRedist_AIO_x86_x64.exe"), Arguments = "/ai /gm2", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("VisualCppRedist_AIO_x86_x64.exe")).DeleteAsync(); }, null),

            // download the microsoft edge webview2 runtime
            ("Downloading the Microsoft Edge WebView2 Runtime", async () => await ProcessActions.RunDownload("https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/7dedb563-79f6-48af-b588-dd8e97f4b73c/MicrosoftEdgeWebView2RuntimeInstallerX64.exe", ApplicationData.Current.TemporaryFolder.Path, "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), null),

            // install microsoft edge webview2 runtime
            ("Installing the Microsoft Edge WebView2 Runtime", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), Arguments = "/silent /install", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("MicrosoftEdgeWebView2RuntimeInstallerX64.exe")).DeleteAsync(); }, null),
            ("Installing the Microsoft Edge WebView2 Runtime", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MicrosoftEdgeUpdate.exe", "Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String), null),

            // download microsoft windows app runtime
            ("Downloading the Microsoft Windows App Runtime", async () => await ProcessActions.RunDownload("https://aka.ms/windowsappsdk/1.6/1.6.250602001/windowsappruntimeinstall-x64.exe", ApplicationData.Current.TemporaryFolder.Path, "WindowsAppRuntimeInstall-x64.exe"), null),

            // install microsoft windows app runtime
            ("Installing the Microsoft Windows App Runtime", async () => { await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "WindowsAppRuntimeInstall-x64.exe"), Arguments = "--quiet --force --msix --license" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(); await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("WindowsAppRuntimeInstall-x64.exe")).DeleteAsync(); }, null),

            // download the directx redistributable
            ("Downloading the DirectX Redistributable", async () => await ProcessActions.RunDownload("https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe", ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist.exe"), null),

            // extract the directx redistributable
            ("Extracting the DirectX Redistributable", async () => await ProcessActions.RunExtract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist.exe"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist")),null),

            // install the directx redistributable
            ("Installing the DirectX Redistributable", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist", "DXSetup.exe"), Arguments = "/silent", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
            ("Installing the DirectX Redistributable", async () => await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("directx_Jun2010_redist.exe")).DeleteAsync(), null),
            ("Installing the DirectX Redistributable", async () => await (await ApplicationData.Current.TemporaryFolder.GetFolderAsync("directx_Jun2010_redist")).DeleteAsync(), null),
        };
    }
}
