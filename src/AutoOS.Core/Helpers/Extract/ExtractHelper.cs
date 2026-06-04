using System.Diagnostics;

namespace AutoOS.Core.Helpers.Extract;

public static partial class ExtractHelper
{
	public static async Task Extract(string inputPath, string outputPath)
	{
		if (!File.Exists(inputPath))
			throw new FileNotFoundException("File not found", inputPath);
		
		await Process.Start(new ProcessStartInfo
		{
			FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "7-Zip", "7z.exe"),
			Arguments = @$"x ""{inputPath}"" -y -o""{outputPath}""",
			CreateNoWindow = true
		})!.WaitForExitAsync();
	}
}
