#if MACCATALYST
using Foundation;
using UIKit;
#endif

namespace Greenshot.Maui;

public partial class MainPage
{
	private enum ClipboardPayloadKind
	{
		None,
		Text,
		Image
	}

	private sealed record ClipboardPayload(ClipboardPayloadKind Kind, string? Text = null, string? ImagePath = null);

	private static Task CopyImageToClipboardAsync(string imagePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

#if MACCATALYST
		var image = UIImage.FromFile(imagePath);
		if (image is null)
		{
			throw new InvalidOperationException("The saved image could not be loaded for clipboard copy.");
		}

		UIPasteboard.General.Image = image;
#endif

		return Task.CompletedTask;
	}

	private async void OnCopyToClipboardClicked(object? sender, EventArgs e)
		=> await CopyCurrentImageToClipboardAsync();

	private async Task CopyCurrentImageToClipboardAsync()
	{
		string imagePath;
		string statusText;

		if (_imageEditorSession.IsEditing)
		{
			if (string.IsNullOrWhiteSpace(_imageEditorSession.SourceImagePath))
			{
				await DisplayAlertAsync("Nothing to copy", "Capture or open an image first.", "OK");
				return;
			}

			if (_imageEditorSession.Annotations.Count > 0)
			{
				imagePath = await _imageEditorService.SaveAnnotatedCopyAsync(
					_imageEditorSession.SourceImagePath,
					_imageEditorSession.Annotations);
				statusText = "Edited image copied to clipboard";
			}
			else
			{
				imagePath = _imageEditorSession.SourceImagePath;
				statusText = "Image copied to clipboard";
			}
		}
		else if (!string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath))
		{
			imagePath = _workspaceSession.SelectedImagePath;
			statusText = "Image copied to clipboard";
		}
		else
		{
			await DisplayAlertAsync("Nothing to copy", "Open an image first.", "OK");
			return;
		}

		try
		{
			await CopyImageToClipboardAsync(imagePath);
			RefreshWorkspaceState(statusText);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Clipboard copy failed", ex.Message, "OK");
		}
	}

	private async void OnPasteFromClipboardClicked(object? sender, EventArgs e)
		=> await PasteClipboardAsync();

	private async Task PasteClipboardAsync()
	{
		if (!_imageEditorSession.IsEditing)
		{
			return;
		}

		try
		{
			var payload = await ReadClipboardPayloadAsync();
			switch (payload.Kind)
			{
				case ClipboardPayloadKind.Image when !string.IsNullOrWhiteSpace(payload.ImagePath):
					var imageInfo = await _imageEditorService.LoadAsync(payload.ImagePath);
					if (_imageEditorSession.AddImageAnnotation(payload.ImagePath, imageInfo) is not null)
					{
						RefreshWorkspaceState("Clipboard image pasted");
					}

					return;
				case ClipboardPayloadKind.Text when !string.IsNullOrWhiteSpace(payload.Text):
					if (_imageEditorSession.AddTextAnnotation(payload.Text) is not null)
					{
						RefreshWorkspaceState("Clipboard text pasted");
					}

					return;
				default:
					return;
			}
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Paste failed", ex.Message, "OK");
		}
	}

	private static async Task<ClipboardPayload> ReadClipboardPayloadAsync()
	{
#if MACCATALYST
		var pasteboard = UIPasteboard.General;
		if (pasteboard.HasImages)
		{
			var image = pasteboard.Image;
			if (image is not null)
			{
				var imagePath = SaveClipboardImageToCache(image);
				if (!string.IsNullOrWhiteSpace(imagePath))
				{
					return new ClipboardPayload(ClipboardPayloadKind.Image, ImagePath: imagePath);
				}
			}
		}

		if (pasteboard.HasStrings)
		{
			var text = pasteboard.String;
			if (!string.IsNullOrWhiteSpace(text))
			{
				return new ClipboardPayload(ClipboardPayloadKind.Text, Text: text);
			}
		}

		return new ClipboardPayload(ClipboardPayloadKind.None);
#else
		if (await Clipboard.Default.HasTextAsync())
		{
			var text = await Clipboard.Default.GetTextAsync();
			return string.IsNullOrWhiteSpace(text)
				? new ClipboardPayload(ClipboardPayloadKind.None)
				: new ClipboardPayload(ClipboardPayloadKind.Text, Text: text);
		}

		return new ClipboardPayload(ClipboardPayloadKind.None);
#endif
	}

#if MACCATALYST
	private static string SaveClipboardImageToCache(UIImage image)
	{
		var pngData = image.AsPNG();
		if (pngData is null)
		{
			throw new InvalidOperationException("Clipboard image data could not be encoded as PNG.");
		}

		var imagePath = Path.Combine(
			FileSystem.Current.CacheDirectory,
			$"greenshot-clipboard-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}.png");

		if (!pngData.Save(imagePath, false, out NSError? error))
		{
			throw new InvalidOperationException(error?.LocalizedDescription ?? "Clipboard image data could not be written to cache.");
		}

		return imagePath;
	}
#endif
}
