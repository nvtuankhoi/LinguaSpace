namespace LinguaSpace.Application.Media.DTOs;

public record MediaTokenDto(
    string Token,
    string LiveKitUrl);

public record RoomMediaParticipantDto(
    string UserId,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt,
    int? DurationSeconds,
    bool WasScreenSharing,
    bool IsActive);
