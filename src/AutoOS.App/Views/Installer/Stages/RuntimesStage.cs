using AutoOS.Common;
using AutoOS.Core.Helpers.Download;
using AutoOS.Core.Helpers.Extract;
using AutoOS.Core.Helpers.Registry;
using AutoOS.Views.Installer.Actions;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.Storage;

namespace AutoOS.Views.Installer.Stages;

public static class RuntimesStage
{
    public static List<(string Title, Func<Task> Action, Func<bool> Condition)> GetActions()
    {
        return new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // download the visual c++ redistributable
            ("Downloading Visual C++ Redistributable", async () => await DownloadHelper.Download("https://github.com/abbodi1406/vcredist/releases/latest/download/VisualCppRedist_AIO_x86_x64.exe", ApplicationData.Current.TemporaryFolder.Path, "VisualCppRedist_AIO_x86_x64.exe", new InstallPageReporter()), null),

            // install visual c++ redistributable
            ("Installing Visual C++ Redistributable", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "VisualCppRedist_AIO_x86_x64.exe"), Arguments = "/ai /gm2", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
            ("Cleaning up Visual C++ Redistributable files", async () => await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("VisualCppRedist_AIO_x86_x64.exe")).DeleteAsync(), null),

            // download the microsoft edge webview2 runtime
            ("Downloading Microsoft Edge WebView2 Runtime", async () => await DownloadHelper.Download("https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/7dedb563-79f6-48af-b588-dd8e97f4b73c/MicrosoftEdgeWebView2RuntimeInstallerX64.exe", ApplicationData.Current.TemporaryFolder.Path, "MicrosoftEdgeWebView2RuntimeInstallerX64.exe", new InstallPageReporter()), null),

            // install microsoft edge webview2 runtime
            ("Installing Microsoft Edge WebView2 Runtime", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"), Arguments = "/silent /install", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
            ("Installing Microsoft Edge WebView2 Runtime", async () => RegistryHelper.SetValue(RegistryHelper.Identity.TrustedInstaller, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MicrosoftEdgeUpdate.exe", "Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String), null),
            ("Cleaning up Microsoft Edge WebView2 Runtime files", async () => await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("MicrosoftEdgeWebView2RuntimeInstallerX64.exe")).DeleteAsync(), null),

            // download microsoft windows app runtime
            ("Downloading Microsoft Windows App Runtime", async () => await DownloadHelper.Download("https://aka.ms/windowsappsdk/1.6/1.6.250602001/windowsappruntimeinstall-x64.exe", ApplicationData.Current.TemporaryFolder.Path, "WindowsAppRuntimeInstall-x64.exe", new InstallPageReporter()), null),

            // install microsoft windows app runtime
            ("Installing Microsoft Windows App Runtime", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "WindowsAppRuntimeInstall-x64.exe"), Arguments = "--quiet --force --msix --license" , WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
            ("Cleaning up Microsoft Windows App Runtime files", async () => await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("WindowsAppRuntimeInstall-x64.exe")).DeleteAsync(), null),

            // download the directx redistributable
            ("Downloading DirectX Redistributable", async () => await DownloadHelper.Download("https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe", ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist.exe", new InstallPageReporter()), null),

            // extract the directx redistributable
            ("Extracting DirectX Redistributable", async () => await ExtractHelper.Extract(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist.exe"), Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist")),null),

            // install the directx redistributable
            ("Installing DirectX Redistributable", async () => await Process.Start(new ProcessStartInfo { FileName = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "directx_Jun2010_redist", "DXSetup.exe"), Arguments = "/silent", WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExitAsync(), null),
            ("Cleaning up DirectX Redistributable files", async () => await (await ApplicationData.Current.TemporaryFolder.GetFileAsync("directx_Jun2010_redist.exe")).DeleteAsync(), null),
            ("Cleaning up DirectX Redistributable files", async () => await (await ApplicationData.Current.TemporaryFolder.GetFolderAsync("directx_Jun2010_redist")).DeleteAsync(), null)
        };
    }
}

