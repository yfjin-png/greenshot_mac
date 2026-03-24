using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;

namespace Greenshot.Maui.Core.Services;

public static class ImageEditorArtifactService
{
	public static async Task<ImageEditorDocumentInfo> IdentifyAsync(
		string filePath,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException("The selected image could not be found.", filePath);
		}

		await using var stream = File.OpenRead(filePath);
		var imageInfo = await Image.IdentifyAsync(stream, cancellationToken);
		if (imageInfo is null)
		{
			throw new InvalidOperationException("The selected file is not a supported image.");
		}

		return new ImageEditorDocumentInfo(imageInfo.Width, imageInfo.Height);
	}

	public static async Task<string> CreateAnnotatedCopyAsync(
		string sourcePath,
		IReadOnlyCollection<ImageEditorAnnotation> annotations,
		string outputDirectory,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

		Directory.CreateDirectory(outputDirectory);

		var destinationPath = Path.Combine(
			outputDirectory,
			$"greenshot-edit-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}.png");

		await SaveAnnotatedCopyAsync(sourcePath, annotations, destinationPath, cancellationToken);
		return destinationPath;
	}

	public static async Task SaveAnnotatedCopyAsync(
		string sourcePath,
		IReadOnlyCollection<ImageEditorAnnotation> annotations,
		string destinationPath,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

		if (!File.Exists(sourcePath))
		{
			throw new FileNotFoundException("The selected image could not be found.", sourcePath);
		}

		await using var sourceStream = File.OpenRead(sourcePath);
		using var image = await Image.LoadAsync<Rgba32>(sourceStream, cancellationToken);

		foreach (var annotation in annotations)
		{
			RenderAnnotation(image, annotation);
		}

		var destinationDirectory = Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrWhiteSpace(destinationDirectory))
		{
			Directory.CreateDirectory(destinationDirectory);
		}

		await image.SaveAsPngAsync(destinationPath, cancellationToken);
	}

	private static void RenderAnnotation(Image<Rgba32> image, ImageEditorAnnotation annotation)
	{
		switch (annotation.Tool)
		{
			case ImageEditorTool.Rectangle:
				DrawRectangle(image, annotation.Bounds, annotation.Style);
				break;
			case ImageEditorTool.Highlight:
				DrawHighlight(image, annotation.Bounds, annotation.Style);
				break;
			case ImageEditorTool.Arrow:
				DrawArrow(image, annotation, includeArrowHead: true);
				break;
			case ImageEditorTool.Line:
				DrawArrow(image, annotation, includeArrowHead: false);
				break;
			case ImageEditorTool.Text:
				DrawTextAnnotation(image, annotation);
				break;
			case ImageEditorTool.SpeechBubble:
				DrawSpeechBubbleAnnotation(image, annotation);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(annotation.Tool), annotation.Tool, "Unsupported annotation tool.");
		}
	}

	private static void DrawRectangle(Image<Rgba32> image, ImageEditorRect bounds, ImageEditorAnnotationStyle style)
	{
		if (IsVisible(style.FillColor))
		{
			FillRect(image, bounds, style.FillColor);
		}

		DrawRectangleOutline(image, bounds, style);
	}

	private static void DrawHighlight(Image<Rgba32> image, ImageEditorRect bounds, ImageEditorAnnotationStyle style)
	{
		FillRect(image, bounds, style.FillColor);
		DrawRectangleOutline(image, bounds, style);
	}

	private static void DrawArrow(Image<Rgba32> image, ImageEditorAnnotation annotation, bool includeArrowHead)
	{
		var start = ClampPoint(annotation.StartPoint, image.Width, image.Height);
		var end = ClampPoint(annotation.EndPoint, image.Width, image.Height);
		var strokeColor = ToRgba32(annotation.Style.StrokeColor);
		var thickness = Math.Max(annotation.Style.StrokeThickness, 2f);

		DrawStyledLine(image, start, end, strokeColor, thickness, annotation.Style.StrokeStyle);
		if (!includeArrowHead)
		{
			return;
		}

		var deltaX = end.X - start.X;
		var deltaY = end.Y - start.Y;
		var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
		if (length <= double.Epsilon)
		{
			return;
		}

		var directionX = deltaX / length;
		var directionY = deltaY / length;
		var headLength = Math.Max(14d, thickness * 4d);
		var headWidth = Math.Max(12d, thickness * 3d);
		var baseX = end.X - (directionX * headLength);
		var baseY = end.Y - (directionY * headLength);
		var perpendicularX = -directionY;
		var perpendicularY = directionX;

		var point1 = end;
		var point2 = new ImageEditorPoint(
			baseX + (perpendicularX * headWidth / 2d),
			baseY + (perpendicularY * headWidth / 2d));
		var point3 = new ImageEditorPoint(
			baseX - (perpendicularX * headWidth / 2d),
			baseY - (perpendicularY * headWidth / 2d));

		FillTriangle(image, point1, point2, point3, strokeColor);
	}

	private static void DrawTextAnnotation(Image<Rgba32> image, ImageEditorAnnotation annotation)
	{
		DrawRectangle(image, annotation.Bounds, annotation.Style);
		DrawText(image, annotation.Bounds, annotation.Text, annotation.Style);
	}

	private static void DrawSpeechBubbleAnnotation(Image<Rgba32> image, ImageEditorAnnotation annotation)
	{
		var rect = NormalizeRect(annotation.Bounds, image.Width, image.Height);
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		DrawRectangle(image, rect, annotation.Style);

		var tailBaseCenterX = rect.X + Math.Max(rect.Width * 0.28d, 18d);
		var tailBaseY = rect.Bottom;
		var tailHalfWidth = Math.Min(Math.Max(rect.Width * 0.08d, 10d), 24d);
		var tailTip = new ImageEditorPoint(
			rect.X + Math.Max(rect.Width * 0.14d, 12d),
			Math.Min(rect.Bottom + Math.Max(rect.Height * 0.20d, 18d), image.Height));
		var tailLeft = new ImageEditorPoint(tailBaseCenterX - tailHalfWidth, tailBaseY);
		var tailRight = new ImageEditorPoint(tailBaseCenterX + tailHalfWidth, tailBaseY);

		if (IsVisible(annotation.Style.FillColor))
		{
			FillTriangle(image, tailLeft, tailTip, tailRight, ToRgba32(annotation.Style.FillColor));
		}

		var strokeColor = ToRgba32(annotation.Style.StrokeColor);
		DrawStyledLine(image, tailLeft, tailTip, strokeColor, annotation.Style.StrokeThickness, annotation.Style.StrokeStyle);
		DrawStyledLine(image, tailTip, tailRight, strokeColor, annotation.Style.StrokeThickness, annotation.Style.StrokeStyle);

		DrawText(image, rect, annotation.Text, annotation.Style);
	}

	private static void DrawRectangleOutline(
		Image<Rgba32> image,
		ImageEditorRect bounds,
		ImageEditorAnnotationStyle style)
	{
		var rect = NormalizeRect(bounds, image.Width, image.Height);
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		var color = ToRgba32(style.StrokeColor);
		var topLeft = new ImageEditorPoint(rect.X, rect.Y);
		var topRight = new ImageEditorPoint(rect.Right, rect.Y);
		var bottomLeft = new ImageEditorPoint(rect.X, rect.Bottom);
		var bottomRight = new ImageEditorPoint(rect.Right, rect.Bottom);

		DrawStyledLine(image, topLeft, topRight, color, style.StrokeThickness, style.StrokeStyle);
		DrawStyledLine(image, topRight, bottomRight, color, style.StrokeThickness, style.StrokeStyle);
		DrawStyledLine(image, bottomRight, bottomLeft, color, style.StrokeThickness, style.StrokeStyle);
		DrawStyledLine(image, bottomLeft, topLeft, color, style.StrokeThickness, style.StrokeStyle);
	}

	private static void FillRect(Image<Rgba32> image, ImageEditorRect bounds, ImageEditorColor fillColor)
	{
		var rect = NormalizeRect(bounds, image.Width, image.Height);
		if (rect.Width <= 0 || rect.Height <= 0 || !IsVisible(fillColor))
		{
			return;
		}

		var left = (int)Math.Floor(rect.X);
		var top = (int)Math.Floor(rect.Y);
		var right = (int)Math.Ceiling(rect.Right);
		var bottom = (int)Math.Ceiling(rect.Bottom);
		var rgba = ToRgba32(fillColor);

		for (var y = top; y < bottom; y++)
		{
			for (var x = left; x < right; x++)
			{
				BlendPixel(image, x, y, rgba);
			}
		}
	}

	private static ImageEditorRect NormalizeRect(ImageEditorRect rect, int imageWidth, int imageHeight)
	{
		var normalized = rect.Width < 0 || rect.Height < 0
			? ImageEditorRect.FromPoints(
				new ImageEditorPoint(rect.X, rect.Y),
				new ImageEditorPoint(rect.Right, rect.Bottom))
			: rect;

		var x = Math.Clamp(normalized.X, 0d, imageWidth);
		var y = Math.Clamp(normalized.Y, 0d, imageHeight);
		var right = Math.Clamp(normalized.Right, 0d, imageWidth);
		var bottom = Math.Clamp(normalized.Bottom, 0d, imageHeight);

		return new ImageEditorRect(x, y, Math.Max(0d, right - x), Math.Max(0d, bottom - y));
	}

	private static ImageEditorPoint ClampPoint(ImageEditorPoint point, int imageWidth, int imageHeight) =>
		new(
			Math.Clamp(point.X, 0d, imageWidth),
			Math.Clamp(point.Y, 0d, imageHeight));

	private static void DrawHorizontalLine(Image<Rgba32> image, int startX, int endX, int y, Rgba32 color)
	{
		if (y < 0 || y >= image.Height)
		{
			return;
		}

		var clampedStart = Math.Max(0, startX);
		var clampedEnd = Math.Min(image.Width - 1, endX);
		for (var x = clampedStart; x <= clampedEnd; x++)
		{
			BlendPixel(image, x, y, color);
		}
	}

	private static void DrawVerticalLine(Image<Rgba32> image, int startY, int endY, int x, Rgba32 color)
	{
		if (x < 0 || x >= image.Width)
		{
			return;
		}

		var clampedStart = Math.Max(0, startY);
		var clampedEnd = Math.Min(image.Height - 1, endY);
		for (var y = clampedStart; y <= clampedEnd; y++)
		{
			BlendPixel(image, x, y, color);
		}
	}

	private static void DrawThickLine(
		Image<Rgba32> image,
		ImageEditorPoint start,
		ImageEditorPoint end,
		Rgba32 color,
		float thickness)
	{
		var radius = Math.Max(thickness / 2f, 1f);
		var minX = (int)Math.Floor(Math.Min(start.X, end.X) - radius - 1d);
		var maxX = (int)Math.Ceiling(Math.Max(start.X, end.X) + radius + 1d);
		var minY = (int)Math.Floor(Math.Min(start.Y, end.Y) - radius - 1d);
		var maxY = (int)Math.Ceiling(Math.Max(start.Y, end.Y) + radius + 1d);
		var squaredRadius = radius * radius;

		for (var y = minY; y <= maxY; y++)
		{
			for (var x = minX; x <= maxX; x++)
			{
				var distanceSquared = DistanceToSegmentSquared(
					x + 0.5d,
					y + 0.5d,
					start.X,
					start.Y,
					end.X,
					end.Y);
				if (distanceSquared <= squaredRadius)
				{
					BlendPixel(image, x, y, color);
				}
			}
		}
	}

	private static void DrawStyledLine(
		Image<Rgba32> image,
		ImageEditorPoint start,
		ImageEditorPoint end,
		Rgba32 color,
		float thickness,
		ImageEditorStrokeStyle strokeStyle)
	{
		if (strokeStyle == ImageEditorStrokeStyle.Dashed)
		{
			DrawDashedLine(image, start, end, color, thickness);
			return;
		}

		DrawThickLine(image, start, end, color, thickness);
	}

	private static void DrawDashedLine(
		Image<Rgba32> image,
		ImageEditorPoint start,
		ImageEditorPoint end,
		Rgba32 color,
		float thickness)
	{
		var deltaX = end.X - start.X;
		var deltaY = end.Y - start.Y;
		var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
		if (length <= double.Epsilon)
		{
			return;
		}

		var directionX = deltaX / length;
		var directionY = deltaY / length;
		var dashLength = Math.Max(12d, thickness * 4d);
		var gapLength = Math.Max(8d, thickness * 2d);
		var current = 0d;

		while (current < length)
		{
			var segmentStart = current;
			var segmentEnd = Math.Min(current + dashLength, length);
			var dashStart = new ImageEditorPoint(
				start.X + (directionX * segmentStart),
				start.Y + (directionY * segmentStart));
			var dashEnd = new ImageEditorPoint(
				start.X + (directionX * segmentEnd),
				start.Y + (directionY * segmentEnd));
			DrawThickLine(image, dashStart, dashEnd, color, thickness);
			current += dashLength + gapLength;
		}
	}

	private static void FillTriangle(
		Image<Rgba32> image,
		ImageEditorPoint point1,
		ImageEditorPoint point2,
		ImageEditorPoint point3,
		Rgba32 color)
	{
		var minX = (int)Math.Floor(Math.Min(point1.X, Math.Min(point2.X, point3.X)));
		var maxX = (int)Math.Ceiling(Math.Max(point1.X, Math.Max(point2.X, point3.X)));
		var minY = (int)Math.Floor(Math.Min(point1.Y, Math.Min(point2.Y, point3.Y)));
		var maxY = (int)Math.Ceiling(Math.Max(point1.Y, Math.Max(point2.Y, point3.Y)));

		for (var y = minY; y <= maxY; y++)
		{
			for (var x = minX; x <= maxX; x++)
			{
				if (IsPointInsideTriangle(x + 0.5d, y + 0.5d, point1, point2, point3))
				{
					BlendPixel(image, x, y, color);
				}
			}
		}
	}

	private static bool IsPointInsideTriangle(
		double x,
		double y,
		ImageEditorPoint point1,
		ImageEditorPoint point2,
		ImageEditorPoint point3)
	{
		var sign1 = Sign(x, y, point1, point2);
		var sign2 = Sign(x, y, point2, point3);
		var sign3 = Sign(x, y, point3, point1);
		var hasNegative = sign1 < 0d || sign2 < 0d || sign3 < 0d;
		var hasPositive = sign1 > 0d || sign2 > 0d || sign3 > 0d;

		return !(hasNegative && hasPositive);
	}

	private static double Sign(double x, double y, ImageEditorPoint point1, ImageEditorPoint point2) =>
		(x - point2.X) * (point1.Y - point2.Y) - (point1.X - point2.X) * (y - point2.Y);

	private static double DistanceToSegmentSquared(
		double x,
		double y,
		double startX,
		double startY,
		double endX,
		double endY)
	{
		var deltaX = endX - startX;
		var deltaY = endY - startY;
		if (Math.Abs(deltaX) < double.Epsilon && Math.Abs(deltaY) < double.Epsilon)
		{
			return Math.Pow(x - startX, 2d) + Math.Pow(y - startY, 2d);
		}

		var projection =
			((x - startX) * deltaX + (y - startY) * deltaY) /
			((deltaX * deltaX) + (deltaY * deltaY));
		var clampedProjection = Math.Clamp(projection, 0d, 1d);
		var closestX = startX + (clampedProjection * deltaX);
		var closestY = startY + (clampedProjection * deltaY);

		return Math.Pow(x - closestX, 2d) + Math.Pow(y - closestY, 2d);
	}

	private static void BlendPixel(Image<Rgba32> image, int x, int y, Rgba32 overlay)
	{
		if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
		{
			return;
		}

		var destination = image[x, y];
		image[x, y] = Blend(destination, overlay);
	}

	private static Rgba32 Blend(Rgba32 destination, Rgba32 overlay)
	{
		var sourceAlpha = overlay.A / 255f;
		var destinationAlpha = destination.A / 255f;
		var outputAlpha = sourceAlpha + (destinationAlpha * (1f - sourceAlpha));

		if (outputAlpha <= 0f)
		{
			return new Rgba32(0, 0, 0, 0);
		}

		var red = ((overlay.R * sourceAlpha) + (destination.R * destinationAlpha * (1f - sourceAlpha))) / outputAlpha;
		var green = ((overlay.G * sourceAlpha) + (destination.G * destinationAlpha * (1f - sourceAlpha))) / outputAlpha;
		var blue = ((overlay.B * sourceAlpha) + (destination.B * destinationAlpha * (1f - sourceAlpha))) / outputAlpha;

		return new Rgba32(
			(byte)Math.Clamp(Math.Round(red), 0d, 255d),
			(byte)Math.Clamp(Math.Round(green), 0d, 255d),
			(byte)Math.Clamp(Math.Round(blue), 0d, 255d),
			(byte)Math.Clamp(Math.Round(outputAlpha * 255f), 0d, 255d));
	}

	private static Rgba32 ToRgba32(ImageEditorColor color) => new(color.R, color.G, color.B, color.A);

	private static bool IsVisible(ImageEditorColor color) => color.A > 0;

	private static void DrawText(
		Image<Rgba32> image,
		ImageEditorRect bounds,
		string text,
		ImageEditorAnnotationStyle style)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var rect = NormalizeRect(bounds, image.Width, image.Height);
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		var fontSize = Math.Max(style.TextSize, 10f);
		var font = SystemFonts.CreateFont("Arial", fontSize, FontStyle.Regular);
		var textOptions = new RichTextOptions(font)
		{
			Origin = new PointF((float)(rect.X + (rect.Width / 2d)), (float)(rect.Y + (rect.Height / 2d))),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			WrappingLength = (float)Math.Max(rect.Width - 12d, 1d)
		};

		image.Mutate(context => context.DrawText(textOptions, text, Color.FromRgba(style.TextColor.R, style.TextColor.G, style.TextColor.B, style.TextColor.A)));
	}
}
