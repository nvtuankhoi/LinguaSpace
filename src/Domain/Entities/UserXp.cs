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

    /// <summary>
    /// Updates streak fields based on today's UTC date.
    /// Returns true if this is new-day activity (streak changed or first activity).
    /// Call this before saving changes.
    /// </summary>
    public bool UpdateStreak()
    {
        DateOnly today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime);

        if (LastActivityAt is null)
        {
            CurrentStreak = 1;
            LongestStreak = 1;
            LastActivityAt = DateTimeOffset.UtcNow;
            return true;
        }

        DateOnly lastActivity = DateOnly.FromDateTime(LastActivityAt.Value.DateTime);

        if (today == lastActivity)
        {
            return false;
        }

        if (today == lastActivity.AddDays(1))
        {
            CurrentStreak++;
            if (CurrentStreak > LongestStreak)
            {
                LongestStreak = CurrentStreak;
            }
        }
        else
        {
            CurrentStreak = 1;
        }

        LastActivityAt = DateTimeOffset.UtcNow;
        return true;
    }
}
