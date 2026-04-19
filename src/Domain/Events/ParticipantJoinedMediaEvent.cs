namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a user joins a media (voice/video) session in a room.
/// Handler: create RoomMediaSession record, notify other participants.
/// </summary>
public class ParticipantJoinedMediaEvent : BaseEvent
{
    public ParticipantJoinedMediaEvent(int roomId, string userId)
    {
        RoomId = roomId;
        UserId = userId;
    }

    public int RoomId { get; }

    public string UserId { get; }
}
