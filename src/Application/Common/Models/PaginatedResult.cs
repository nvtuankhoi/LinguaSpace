namespace LinguaSpace.Application.Common.Models;

/// <summary>Wrapper for offset-based paginated list responses.</summary>
public record PaginatedResult<T>(
    IList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>Wrapper for cursor-based paginated list responses.</summary>
public record CursorPagedResult<T>(
    IList<T> Items,
    bool HasMore,
    DateTimeOffset? NextCursor);
