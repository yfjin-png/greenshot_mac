using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public class ScreenshotWorkspaceSessionTests
{
	[Fact]
	public void BeginRegionSelection_TracksSourceImageAndDimensions()
	{
		var session = new ScreenshotWorkspaceSession();
		var captureResult = ScreenshotCaptureResult.Success("/tmp/fullscreen.png", "Captured.", 1600, 900);

		session.BeginRegionSelection(captureResult);

		Assert.True(session.IsSelectingRegion);
		Assert.False(session.IsDraggingSelection);
		Assert.Equal("/tmp/fullscreen.png", session.SelectedImagePath);
		Assert.Equal("/tmp/fullscreen.png", session.SelectionSourcePath);
		Assert.Equal(1600, session.SelectionSourcePixelWidth);
		Assert.Equal(900, session.SelectionSourcePixelHeight);
	}

	[Fact]
	public void CancelRegionSelection_RestoresSourceImageAndClearsSelectionState()
	{
		var session = new ScreenshotWorkspaceSession();
		session.BeginRegionSelection(ScreenshotCaptureResult.Success("/tmp/fullscreen.png", "Captured.", 1600, 900));
		session.StartSelection(new SelectionCanvasPoint(40d, 50d));
		session.UpdateSelection(new SelectionCanvasPoint(300d, 250d));

		var restoredPath = session.CancelRegionSelection();

		Assert.Equal("/tmp/fullscreen.png", restoredPath);
		Assert.False(session.IsSelectingRegion);
		Assert.False(session.IsDraggingSelection);
		Assert.Equal("/tmp/fullscreen.png", session.SelectedImagePath);
		Assert.Null(session.SelectionSourcePath);
		Assert.Null(session.SelectionStartPoint);
		Assert.Null(session.SelectionCurrentPoint);
	}

	[Fact]
	public void CompleteRegionSelection_ReplacesSelectedImageWithCroppedArtifact()
	{
		var session = new ScreenshotWorkspaceSession();
		session.BeginRegionSelection(ScreenshotCaptureResult.Success("/tmp/fullscreen.png", "Captured.", 1600, 900));

		session.CompleteRegionSelection("/tmp/region.png");

		Assert.False(session.IsSelectingRegion);
		Assert.Equal("/tmp/region.png", session.SelectedImagePath);
		Assert.Null(session.SelectionSourcePath);
		Assert.Equal(0, session.SelectionSourcePixelWidth);
		Assert.Equal(0, session.SelectionSourcePixelHeight);
	}

	[Fact]
	public void TryProjectSelection_UsesTrackedSelectionState()
	{
		var session = new ScreenshotWorkspaceSession();
		session.BeginRegionSelection(ScreenshotCaptureResult.Success("/tmp/fullscreen.png", "Captured.", 1600, 800));
		session.StartSelection(new SelectionCanvasPoint(100d, 150d));
		session.UpdateSelection(new SelectionCanvasPoint(500d, 350d));
		session.EndSelection();

		var projected = session.TryProjectSelection(
			stageWidth: 800d,
			stageHeight: 600d,
			minimumSelectionDisplaySize: 8d,
			out var projection);

		Assert.True(projected);
		Assert.Equal(new ScreenshotCropBounds(200, 100, 800, 400), projection.CropBounds);
	}

	[Fact]
	public void ClearImage_ResetsSelectionAndSelectedImage()
	{
		var session = new ScreenshotWorkspaceSession();
		session.BeginRegionSelection(ScreenshotCaptureResult.Success("/tmp/fullscreen.png", "Captured.", 1600, 900));

		session.ClearImage();

		Assert.Null(session.SelectedImagePath);
		Assert.False(session.IsSelectingRegion);
		Assert.Null(session.SelectionSourcePath);
	}

	[Fact]
	public void LoadImage_AppendsToHistoryAndTracksCurrentPosition()
	{
		var session = new ScreenshotWorkspaceSession();

		session.LoadImage("/tmp/first.png");
		session.LoadImage("/tmp/second.png");

		Assert.Equal(["/tmp/first.png", "/tmp/second.png"], session.ImageHistory);
		Assert.Equal("/tmp/second.png", session.SelectedImagePath);
		Assert.Equal(2, session.SelectedImageHistoryNumber);
		Assert.True(session.CanSelectPreviousImage);
		Assert.False(session.CanSelectNextImage);
	}

	[Fact]
	public void SelectPreviousImage_MovesBackwardThroughHistory()
	{
		var session = new ScreenshotWorkspaceSession();
		session.LoadImage("/tmp/first.png");
		session.LoadImage("/tmp/second.png");
		session.LoadImage("/tmp/third.png");

		var moved = session.SelectPreviousImage();

		Assert.True(moved);
		Assert.Equal("/tmp/second.png", session.SelectedImagePath);
		Assert.Equal(2, session.SelectedImageHistoryNumber);
		Assert.True(session.CanSelectPreviousImage);
		Assert.True(session.CanSelectNextImage);
	}

	[Fact]
	public void SelectNextImage_MovesForwardAfterGoingBack()
	{
		var session = new ScreenshotWorkspaceSession();
		session.LoadImage("/tmp/first.png");
		session.LoadImage("/tmp/second.png");
		session.SelectPreviousImage();

		var moved = session.SelectNextImage();

		Assert.True(moved);
		Assert.Equal("/tmp/second.png", session.SelectedImagePath);
		Assert.Equal(2, session.SelectedImageHistoryNumber);
		Assert.True(session.CanSelectPreviousImage);
		Assert.False(session.CanSelectNextImage);
	}

	[Fact]
	public void LoadImage_AfterGoingBack_ReplacesForwardHistory()
	{
		var session = new ScreenshotWorkspaceSession();
		session.LoadImage("/tmp/first.png");
		session.LoadImage("/tmp/second.png");
		session.LoadImage("/tmp/third.png");
		session.SelectPreviousImage();

		session.LoadImage("/tmp/fourth.png");

		Assert.Equal(["/tmp/first.png", "/tmp/second.png", "/tmp/fourth.png"], session.ImageHistory);
		Assert.Equal("/tmp/fourth.png", session.SelectedImagePath);
		Assert.False(session.CanSelectNextImage);
	}
}
