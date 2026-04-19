namespace LinguaSpace.Domain.Entities;

/// <summary>Junction table: which user earned which badge, and when.</summary>
public class UserBadge : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id.</summary>
    public string UserId { get; set; } = string.Empty;

    public int BadgeId { get; set; }

    public Badge Badge { get; set; } = null!;

    public DateTimeOffset EarnedAt { get; set; }
}
