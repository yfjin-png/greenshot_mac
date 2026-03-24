namespace Greenshot.Maui.Core.Services;

public enum ImageEditorTool
{
	Rectangle,
	Arrow,
	Highlight,
	Line,
	Text,
	SpeechBubble
}

public enum ImageEditorStrokeStyle
{
	Solid,
	Dashed
}

public readonly record struct ImageEditorPoint(double X, double Y);

public readonly record struct ImageEditorRect(double X, double Y, double Width, double Height)
{
	public double Right => X + Width;

	public double Bottom => Y + Height;

	public static ImageEditorRect FromPoints(ImageEditorPoint startPoint, ImageEditorPoint endPoint)
	{
		var left = Math.Min(startPoint.X, endPoint.X);
		var top = Math.Min(startPoint.Y, endPoint.Y);
		var right = Math.Max(startPoint.X, endPoint.X);
		var bottom = Math.Max(startPoint.Y, endPoint.Y);

		return new ImageEditorRect(left, top, right - left, bottom - top);
	}
}

public readonly record struct ImageEditorColor(byte R, byte G, byte B, byte A = byte.MaxValue);

public enum ImageEditorSelectionHandle
{
	Body,
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight,
	StartPoint,
	EndPoint
}

public readonly record struct ImageEditorHitTarget(Guid AnnotationId, ImageEditorSelectionHandle Handle);

public readonly record struct ImageEditorAnnotationStyle(
	ImageEditorColor StrokeColor,
	float StrokeThickness,
	ImageEditorStrokeStyle StrokeStyle,
	ImageEditorColor FillColor,
	ImageEditorColor TextColor,
	float TextSize);

public readonly record struct ImageEditorToolPreset(
	ImageEditorAnnotationStyle Style,
	string Text);

public static class ImageEditorToolDefaults
{
	public static ImageEditorToolPreset Get(ImageEditorTool tool) => tool switch
	{
		ImageEditorTool.Rectangle => new(
			new ImageEditorAnnotationStyle(
				new ImageEditorColor(220, 53, 69),
				4f,
				ImageEditorStrokeStyle.Solid,
				new ImageEditorColor(0, 0, 0, 0),
				new ImageEditorColor(220, 53, 69),
				20f),
			string.Empty),
		ImageEditorTool.Arrow => new(
			new ImageEditorAnnotationStyle(
				new ImageEditorColor(220, 53, 69),
				6f,
				ImageEditorStrokeStyle.Solid,
				new ImageEditorColor(0, 0, 0, 0),
				new ImageEditorColor(220, 53, 69),
				20f),
			string.Empty),
		ImageEditorTool.Highlight => new(
			new ImageEditorAnnotationStyle(
				new ImageEditorColor(173, 130, 0, 220),
				2f,
				ImageEditorStrokeStyle.Solid,
				new ImageEditorColor(255, 230, 91, 96),
				new ImageEditorColor(173, 130, 0, 220),
				20f),
			string.Empty),
		ImageEditorTool.Line => new(
			new ImageEditorAnnotationStyle(
				new ImageEditorColor(21, 135, 92),
				4f,
				ImageEditorStrokeStyle.Solid,
				new ImageEditorColor(0, 0, 0, 0),
				new ImageEditorColor(21, 135, 92),
				20f),
			string.Empty),
		ImageEditorTool.Text => new(
			new ImageEditorAnnotationStyle(
				new ImageEditorColor(21, 135, 92),
				2f,
				ImageEditorStrokeStyle.Solid,
				new ImageEditorColor(255, 255, 255, 0),
				new ImageEditorColor(21, 135, 92),
				24f),
			"Text"),
		ImageEditorTool.SpeechBubble => new(
			new ImageEditorAnnotationStyle(
				new ImageEditorColor(41, 107, 163),
				2f,
				ImageEditorStrokeStyle.Solid,
				new ImageEditorColor(255, 255, 255, 255),
				new ImageEditorColor(41, 107, 163),
				22f),
			"Callout"),
		_ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unsupported image editor tool.")
	};
}

