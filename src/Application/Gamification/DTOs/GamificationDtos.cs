namespace LinguaSpace.Application.Gamification.DTOs;

public record XpSummaryDto(
    int TotalXp,
    int CurrentStreak,
    int LongestStreak,
    DateTimeOffset? LastActivityAt,
    int BadgeCount,
    int Rank);

public record LeaderboardEntryDto(
    int Rank,
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    int TotalXp,
    int CurrentStreak);

public record BadgeDto(
    int BadgeId,
    string Code,
    string Name,
    string? Description,
    string? IconUrl,
    DateTimeOffset EarnedAt);
