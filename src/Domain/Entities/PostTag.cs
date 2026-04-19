namespace LinguaSpace.Domain.Entities;

/// <summary>Max 5 tags per post. Tags are stored in lowercase.</summary>
public class PostTag : BaseEntity
{
    public int PostId { get; set; }

    public Post Post { get; set; } = null!;

    /// <summary>Lowercase tag string (e.g., "grammar", "vocabulary").</summary>
    public string Tag { get; set; } = string.Empty;
}
