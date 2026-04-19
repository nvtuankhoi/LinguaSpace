namespace LinguaSpace.Application.Users.DTOs;

public record UserLanguageDto(int Id, string LanguageCode, string Type, string? Level);

public record UserProfileDto(
    int Id,
    string UserId,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Timezone,
    bool IsOnline,
    DateTimeOffset? LastSeenAt,
    IList<UserLanguageDto> Languages);
