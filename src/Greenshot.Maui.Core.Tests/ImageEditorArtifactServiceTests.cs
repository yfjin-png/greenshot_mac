using Greenshot.Maui.Core.Services;
using SixLabors.Fonts;
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
	public async Task SaveAnnotatedCopyAsync_WritesPencilMarkupIntoOutputPng()
	{
		var sourcePath = await CreateWhiteImageAsync(80, 60);
		var destinationPath = Path.Combine(_workspaceDirectory, "pencil.png");
		var annotations = new[]
		{
			new ImageEditorAnnotation(
				Guid.NewGuid(),
				ImageEditorTool.Pencil,
				new ImageEditorPoint(10, 12),
				new ImageEditorPoint(44, 32),
				ImageEditorToolDefaults.Get(ImageEditorTool.Pencil).Style,
				string.Empty,
				PathPoints:
				[
					new ImageEditorPoint(10, 12),
					new ImageEditorPoint(18, 20),
					new ImageEditorPoint(28, 18),
					new ImageEditorPoint(36, 28),
					new ImageEditorPoint(44, 32)
				])
		};

		await ImageEditorArtifactService.SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath);

		using var editedImage = await Image.LoadAsync<Rgba32>(destinationPath);
		Assert.True(CountNonWhitePixels(editedImage, 8, 10, 48, 36) > 0);
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
	public async Task SaveAnnotatedCopyAsync_WritesJapaneseTextMarkupIntoOutputPng()
	{
		var sourcePath = await CreateWhiteImageAsync(180, 100);
		var destinationPath = Path.Combine(_workspaceDirectory, "text-ja.png");
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
				new ImageEditorPoint(12, 12),
				new ImageEditorPoint(168, 72),
				style,
				"日本語テスト")
		};

		await ImageEditorArtifactService.SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath);

		using var editedImage = await Image.LoadAsync<Rgba32>(destinationPath);
		Assert.True(CountNonWhitePixels(editedImage, 24, 18, 156, 68) > 0);
	}

	[Fact]
	public void ResolveFontFamilies_SkipsMissingFamiliesAndReturnsAvailableMatches()
	{
		var existingFamily = SystemFonts.Families.First();

		var resolvedFamilies = TextFontResolver.ResolveFontFamilies(
			SystemFonts.Collection,
			["Definitely Missing Font", existingFamily.Name]);

		Assert.Contains(resolvedFamilies, family => string.Equals(family.Name, existingFamily.Name, StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ResolveFallbackFamilies_DoesNotRepeatPrimaryFamily()
	{
		var primaryFamily = SystemFonts.Families.First();

		var fallbackFamilies = TextFontResolver.ResolveFallbackFamilies(SystemFonts.Collection, primaryFamily);

		Assert.DoesNotContain(fallbackFamilies, family => string.Equals(family.Name, primaryFamily.Name, StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void LoadFontFamilies_LoadsBundledOpenSansFont()
	{
		var fontPath = GetRepoPath("src/Greenshot.Maui/Resources/Fonts/OpenSans-Regular.ttf");

		var families = TextFontResolver.LoadFontFamilies([fontPath]);

		Assert.Contains(families, family => string.Equals(family.Name, "Open Sans", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ResolvePrimaryFamily_UsesBundledFontWhenSystemFontsAreUnavailable()
	{
		var emptyFontCollection = new FontCollection();
		var bundledFamilies = TextFontResolver.LoadFontFamilies([GetRepoPath("src/Greenshot.Maui/Resources/Fonts/OpenSans-Regular.ttf")]);

		var primaryFamily = TextFontResolver.ResolvePrimaryFamily(emptyFontCollection, bundledFamilies);

		Assert.Equal("Open Sans", primaryFamily.Name);
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

	[Fact]
	public async Task SaveAnnotatedCopyAsync_WritesPastedImageIntoOutputPng()
	{
		var sourcePath = await CreateWhiteImageAsync(120, 90);
		var overlayPath = await CreateSolidImageAsync(24, 20, new Rgba32(20, 120, 220, 255));
		var destinationPath = Path.Combine(_workspaceDirectory, "pasted-image.png");
		var annotations = new[]
		{
			new ImageEditorAnnotation(
				Guid.NewGuid(),
				ImageEditorTool.Image,
				new ImageEditorPoint(20, 18),
				new ImageEditorPoint(68, 58),
				ImageEditorToolDefaults.Get(ImageEditorTool.Image).Style,
				string.Empty,
				overlayPath)
		};

		await ImageEditorArtifactService.SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath);

		using var editedImage = await Image.LoadAsync<Rgba32>(destinationPath);
		Assert.Equal(new Rgba32(20, 120, 220, 255), editedImage[30, 28]);
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

	private async Task<string> CreateSolidImageAsync(int width, int height, Rgba32 color)
	{
		Directory.CreateDirectory(_workspaceDirectory);
		var filePath = Path.Combine(_workspaceDirectory, $"{Guid.NewGuid():N}.png");

		using var image = new Image<Rgba32>(width, height, color);
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

	private static string GetRepoPath(string relativePath)
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
		return Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
	}
}
