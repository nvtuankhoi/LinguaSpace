namespace LinguaSpace.Domain.Entities;

/// <summary>
/// In-app notification for a user.
/// Payload stores typed JSON data (e.g., sender info, post excerpt) for rich display.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id — recipient of the notification.</summary>
    public string RecipientId { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    /// <summary>JSON payload with context data (e.g., { "senderId": "...", "postId": 42 }).</summary>
    public string? Payload { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
