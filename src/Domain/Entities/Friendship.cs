namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Friend relationship between two users.
/// RequesterId initiates, AddresseeId responds.
/// </summary>
public class Friendship : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id — the user who sent the request.</summary>
    public string RequesterId { get; set; } = string.Empty;

    /// <summary>FK to ApplicationUser.Id — the user who received the request.</summary>
    public string AddresseeId { get; set; } = string.Empty;

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
