using System.Security.Cryptography.X509Certificates;
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

        StorageFile cerFile = await tempFolder.CreateFileAsync("AutoOS.cer", CreationCollisionOption.ReplaceExisting);
        await dialog.Download(downloadUrl.Replace("AutoOS.msix", "AutoOS.cer"), tempFolder.Path, "AutoOS.cer", "Downloading Certificate...", 0, 25);
        dialog.SetStatus("Installing Certificate...");
        using (X509Store store = new(StoreName.Root, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadWrite);
            var cert = X509CertificateLoader.LoadCertificateFromFile(cerFile.Path);
            foreach (var oldCert in store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cert.Subject, false))
            {
                if (oldCert.Thumbprint != cert.Thumbprint)
                    store.Remove(oldCert);
            }
            store.Add(cert);
        }

        dialog.SetProgress(50);
        await dialog.Download(downloadUrl, tempFolder.Path, "AutoOS.msix", "Downloading Update...", 50, 75);
        double startValue = 75;
        double weight = 25;

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
                    double scaledProgress = startValue + (progress.percentage / 80.0 * weight);
                    dialog.SetProgress(scaledProgress);
                    dialog.SetStatus($"Installing Update ({Math.Round(progress.percentage / 80.0 * 100)}%)...");
                }
            });
        };
        await deploymentOperation;
    }
}
