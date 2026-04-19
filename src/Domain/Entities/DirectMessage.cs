namespace LinguaSpace.Domain.Entities;

public class DirectMessage : BaseEntity
{
    public int ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    /// <summary>FK to ApplicationUser.Id.</summary>
    public string SenderId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }

    public bool IsRead { get; set; }
}
