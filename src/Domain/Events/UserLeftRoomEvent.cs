namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a user leaves a room.
/// Handlers: if host leaves, close the room and evict remaining participants.
/// </summary>
public class UserLeftRoomEvent : BaseEvent
{
    public UserLeftRoomEvent(int roomId, string userId, bool wasHost)
    {
        RoomId = roomId;
        UserId = userId;
        WasHost = wasHost;
    }

    public int RoomId { get; }

    public string UserId { get; }

    public bool WasHost { get; }
}
