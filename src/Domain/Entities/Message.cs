namespace LinguaSpace.Domain.Entities;

public class Message : BaseEntity
{
    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    /// <summary>FK to ApplicationUser.Id.</summary>
    public string SenderId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }

    public MessageType Type { get; set; } = MessageType.Text;

    public bool IsDeleted { get; set; }
}
