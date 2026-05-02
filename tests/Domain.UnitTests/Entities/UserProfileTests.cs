using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for UserProfile entity.
///
/// These tests verify the entity's property contracts and defaults
/// without requiring a database or DI container.
/// </summary>
public class UserProfileTests
{
    [Test]
    public void NewUserProfile_HasExpectedDefaults()
    {
        UserProfile profile = new();

        profile.UserId.ShouldBe(string.Empty);
        profile.DisplayName.ShouldBe(string.Empty);
        profile.Bio.ShouldBeNull();
        profile.AvatarUrl.ShouldBeNull();
        profile.Timezone.ShouldBeNull();
        profile.IsOnline.ShouldBeFalse();
        profile.LastSeenAt.ShouldBeNull();
        profile.Languages.ShouldBeEmpty();
    }

    [Test]
    public void SetDisplayName_Persists()
    {
        UserProfile profile = new() { DisplayName = "Alice" };

        profile.DisplayName.ShouldBe("Alice");
    }

    [Test]
    public void SetAvatarUrl_Persists()
    {
        UserProfile profile = new();
        string url = "https://example.com/avatar.png";

        profile.AvatarUrl = url;

        profile.AvatarUrl.ShouldBe(url);
    }

    [Test]
    public void SetAvatarUrlToNull_ClearsAvatar()
    {
        UserProfile profile = new() { AvatarUrl = "https://example.com/avatar.png" };

        profile.AvatarUrl = null;

        profile.AvatarUrl.ShouldBeNull();
    }

    [Test]
    public void SetIsOnline_True_Persists()
    {
        UserProfile profile = new();

        profile.IsOnline = true;

        profile.IsOnline.ShouldBeTrue();
    }

    [Test]
    public void SetLastSeenAt_Persists()
    {
        UserProfile profile = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        profile.LastSeenAt = now;

        profile.LastSeenAt.ShouldBe(now);
    }

    [Test]
    public void LanguagesCollection_CanAddLanguage()
    {
        UserProfile profile = new();
        UserLanguage language = new()
        {
            LanguageCode = "en",
            Type = LanguageType.Native,
        };

        profile.Languages.Add(language);

        profile.Languages.Count.ShouldBe(1);
        profile.Languages.First().LanguageCode.ShouldBe("en");
    }

    [Test]
    public void LanguagesCollection_CanAddMultipleLanguages()
    {
        UserProfile profile = new();

        profile.Languages.Add(new UserLanguage { LanguageCode = "en", Type = LanguageType.Native });
        profile.Languages.Add(new UserLanguage { LanguageCode = "fr", Type = LanguageType.Learning });

        profile.Languages.Count.ShouldBe(2);
    }
}
