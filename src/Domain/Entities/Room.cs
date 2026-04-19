namespace LinguaSpace.Domain.Entities;

public class Room : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>ISO 639-1 language code for the room's primary language.</summary>
    public string LanguageCode { get; set; } = string.Empty;

    public int MaxParticipants { get; set; } = 20;

    public RoomStatus Status { get; set; } = RoomStatus.Active;

    public RoomType RoomType { get; set; } = RoomType.TextOnly;

    /// <summary>LiveKit room name for media sessions (Phase 2).</summary>
    public string? LiveKitRoomName { get; set; }

    /// <summary>FK to ApplicationUser.Id of the host.</summary>
    public string HostId { get; set; } = string.Empty;

    public ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
