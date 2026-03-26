using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public sealed class ImageEditorSessionTests
{
	[Fact]
	public void AddTextAnnotation_CreatesCenteredTextAnnotation()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/source.png", new ImageEditorDocumentInfo(1200, 800));

		var annotation = session.AddTextAnnotation("clipboard text");

		Assert.NotNull(annotation);
		Assert.Equal(ImageEditorTool.Text, annotation!.Tool);
		Assert.Equal("clipboard text", annotation.Text);
		Assert.Equal(annotation.Id, session.SelectedAnnotationId);
		Assert.Equal(ImageEditorTool.Text, session.ActiveTool);
		Assert.True(annotation.Bounds.Width > 0);
		Assert.True(annotation.Bounds.Height > 0);
	}

	[Fact]
	public void CompleteDraft_WithPencil_CreatesFreehandAnnotation()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/source.png", new ImageEditorDocumentInfo(1200, 800));
		session.SetTool(ImageEditorTool.Pencil);
		session.StartDraft(new ImageEditorPoint(20, 30));
		session.UpdateDraft(new ImageEditorPoint(28, 42));
		session.UpdateDraft(new ImageEditorPoint(44, 38));

		var annotation = session.CompleteDraft(new ImageEditorPoint(60, 54));

		Assert.NotNull(annotation);
		Assert.Equal(ImageEditorTool.Pencil, annotation!.Tool);
		Assert.NotNull(annotation.PathPoints);
		Assert.True(annotation.PathPoints!.Count >= 4);
		Assert.Equal(annotation.StartPoint, annotation.PathPoints[0]);
		Assert.Equal(annotation.EndPoint, annotation.PathPoints[^1]);
		Assert.Equal(annotation.Id, session.SelectedAnnotationId);
		Assert.Equal(ImageEditorTool.Pencil, session.ActiveTool);
		Assert.True(annotation.Length > 0);
	}

	[Fact]
	public void AddImageAnnotation_CreatesImageAnnotationWithAssetPath()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/source.png", new ImageEditorDocumentInfo(1000, 700));
		var imagePath = Path.GetTempFileName();
		File.WriteAllBytes(imagePath, [1, 2, 3]);

		try
		{
			var annotation = session.AddImageAnnotation(imagePath, new ImageEditorDocumentInfo(300, 200));

			Assert.NotNull(annotation);
			Assert.Equal(ImageEditorTool.Image, annotation!.Tool);
			Assert.Equal(imagePath, annotation.AssetPath);
			Assert.Equal(annotation.Id, session.SelectedAnnotationId);
			Assert.Equal(ImageEditorTool.Image, session.ActiveTool);
			Assert.True(annotation.Bounds.Width <= 600);
			Assert.True(annotation.Bounds.Height <= 420);
		}
		finally
		{
			File.Delete(imagePath);
		}
	}

	[Fact]
	public void ClearSelection_ResetsImageToolBackToRectangle()
	{
		var session = new ImageEditorSession();
		session.BeginEditing("/tmp/source.png", new ImageEditorDocumentInfo(1000, 700));
		var imagePath = Path.GetTempFileName();
		File.WriteAllBytes(imagePath, [1, 2, 3]);

		try
		{
			_ = session.AddImageAnnotation(imagePath, new ImageEditorDocumentInfo(120, 80));

			session.ClearSelection();

			Assert.Null(session.SelectedAnnotationId);
			Assert.Equal(ImageEditorTool.Rectangle, session.ActiveTool);
		}
		finally
		{
			File.Delete(imagePath);
		}
	}
}
