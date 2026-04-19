namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Tracks media session (voice/video) participation per user per room.
/// Used for analytics and XP rewards (Phase 2).
/// </summary>
public class RoomMediaSession : BaseEntity
{
    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    /// <summary>FK to ApplicationUser.Id.</summary>
    public string UserId { get; set; } = string.Empty;

    public DateTimeOffset JoinedAt { get; set; }

    public DateTimeOffset? LeftAt { get; set; }

    public int? DurationSeconds { get; set; }

    public bool WasScreenSharing { get; set; }
}
