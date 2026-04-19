namespace LinguaSpace.Application.Rooms.DTOs;

/// <summary>
/// A single chat message in a room.
/// Cursor is the SentAt timestamp (ISO 8601) used for cursor-based pagination.
/// </summary>
public record MessageDto(
    int Id,
    string SenderId,
    string SenderDisplayName,
    string Content,
    string Type,
    DateTimeOffset SentAt,
    bool IsDeleted);
