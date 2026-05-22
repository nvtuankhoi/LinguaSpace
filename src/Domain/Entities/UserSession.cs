namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Represents an active login session (device) for a user.
/// </summary>
public class UserSession : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Short description of the client (e.g., "Chrome on Windows 11").</summary>
    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>The refresh token associated with this session (hashed).</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastActiveAt { get; set; }

    public bool IsRevoked { get; set; }
}
