namespace LinguaSpace.Application.Auth.DTOs;

public record CurrentUserDto(string UserId, string Email, string DisplayName, IList<string> Roles, string? AvatarUrl, bool IsEmailConfirmed);
