namespace LinguaSpace.Application.Rooms.DTOs;

public record RoomParticipantDto(string UserId, string DisplayName, string? AvatarUrl, string Role, DateTimeOffset JoinedAt);

public record RoomDto(
    int Id,
    string Title,
    string? Description,
    string LanguageCode,
    int MaxParticipants,
    int ParticipantCount,
    string Status,
    string RoomType,
    string HostId,
    DateTimeOffset Created,
    IList<RoomParticipantDto> Participants);

public record RoomSummaryDto(
    int Id,
    string Title,
    string LanguageCode,
    int MaxParticipants,
    int ParticipantCount,
    string RoomType,
    string HostId);
