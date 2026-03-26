using System.Runtime.Versioning;
using CoreGraphics;
using Greenshot.Maui.Core.Services;
using UIKit;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

[SupportedOSPlatform("maccatalyst")]
internal sealed class MacCatalystPngArtifactWriter
{
	[SupportedOSPlatform("macos14.0")]
	public async Task<ScreenshotCaptureResult> WriteAsync(
		CGImage image,
		string filePrefix,
		string successMessage,
		string encodeFailureMessage,
		CancellationToken cancellationToken)
	{
		using var uiImage = UIImage.FromImage(image);
		using var data = uiImage?.AsPNG();
		if (data is null)
		{
			return ScreenshotCaptureResult.Failed(encodeFailureMessage);
		}

		var outputPath = Path.Combine(
			FileSystem.Current.CacheDirectory,
			$"{filePrefix}-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}.png");

		await File.WriteAllBytesAsync(outputPath, data.ToArray(), cancellationToken).ConfigureAwait(false);
		return ScreenshotCaptureResult.Success(outputPath, successMessage, (int)image.Width, (int)image.Height);
	}
}
