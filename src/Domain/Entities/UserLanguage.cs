namespace LinguaSpace.Domain.Entities;

public class UserLanguage : BaseEntity
{
    public int UserProfileId { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    /// <summary>ISO 639-1 language code (e.g., "en", "vi", "ja").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    public LanguageType Type { get; set; }

    public LanguageLevel? Level { get; set; }
}
