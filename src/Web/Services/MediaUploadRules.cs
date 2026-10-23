using Microsoft.AspNetCore.Http;

namespace LinguaSpace.Web.Services;

/// <summary>
/// Validation rules for uploaded media (images + short videos).
/// Shared by the post-media and avatar upload endpoints.
/// </summary>
public static class MediaUploadRules
{
    public const long MaxImageBytes = 5_000_000;
    public const long MaxVideoBytes = 50_000_000;

    public static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif",
    };

    public static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm",
    };

    /// <summary>Validates an image-or-video file. Returns an error message, or null when valid.</summary>
    public static string? ValidateMedia(IFormFile file)
    {
        string ext = Path.GetExtension(file.FileName);

        if (ImageExtensions.Contains(ext))
        {
            return file.Length > MaxImageBytes
                ? $"Image '{file.FileName}' exceeds the 5 MB limit."
                : null;
        }

        if (VideoExtensions.Contains(ext))
        {
            return file.Length > MaxVideoBytes
                ? $"Video '{file.FileName}' exceeds the 50 MB limit."
                : null;
        }

        return $"'{file.FileName}' has an unsupported file type. Allowed: jpg, jpeg, png, webp, gif, mp4, webm.";
    }

    /// <summary>Validates an avatar (images only). Returns an error message, or null when valid.</summary>
    public static string? ValidateAvatar(IFormFile file)
    {
        string ext = Path.GetExtension(file.FileName);

        if (!ImageExtensions.Contains(ext))
        {
            return "Avatar must be an image (jpg, jpeg, png, webp, or gif).";
        }

        return file.Length > MaxImageBytes ? "Avatar exceeds the 5 MB limit." : null;
    }
}
