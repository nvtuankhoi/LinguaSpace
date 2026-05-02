using LinguaSpace.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LinguaSpace.Infrastructure.Storage;

/// <summary>
/// Development stub for IStorageService — returns placeholder URLs instead of uploading.
/// Replace with AzureBlobStorageService in production (Phase 2).
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(ILogger<LocalStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadAvatarAsync(
        string userId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Return a placeholder DiceBear avatar based on userId so the UI shows something useful.
        string placeholderUrl = $"https://api.dicebear.com/7.x/initials/svg?seed={userId}";

        _logger.LogInformation(
            "[STORAGE STUB] Avatar upload for user {UserId} → returning placeholder: {Url}",
            userId,
            placeholderUrl);

        return Task.FromResult(placeholderUrl);
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[STORAGE STUB] Delete file: {Url}", fileUrl);

        return Task.CompletedTask;
    }
}
