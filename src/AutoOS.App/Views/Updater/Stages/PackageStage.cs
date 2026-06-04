using System.Security.Cryptography.X509Certificates;
using Windows.Management.Deployment;
using Windows.Win32;

namespace AutoOS.Views.Updater.Stages;

public static class PackageStage
{
	public static async Task PackageActions(string downloadUrl, UpdateDialog dialog)
	{
		string tempFolderPath = Path.Combine(Path.GetTempPath(), "AutoOS Updater");
		Directory.CreateDirectory(tempFolderPath);
		string tempFilePath = Path.Combine(tempFolderPath, "AutoOS.msix");
		string cerFilePath = Path.Combine(tempFolderPath, "AutoOS.cer");

		await dialog.Download(downloadUrl.Replace("AutoOS.msix", "AutoOS.cer"), tempFolderPath, "AutoOS.cer", "Downloading Certificate...", 0, 25);
		dialog.SetStatus("Installing Certificate...");
		using (X509Store store = new(StoreName.Root, StoreLocation.LocalMachine))
		{
			store.Open(OpenFlags.ReadWrite);
			var cert = X509CertificateLoader.LoadCertificateFromFile(cerFilePath);
			foreach (var oldCert in store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cert.Subject, false))
			{
				if (oldCert.Thumbprint != cert.Thumbprint)
					store.Remove(oldCert);
			}
			store.Add(cert);
		}

		await dialog.Download(downloadUrl, tempFolderPath, "AutoOS.msix", "Downloading Update", 50, 75);

		dialog.SetStatus("Installing Update...");
		PInvoke.RegisterApplicationRestart(null, 0);
		var packageManager = new PackageManager();
		var deploymentOperation = packageManager.AddPackageAsync(new Uri(tempFilePath), null, DeploymentOptions.ForceApplicationShutdown);
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
					double scaledProgress = 75 + (progress.percentage / 80.0 * 25);
					dialog.SetProgress(scaledProgress);
					dialog.SetStatus($"Installing Update ({Math.Round(progress.percentage / 80.0 * 100)}%)...");
				}
			});
		};
		await deploymentOperation;
	}
}
