namespace LinguaSpace.Application.Social.DTOs;

public record ConversationDto(
    int Id,
    string OtherUserId,
    string? LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);

public record DirectMessageDto(
    int Id,
    int ConversationId,
    string SenderId,
    string Content,
    DateTimeOffset SentAt,
    bool IsRead);
