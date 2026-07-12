namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a host mutes or unmutes a participant.
/// Handler: broadcast a ParticipantMuted SignalR event to the room group.
/// </summary>
public class ParticipantMutedEvent : BaseEvent
{
    public ParticipantMutedEvent(int roomId, string userId, bool isMuted)
    {
        RoomId = roomId;
        UserId = userId;
        IsMuted = isMuted;
    }

    public int RoomId { get; }

    public string UserId { get; }

    public bool IsMuted { get; }
}
