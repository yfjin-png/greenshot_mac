using Greenshot.Maui.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Maui.Core.Tests;

public sealed class ImageEditorArtifactServiceTests : IDisposable
{
	private readonly string _workspaceDirectory = Path.Combine(
		Path.GetTempPath(),
		$"greenshot-maui-core-tests-{Guid.NewGuid():N}");

	[Fact]
	public async Task IdentifyAsync_ReturnsImageDimensions()
	{
		var sourcePath = await CreateWhiteImageAsync(48, 32);

		var info = await ImageEditorArtifactService.IdentifyAsync(sourcePath);

		Assert.Equal(48, info.PixelWidth);
		Assert.Equal(32, info.PixelHeight);
	}

	[Fact]
	public async Task SaveAnnotatedCopyAsync_WritesRectangleMarkupIntoOutputPng()
	{
		var sourcePath = await CreateWhiteImageAsync(80, 60);
		var destinationPath = Path.Combine(_workspaceDirectory, "edited.png");
		var annotations = new[]
		{
			new ImageEditorAnnotation(
				Guid.NewGuid(),
				ImageEditorTool.Rectangle,
				new ImageEditorPoint(10, 10),
				new ImageEditorPoint(50, 40),
				ImageEditorToolDefaults.Get(ImageEditorTool.Rectangle).Style,
				ImageEditorToolDefaults.Get(ImageEditorTool.Rectangle).Text)
		};

		await ImageEditorArtifactService.SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath);

		Assert.True(File.Exists(destinationPath));

		using var editedImage = await Image.LoadAsync<Rgba32>(destinationPath);
		var markedPixel = editedImage[10, 10];
		Assert.NotEqual(new Rgba32(255, 255, 255, 255), markedPixel);
	}

	[Fact]
	public async Task SaveAnnotatedCopyAsync_WritesHighlightMarkupIntoOutputPng()
	{
		var sourcePath = await CreateWhiteImageAsync(80, 60);
		var destinationPath = Path.Combine(_workspaceDirectory, "highlighted.png");
		var annotations = new[]
		{
			new ImageEditorAnnotation(
				Guid.NewGuid(),
				ImageEditorTool.Highlight,
				new ImageEditorPoint(15, 12),
				new ImageEditorPoint(45, 36),
				ImageEditorToolDefaults.Get(ImageEditorTool.Highlight).Style,
				ImageEditorToolDefaults.Get(ImageEditorTool.Highlight).Text)
		};

		await ImageEditorArtifactService.SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath);

		using var editedImage = await Image.LoadAsync<Rgba32>(destinationPath);
		var highlightedPixel = editedImage[25, 24];
		Assert.NotEqual(new Rgba32(255, 255, 255, 255), highlightedPixel);
	}

	[Fact]
	public async Task SaveAnnotatedCopyAsync_WritesTextMarkupIntoOutputPng()
	{
		var sourcePath = await CreateWhiteImageAsync(120, 90);
		var destinationPath = Path.Combine(_workspaceDirectory, "text.png");
		var style = ImageEditorToolDefaults.Get(ImageEditorTool.Text).Style with
		{
			StrokeColor = new ImageEditorColor(0, 0, 0, 0),
			FillColor = new ImageEditorColor(0, 0, 0, 0),
			TextColor = new ImageEditorColor(34, 34, 34),
			TextSize = 28f
		};
		var annotations = new[]
		{
			new ImageEditorAnnotation(
				Guid.NewGuid(),
				ImageEditorTool.Text,
				new ImageEditorPoint(10, 10),
				new ImageEditorPoint(110, 70),
				style,
				"TEST")
		};

		await ImageEditorArtifactService.SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath);

		using var editedImage = await Image.LoadAsync<Rgba32>(destinationPath);
		Assert.True(CountNonWhitePixels(editedImage, 24, 20, 96, 60) > 0);
	}

	[Fact]
	public async Task SaveAnnotatedCopyAsync_WritesCalloutMarkupIntoOutputPng()
	{
		var sourcePath = await CreateWhiteImageAsync(120, 90);
		var destinationPath = Path.Combine(_workspaceDirectory, "callout.png");
		var style = ImageEditorToolDefaults.Get(ImageEditorTool.SpeechBubble).Style with
		{
			FillColor = new ImageEditorColor(214, 234, 252, 255),
			StrokeColor = new ImageEditorColor(41, 107, 163),
			TextColor = new ImageEditorColor(34, 34, 34)
		};
		var annotations = new[]
		{
			new ImageEditorAnnotation(
				Guid.NewGuid(),
				ImageEditorTool.SpeechBubble,
				new ImageEditorPoint(14, 12),
				new ImageEditorPoint(100, 56),
				style,
				"Callout")
		};

		await ImageEditorArtifactService.SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath);

		using var editedImage = await Image.LoadAsync<Rgba32>(destinationPath);
		Assert.NotEqual(new Rgba32(255, 255, 255, 255), editedImage[40, 30]);
		Assert.NotEqual(new Rgba32(255, 255, 255, 255), editedImage[30, 64]);
	}

	public void Dispose()
	{
		if (Directory.Exists(_workspaceDirectory))
		{
			Directory.Delete(_workspaceDirectory, recursive: true);
		}
	}

	private async Task<string> CreateWhiteImageAsync(int width, int height)
	{
		Directory.CreateDirectory(_workspaceDirectory);
		var filePath = Path.Combine(_workspaceDirectory, $"{Guid.NewGuid():N}.png");

		using var image = new Image<Rgba32>(width, height, new Rgba32(255, 255, 255, 255));
		await image.SaveAsPngAsync(filePath);

		return filePath;
	}

	private static int CountNonWhitePixels(
		Image<Rgba32> image,
		int startX,
		int startY,
		int endX,
		int endY)
	{
		var count = 0;
		for (var y = startY; y < endY; y++)
		{
			for (var x = startX; x < endX; x++)
			{
				if (image[x, y] != new Rgba32(255, 255, 255, 255))
				{
					count++;
				}
			}
		}

		return count;
	}
}
