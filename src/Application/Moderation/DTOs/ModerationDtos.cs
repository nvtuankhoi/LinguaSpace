using LinguaSpace.Application.Common.Models;

namespace LinguaSpace.Application.Moderation.DTOs;

public record ReportDto(
    int Id,
    string ReporterId,
    string TargetId,
    string TargetType,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolvedBy);
