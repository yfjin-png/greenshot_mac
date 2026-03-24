using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Services;

public sealed class NativeImageEditorService : IImageEditorService
{
	public bool IsSupported => true;

	public Task<ImageEditorDocumentInfo> LoadAsync(
		string filePath,
		CancellationToken cancellationToken = default) =>
		ImageEditorArtifactService.IdentifyAsync(filePath, cancellationToken);

	public Task<string> SaveAnnotatedCopyAsync(
		string filePath,
		IReadOnlyCollection<ImageEditorAnnotation> annotations,
		CancellationToken cancellationToken = default) =>
		ImageEditorArtifactService.CreateAnnotatedCopyAsync(
			filePath,
			annotations,
			FileSystem.Current.CacheDirectory,
			cancellationToken);
}
