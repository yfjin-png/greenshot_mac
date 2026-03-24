using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public sealed class ImageEditorSessionTests
{
	[Fact]
	public void BeginEditing_ResetsStateAndSeedsDefaults()
	{
		var session = new ImageEditorSession();

		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(640, 480));

		Assert.True(session.IsEditing);
		Assert.Equal(ImageEditorTool.Rectangle, session.ActiveTool);
		Assert.Empty(session.Annotations);
		Assert.Equal(640, session.DocumentInfo.PixelWidth);
		Assert.Equal(480, session.DocumentInfo.PixelHeight);
	}

	[Fact]
	public void CompleteDraft_AddsAnnotationWhenMinimumSizeIsMet()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));

		session.StartDraft(new ImageEditorPoint(10, 20));
		session.UpdateDraft(new ImageEditorPoint(80, 120));

		var annotation = session.CompleteDraft();

		Assert.NotNull(annotation);
		Assert.Single(session.Annotations);
		Assert.False(session.IsDrawingDraft);
		Assert.Equal(ImageEditorTool.Rectangle, annotation!.Tool);
		Assert.Equal(70d, annotation.Bounds.Width);
		Assert.Equal(100d, annotation.Bounds.Height);
	}

	[Fact]
	public void CompleteDraft_IgnoresTinyRectangleAnnotations()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));

		session.StartDraft(new ImageEditorPoint(10, 20));
		session.UpdateDraft(new ImageEditorPoint(14, 24));

		var annotation = session.CompleteDraft();

		Assert.Null(annotation);
		Assert.Empty(session.Annotations);
	}

	[Fact]
	public void UndoLastAnnotation_RemovesNewestAnnotation()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));

		session.StartDraft(new ImageEditorPoint(10, 20));
		session.UpdateDraft(new ImageEditorPoint(80, 120));
		session.CompleteDraft();
		session.SetTool(ImageEditorTool.Arrow);
		session.StartDraft(new ImageEditorPoint(100, 120));
		session.UpdateDraft(new ImageEditorPoint(170, 180));
		session.CompleteDraft();

		var removed = session.UndoLastAnnotation();

		Assert.True(removed);
		Assert.Single(session.Annotations);
		Assert.Equal(ImageEditorTool.Rectangle, session.Annotations[0].Tool);
	}

	[Fact]
	public void TryBeginInteraction_SelectsExistingRectangleAndMovesIt()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));
		session.StartDraft(new ImageEditorPoint(10, 20));
		session.UpdateDraft(new ImageEditorPoint(80, 120));
		var annotation = session.CompleteDraft();

		var started = session.TryBeginInteraction(new ImageEditorPoint(40, 60));
		var updated = session.UpdateInteraction(new ImageEditorPoint(60, 80));
		var completed = session.CompleteInteraction(new ImageEditorPoint(60, 80));

		Assert.True(started);
		Assert.True(updated);
		Assert.True(completed);
		Assert.Equal(annotation!.Id, session.SelectedAnnotationId);
		Assert.Equal(30d, session.SelectedAnnotation!.Bounds.X);
		Assert.Equal(40d, session.SelectedAnnotation.Bounds.Y);
	}

	[Fact]
	public void TryBeginInteraction_ResizesRectangleFromBottomRightHandle()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));
		session.StartDraft(new ImageEditorPoint(10, 20));
		session.UpdateDraft(new ImageEditorPoint(80, 120));
		session.CompleteDraft();

		var started = session.TryBeginInteraction(new ImageEditorPoint(80, 120));
		session.UpdateInteraction(new ImageEditorPoint(110, 150));
		var completed = session.CompleteInteraction(new ImageEditorPoint(110, 150));

		Assert.True(started);
		Assert.True(completed);
		Assert.Equal(100d, session.SelectedAnnotation!.Bounds.Width);
		Assert.Equal(130d, session.SelectedAnnotation.Bounds.Height);
	}

	[Fact]
	public void TryBeginInteraction_UpdatesArrowEndpoint()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));
		session.SetTool(ImageEditorTool.Arrow);
		session.StartDraft(new ImageEditorPoint(100, 120));
		session.UpdateDraft(new ImageEditorPoint(170, 180));
		session.CompleteDraft();

		var started = session.TryBeginInteraction(new ImageEditorPoint(170, 180));
		session.UpdateInteraction(new ImageEditorPoint(220, 210));
		var completed = session.CompleteInteraction(new ImageEditorPoint(220, 210));

		Assert.True(started);
		Assert.True(completed);
		Assert.Equal(220d, session.SelectedAnnotation!.EndPoint.X);
		Assert.Equal(210d, session.SelectedAnnotation.EndPoint.Y);
	}

	[Fact]
	public void TryBeginInteraction_OnMissClearsSelection()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));
		session.StartDraft(new ImageEditorPoint(10, 20));
		session.UpdateDraft(new ImageEditorPoint(80, 120));
		session.CompleteDraft();
		session.TryBeginInteraction(new ImageEditorPoint(40, 60));

		var started = session.TryBeginInteraction(new ImageEditorPoint(300, 250));

		Assert.False(started);
		Assert.Null(session.SelectedAnnotationId);
	}

	[Fact]
	public void StyleAndTextUpdates_ApplyToSelectedTextAnnotation()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/sample.png", new ImageEditorDocumentInfo(400, 300));
		session.SetTool(ImageEditorTool.Text);
		session.StartDraft(new ImageEditorPoint(40, 50));
		session.UpdateDraft(new ImageEditorPoint(220, 140));
		session.CompleteDraft();

		session.SetText("Hello Greenshot");
		session.SetStrokeColor(new ImageEditorColor(41, 107, 163));
		session.SetFillColor(new ImageEditorColor(255, 255, 255, 255));
		session.SetTextColor(new ImageEditorColor(34, 34, 34));
		session.SetStrokeThickness(5f);
		session.SetStrokeStyle(ImageEditorStrokeStyle.Dashed);
		session.SetTextSize(30f);

		var annotation = Assert.Single(session.Annotations);
		Assert.Equal(ImageEditorTool.Text, annotation.Tool);
		Assert.Equal("Hello Greenshot", annotation.Text);
		Assert.Equal(new ImageEditorColor(41, 107, 163), annotation.Style.StrokeColor);
		Assert.Equal(new ImageEditorColor(255, 255, 255, 255), annotation.Style.FillColor);
		Assert.Equal(new ImageEditorColor(34, 34, 34), annotation.Style.TextColor);
		Assert.Equal(5f, annotation.Style.StrokeThickness);
		Assert.Equal(ImageEditorStrokeStyle.Dashed, annotation.Style.StrokeStyle);
		Assert.Equal(30f, annotation.Style.TextSize);
	}
}
