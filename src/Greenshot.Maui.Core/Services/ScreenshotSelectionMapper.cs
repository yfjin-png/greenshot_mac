namespace Greenshot.Maui.Core.Services;

public readonly record struct SelectionCanvasPoint(double X, double Y);

public readonly record struct SelectionCanvasRect(double X, double Y, double Width, double Height)
{
	public bool IsEmpty => Width <= 0d || Height <= 0d;

	public double Right => X + Width;

	public double Bottom => Y + Height;
}

public readonly record struct ScreenshotSelectionProjection(
	SelectionCanvasRect SelectionDisplayRect,
	ScreenshotCropBounds CropBounds);

public static class ScreenshotSelectionMapper
{
	public static SelectionCanvasRect GetDisplayedImageRect(
		double stageWidth,
		double stageHeight,
		int imagePixelWidth,
		int imagePixelHeight)
	{
		if (stageWidth <= 0d || stageHeight <= 0d || imagePixelWidth <= 0 || imagePixelHeight <= 0)
		{
			return default;
		}

		var imageAspect = (double)imagePixelWidth / imagePixelHeight;
		var stageAspect = stageWidth / stageHeight;

		if (imageAspect >= stageAspect)
		{
			var displayHeight = stageWidth / imageAspect;
			return new SelectionCanvasRect(0d, (stageHeight - displayHeight) / 2d, stageWidth, displayHeight);
		}

		var displayWidth = stageHeight * imageAspect;
		return new SelectionCanvasRect((stageWidth - displayWidth) / 2d, 0d, displayWidth, stageHeight);
	}

	public static bool TryProjectSelection(
		SelectionCanvasPoint? startPoint,
		SelectionCanvasPoint? currentPoint,
		double stageWidth,
		double stageHeight,
		int imagePixelWidth,
		int imagePixelHeight,
		double minimumSelectionDisplaySize,
		out ScreenshotSelectionProjection projection)
	{
		projection = default;

		if (!startPoint.HasValue || !currentPoint.HasValue)
		{
			return false;
		}

		var imageRect = GetDisplayedImageRect(stageWidth, stageHeight, imagePixelWidth, imagePixelHeight);
		if (imageRect.IsEmpty)
		{
			return false;
		}

		var clampedStart = ClampPointToImageRect(startPoint.Value, imageRect);
		var clampedCurrent = ClampPointToImageRect(currentPoint.Value, imageRect);
		var left = Math.Min(clampedStart.X, clampedCurrent.X);
		var top = Math.Min(clampedStart.Y, clampedCurrent.Y);
		var width = Math.Abs(clampedCurrent.X - clampedStart.X);
		var height = Math.Abs(clampedCurrent.Y - clampedStart.Y);
		var minimumSize = Math.Max(0d, minimumSelectionDisplaySize);

		if (width < minimumSize || height < minimumSize)
		{
			return false;
		}

		var selectionRect = new SelectionCanvasRect(left, top, width, height);
		var normalizedLeft = (selectionRect.X - imageRect.X) / imageRect.Width;
		var normalizedTop = (selectionRect.Y - imageRect.Y) / imageRect.Height;
		var normalizedRight = (selectionRect.Right - imageRect.X) / imageRect.Width;
		var normalizedBottom = (selectionRect.Bottom - imageRect.Y) / imageRect.Height;
		var pixelLeft = Math.Clamp((int)Math.Floor(normalizedLeft * imagePixelWidth), 0, imagePixelWidth);
		var pixelTop = Math.Clamp((int)Math.Floor(normalizedTop * imagePixelHeight), 0, imagePixelHeight);
		var pixelRight = Math.Clamp((int)Math.Ceiling(normalizedRight * imagePixelWidth), 0, imagePixelWidth);
		var pixelBottom = Math.Clamp((int)Math.Ceiling(normalizedBottom * imagePixelHeight), 0, imagePixelHeight);
		var pixelWidth = pixelRight - pixelLeft;
		var pixelHeight = pixelBottom - pixelTop;

		if (pixelWidth <= 0 || pixelHeight <= 0)
		{
			return false;
		}

		projection = new ScreenshotSelectionProjection(
			selectionRect,
			new ScreenshotCropBounds(pixelLeft, pixelTop, pixelWidth, pixelHeight));
		return true;
	}

	private static SelectionCanvasPoint ClampPointToImageRect(SelectionCanvasPoint point, SelectionCanvasRect imageRect)
	{
		var maxX = imageRect.Right;
		var maxY = imageRect.Bottom;
		return new SelectionCanvasPoint(
			Math.Clamp(point.X, imageRect.X, maxX),
			Math.Clamp(point.Y, imageRect.Y, maxY));
	}
}
