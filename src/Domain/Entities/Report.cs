namespace LinguaSpace.Domain.Entities;

/// <summary>
/// User-submitted report against any content or user.
/// Moderation team reviews pending reports.
/// </summary>
public class Report : BaseEntity
{
    /// <summary>FK to ApplicationUser.Id — user who submitted the report.</summary>
    public string ReporterId { get; set; } = string.Empty;

    /// <summary>Id of the reported entity (userId, roomId, postId, etc.).</summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>Type of the reported target (e.g., "User", "Post", "Room", "Message").</summary>
    public string TargetType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>FK to ApplicationUser.Id — moderator who resolved the report.</summary>
    public string? ResolvedBy { get; set; }
}
