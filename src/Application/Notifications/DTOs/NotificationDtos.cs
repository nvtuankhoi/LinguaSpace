namespace LinguaSpace.Application.Notifications.DTOs;

public record NotificationDto(
    int Id,
    string Type,
    string? Payload,
    bool IsRead,
    DateTimeOffset CreatedAt);
