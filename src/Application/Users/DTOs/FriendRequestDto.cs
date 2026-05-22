namespace LinguaSpace.Application.Users.DTOs;

public record FriendRequestDto(
    int Id,
    string RequesterId,
    string RequesterDisplayName,
    string? RequesterAvatarUrl,
    string AddresseeId,
    string AddresseeDisplayName,
    string? AddresseeAvatarUrl,
    DateTimeOffset CreatedAt);
