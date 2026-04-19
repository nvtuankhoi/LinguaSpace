namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Social feed post. Supports multiple PostTypes with optional JSON metadata.
/// VocabCard type stores word/definition/example in Metadata JSON.
/// </summary>
public class Post : BaseAuditableEntity
{
    /// <summary>FK to ApplicationUser.Id.</summary>
    public string AuthorId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public PostType PostType { get; set; } = PostType.Text;

    /// <summary>JSON payload for VocabCard (word, definition, example, partOfSpeech) or Poll options.</summary>
    public string? Metadata { get; set; }

    /// <summary>ISO 639-1 language code for the post's language context.</summary>
    public string? LanguageCode { get; set; }

    public int LikeCount { get; set; }

    public int CommentCount { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<PostMediaItem> MediaItems { get; set; } = new List<PostMediaItem>();

    public ICollection<PostTag> Tags { get; set; } = new List<PostTag>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
