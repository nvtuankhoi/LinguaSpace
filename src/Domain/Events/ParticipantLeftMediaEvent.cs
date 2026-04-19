namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a user leaves a media session.
/// Handler: finalize RoomMediaSession (set LeftAt, compute DurationSeconds), award XP.
/// </summary>
public class ParticipantLeftMediaEvent : BaseEvent
{
    public ParticipantLeftMediaEvent(int roomId, string userId, int mediaSessionId)
    {
        RoomId = roomId;
        UserId = userId;
        MediaSessionId = mediaSessionId;
    }

    public int RoomId { get; }

    public string UserId { get; }

    public int MediaSessionId { get; }
}
