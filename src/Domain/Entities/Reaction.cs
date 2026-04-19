namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Emoji reaction on a Post or Comment.
/// UNIQUE constraint: one reaction type per user per target.
/// </summary>
public class Reaction : BaseEntity
{
    public int TargetId { get; set; }

    public ReactionTargetType TargetType { get; set; }

    /// <summary>FK to ApplicationUser.Id.</summary>
    public string UserId { get; set; } = string.Empty;

    public ReactionType Type { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
