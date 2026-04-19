namespace LinguaSpace.Domain.Entities;

/// <summary>
/// User profile entity — display and social data, separate from ASP.NET Identity ApplicationUser.
/// Created automatically when ApplicationUser registers (via UserRegisteredEvent).
/// </summary>
public class UserProfile : BaseAuditableEntity
{
    /// <summary>FK to ApplicationUser.Id (string GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Timezone { get; set; }

    public bool IsOnline { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public ICollection<UserLanguage> Languages { get; set; } = new List<UserLanguage>();
}
