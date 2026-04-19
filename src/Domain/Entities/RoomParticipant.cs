namespace LinguaSpace.Domain.Entities;

public class RoomParticipant : BaseEntity
{
    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    /// <summary>FK to ApplicationUser.Id.</summary>
    public string UserId { get; set; } = string.Empty;

    public ParticipantRole Role { get; set; } = ParticipantRole.Speaker;

    public DateTimeOffset JoinedAt { get; set; }
}
