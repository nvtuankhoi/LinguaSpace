using System.Text.RegularExpressions;
using LinguaSpace.Application.Common;
using LinguaSpace.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LinguaSpace.Infrastructure.Storage;

/// <summary>
/// Dev/local implementation of <see cref="IStorageService"/>: persists files under
/// <c>wwwroot/uploads/{category}/{userId}/</c> and serves them via the app's static-file
/// middleware (<c>app.UseFileServer()</c>). The public URL origin is taken from
/// <c>Storage:PublicBaseUrl</c> (default <c>https://localhost:7241</c>).
/// Replace with AzureBlobStorageService in production (Phase 2) — the DI registration is unchanged.
/// </summary>
public class LocalStorageService : IStorageService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase) { "posts", "avatars" };

    // Defends Path.Combine against path traversal — ASP.NET Identity ids are GUIDs, but the
    // contract doesn't guarantee it, so only allow safe path components.
    private static readonly Regex SafeUserId = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    private readonly IWebHostEnvironment _env;
    private readonly string _publicBaseUrl;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(
        IWebHostEnvironment env,
        IConfiguration configuration,
        ILogger<LocalStorageService> logger)
    {
        _env = env;
        _publicBaseUrl = (configuration["Storage:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
        _logger = logger;
    }

    public async Task<UploadedFile> UploadAsync(
        string category,
        string userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedCategories.Contains(category))
        {
            throw new ArgumentException($"Unknown upload category: '{category}'.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(userId) || !SafeUserId.IsMatch(userId))
        {
            throw new ArgumentException("Invalid userId for storage path.", nameof(userId));
        }

        // GUID filename keeps the original extension (for client-side img/video inference);
        // the client's filename is never used as a path segment.
        string storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        string directory = Path.Combine(_env.WebRootPath, "uploads", category, userId);
        Directory.CreateDirectory(directory);

        string fullPath = Path.Combine(directory, storedFileName);
        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(stream, cancellationToken);
        }

        string url = $"{_publicBaseUrl}/uploads/{category}/{userId}/{storedFileName}";
        _logger.LogInformation(
            "[STORAGE] Uploaded {Category}/{UserId}/{File} ({ContentType}, {Bytes} bytes)",
            category, userId, storedFileName, contentType, content.Length);

        return new UploadedFile(url, contentType);
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        // Map the public URL back to a wwwroot path by locating the "/uploads/" segment.
        int index = fileUrl.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return Task.CompletedTask;
        }

        string relative = fileUrl[(index + 1)..]; // "uploads/..."
        string[] segments = relative.Split('/');
        string fullPath = Path.Combine(_env.WebRootPath, Path.Combine(segments));

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("[STORAGE] Deleted {Path}", fullPath);
            }
        }
        catch (IOException ex)
        {
            // Best-effort: a failed local delete must not break the calling operation.
            _logger.LogWarning(ex, "[STORAGE] Failed to delete {Path}", fullPath);
        }

        return Task.CompletedTask;
    }
}
