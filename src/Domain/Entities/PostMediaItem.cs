namespace LinguaSpace.Domain.Entities;

/// <summary>Max 4 media items per post (image/video URLs).</summary>
public class PostMediaItem : BaseEntity
{
    public int PostId { get; set; }

    public Post Post { get; set; } = null!;

    public string Url { get; set; } = string.Empty;

    /// <summary>Display order (0-based). Max 4 items.</summary>
    public int SortOrder { get; set; }
}
