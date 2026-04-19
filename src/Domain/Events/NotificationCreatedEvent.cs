namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a Notification entity is created.
/// Handler: push real-time notification via SignalR to the recipient.
/// </summary>
public class NotificationCreatedEvent : BaseEvent
{
    public NotificationCreatedEvent(int notificationId, string recipientId)
    {
        NotificationId = notificationId;
        RecipientId = recipientId;
    }

    public int NotificationId { get; }

    public string RecipientId { get; }
}
