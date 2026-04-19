namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a user joins a room.
/// Handlers: update presence, notify room participants via SignalR.
/// </summary>
public class UserJoinedRoomEvent : BaseEvent
{
    public UserJoinedRoomEvent(int roomId, string userId, ParticipantRole role)
    {
        RoomId = roomId;
        UserId = userId;
        Role = role;
    }

    public int RoomId { get; }

    public string UserId { get; }

    public ParticipantRole Role { get; }
}
