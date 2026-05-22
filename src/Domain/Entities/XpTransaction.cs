namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Records a single XP award event for audit and history display.
/// </summary>
public class XpTransaction : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id.</summary>
    public string UserId { get; set; } = string.Empty;

    public int Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset EarnedAt { get; set; }
}
