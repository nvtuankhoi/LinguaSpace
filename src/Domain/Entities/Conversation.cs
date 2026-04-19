namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Represents a direct message thread between exactly two users.
/// User1Id is always less than User2Id (enforced at service layer) to guarantee uniqueness.
/// </summary>
public class Conversation : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id. Always stored as the lexicographically smaller Id.</summary>
    public string User1Id { get; set; } = string.Empty;

    /// <summary>FK to ApplicationUser.Id. Always stored as the lexicographically larger Id.</summary>
    public string User2Id { get; set; } = string.Empty;

    public DateTimeOffset? LastMessageAt { get; set; }

    public int UnreadCountUser1 { get; set; }

    public int UnreadCountUser2 { get; set; }

    public ICollection<DirectMessage> Messages { get; set; } = new List<DirectMessage>();
}
