namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Gamification XP tracker — one record per user (1-1 with ApplicationUser).
/// </summary>
public class UserXp : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id.</summary>
    public string UserId { get; set; } = string.Empty;

    public int TotalXp { get; set; }

    public int CurrentStreak { get; set; }

    public int LongestStreak { get; set; }

    public DateTimeOffset? LastActivityAt { get; set; }
}
