namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Comment on a Post. Supports one level of nesting (replies via ParentCommentId).
/// </summary>
public class Comment : BaseAuditableEntity
{
    public int PostId { get; set; }

    public Post Post { get; set; } = null!;

    /// <summary>FK to ApplicationUser.Id.</summary>
    public string AuthorId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>Null for top-level comments. Set for replies (one level only).</summary>
    public int? ParentCommentId { get; set; }

    public Comment? ParentComment { get; set; }

    public int LikeCount { get; set; }

    public bool IsDeleted { get; set; }
}