public sealed record ImageEditorAnnotation(
	Guid Id,
	ImageEditorTool Tool,
	ImageEditorPoint StartPoint,
	ImageEditorPoint EndPoint,
	ImageEditorAnnotationStyle Style,
	string Text)
{
	public ImageEditorRect Bounds => ImageEditorRect.FromPoints(StartPoint, EndPoint);

	public double Length =>
		Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2d) + Math.Pow(EndPoint.Y - StartPoint.Y, 2d));

	public bool IsMeaningful(double minimumPixels = 6d)
	{
		return Tool switch
		{
			ImageEditorTool.Arrow or ImageEditorTool.Line => Length >= minimumPixels,
			_ => Bounds.Width >= minimumPixels && Bounds.Height >= minimumPixels
		};
	}

	public ImageEditorAnnotation MoveBy(double deltaX, double deltaY) =>
		this with
		{
			StartPoint = new ImageEditorPoint(StartPoint.X + deltaX, StartPoint.Y + deltaY),
			EndPoint = new ImageEditorPoint(EndPoint.X + deltaX, EndPoint.Y + deltaY)
		};

	public ImageEditorAnnotation WithStart(ImageEditorPoint point) =>
		this with
		{
			StartPoint = point
		};

	public ImageEditorAnnotation WithEnd(ImageEditorPoint point) =>
		this with
		{
			EndPoint = point
		};

	public ImageEditorAnnotation WithBounds(ImageEditorRect bounds) =>
		this with
		{
			StartPoint = new ImageEditorPoint(bounds.X, bounds.Y),
			EndPoint = new ImageEditorPoint(bounds.Right, bounds.Bottom)
		};

	public bool TryHitTest(ImageEditorPoint point, double tolerance, out ImageEditorSelectionHandle handle)
	{
		switch (Tool)
		{
			case ImageEditorTool.Rectangle:
			case ImageEditorTool.Highlight:
			case ImageEditorTool.Text:
			case ImageEditorTool.SpeechBubble:
				var bounds = Bounds;
				if (IsPointNear(point, new ImageEditorPoint(bounds.X, bounds.Y), tolerance))
				{
					handle = ImageEditorSelectionHandle.TopLeft;
					return true;
				}

				if (IsPointNear(point, new ImageEditorPoint(bounds.Right, bounds.Y), tolerance))
				{
					handle = ImageEditorSelectionHandle.TopRight;
					return true;
				}

				if (IsPointNear(point, new ImageEditorPoint(bounds.X, bounds.Bottom), tolerance))
				{
					handle = ImageEditorSelectionHandle.BottomLeft;
					return true;
				}

				if (IsPointNear(point, new ImageEditorPoint(bounds.Right, bounds.Bottom), tolerance))
				{
					handle = ImageEditorSelectionHandle.BottomRight;
					return true;
				}

				if (IsPointInsideExpandedRect(point, bounds, tolerance))
				{
					handle = ImageEditorSelectionHandle.Body;
					return true;
				}

				break;
			case ImageEditorTool.Arrow:
			case ImageEditorTool.Line:
				if (IsPointNear(point, StartPoint, tolerance))
				{
					handle = ImageEditorSelectionHandle.StartPoint;
					return true;
				}

				if (IsPointNear(point, EndPoint, tolerance))
				{
					handle = ImageEditorSelectionHandle.EndPoint;
					return true;
				}

				if (DistanceToSegment(point, StartPoint, EndPoint) <= tolerance)
				{
					handle = ImageEditorSelectionHandle.Body;
					return true;
				}

				break;
		}

		handle = default;
		return false;
	}

	private static bool IsPointInsideExpandedRect(ImageEditorPoint point, ImageEditorRect bounds, double tolerance) =>
		point.X >= bounds.X - tolerance &&
		point.X <= bounds.Right + tolerance &&
		point.Y >= bounds.Y - tolerance &&
		point.Y <= bounds.Bottom + tolerance;

	private static bool IsPointNear(ImageEditorPoint point, ImageEditorPoint target, double tolerance)
	{
		var deltaX = point.X - target.X;
		var deltaY = point.Y - target.Y;
		return (deltaX * deltaX) + (deltaY * deltaY) <= tolerance * tolerance;
	}

	private static double DistanceToSegment(ImageEditorPoint point, ImageEditorPoint start, ImageEditorPoint end)
	{
		var deltaX = end.X - start.X;
		var deltaY = end.Y - start.Y;
		if (Math.Abs(deltaX) < double.Epsilon && Math.Abs(deltaY) < double.Epsilon)
		{
			return Math.Sqrt(Math.Pow(point.X - start.X, 2d) + Math.Pow(point.Y - start.Y, 2d));
		}

		var projection =
			((point.X - start.X) * deltaX + (point.Y - start.Y) * deltaY) /
			((deltaX * deltaX) + (deltaY * deltaY));
		var clampedProjection = Math.Clamp(projection, 0d, 1d);
		var closestX = start.X + (clampedProjection * deltaX);
		var closestY = start.Y + (clampedProjection * deltaY);

		return Math.Sqrt(Math.Pow(point.X - closestX, 2d) + Math.Pow(point.Y - closestY, 2d));
	}
}

public readonly record struct ImageEditorDocumentInfo(int PixelWidth, int PixelHeight)
{
	public bool IsValid => PixelWidth > 0 && PixelHeight > 0;
}
