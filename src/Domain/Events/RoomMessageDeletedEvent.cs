namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a room message is soft-deleted (by its sender or the room host).
/// Handler: broadcast a MessageDeleted SignalR event to the room group.
/// </summary>
public class RoomMessageDeletedEvent : BaseEvent
{
    public RoomMessageDeletedEvent(int roomId, int messageId)
    {
        RoomId = roomId;
        MessageId = messageId;
    }

    public int RoomId { get; }

    public int MessageId { get; }
}
