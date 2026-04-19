namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Follow relationship (asymmetric, unlike Friendship).
/// UNIQUE: one FollowerId can only follow a given FolloweeId once.
/// </summary>
public class Follow : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id — the user who is following.</summary>
    public string FollowerId { get; set; } = string.Empty;

    /// <summary>FK to ApplicationUser.Id — the user being followed.</summary>
    public string FolloweeId { get; set; } = string.Empty;

    public DateTimeOffset FollowedAt { get; set; }
}
