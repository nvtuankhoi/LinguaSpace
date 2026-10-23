using LinguaSpace.Application.Common;

namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// File/blob storage abstraction. Implemented by LocalStorageService (dev) or AzureBlobStorageService (prod).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Stores <paramref name="content"/> under the given <paramref name="category"/> and
    /// <paramref name="userId"/>, returning the public URL and the content type.
    /// <paramref name="category"/> is a top-level bucket, e.g. "posts" or "avatars".
    /// </summary>
    Task<UploadedFile> UploadAsync(
        string category,
        string userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by its public URL. Best-effort: a missing file is not an error.
    /// </summary>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}
