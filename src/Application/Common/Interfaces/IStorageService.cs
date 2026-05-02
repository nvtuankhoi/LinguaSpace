namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// File/blob storage abstraction. Implemented by LocalStorageService (dev) or AzureBlobStorageService (prod).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads an avatar image and returns its public URL.
    /// </summary>
    Task<string> UploadAvatarAsync(string userId, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by its URL/path.
    /// </summary>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}
