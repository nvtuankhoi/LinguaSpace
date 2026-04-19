namespace LinguaSpace.Application.Users.DTOs;

/// <summary>Lightweight user summary used in search results and social lists.</summary>
public record UserSummaryDto(
    int Id,
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    bool IsOnline);
