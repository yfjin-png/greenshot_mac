using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Services;

public interface IImageEditorService
{
	bool IsSupported { get; }

	Task<ImageEditorDocumentInfo> LoadAsync(string filePath, CancellationToken cancellationToken = default);

	Task<string> SaveAnnotatedCopyAsync(
		string filePath,
		IReadOnlyCollection<ImageEditorAnnotation> annotations,
		CancellationToken cancellationToken = default);
}
