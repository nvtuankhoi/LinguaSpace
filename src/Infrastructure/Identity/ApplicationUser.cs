using Microsoft.AspNetCore.Identity;

namespace LinguaSpace.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    /// <summary>SHA-256 hash of the latest refresh token. Null when logged out.</summary>
    public string? RefreshTokenHash { get; set; }

    /// <summary>Expiry of the refresh token. Null when logged out.</summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
}
