using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class PackageStage
{
    public static async Task PackageActions(string downloadUrl, UpdateDialog dialog)
    {
        StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("AutoOS Updater", CreationCollisionOption.OpenIfExists);
        StorageFile tempFile = await tempFolder.CreateFileAsync("AutoOS.msix", CreationCollisionOption.ReplaceExisting);

        await dialog.RunDownload(downloadUrl, tempFolder.Path, "AutoOS.msix", "Downloading Update...", 0, 50);

        dialog.SetStatus("Installing Update...");
        PInvoke.RegisterApplicationRestart(null, 0);

        var pm = new PackageManager();
        var deploymentOperation = pm.AddPackageAsync(new Uri(tempFile.Path), null, DeploymentOptions.ForceApplicationShutdown);

        deploymentOperation.Progress = (info, progress) =>
        {
            _ = dialog.DispatcherQueue.TryEnqueue(() =>
            {
                if (progress.percentage > 80)
                {
                    dialog.SetProgress(100);
                    dialog.SetSuccess();
                }
                else
                {
                    double scaledProgress = 50 + (progress.percentage / 80.0 * 50);
                    dialog.SetProgress(scaledProgress);
                    dialog.SetStatus($"Installing Update ({Math.Round(progress.percentage / 80.0 * 100)}%)...");
                }
            });
        };

        await deploymentOperation;
    }
}
