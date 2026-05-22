using System.Text.Json;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Notifications.DTOs;

namespace LinguaSpace.Application.Notifications.Queries.GetNotifications;

[Authorize]
public record GetNotificationsQuery(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 30) : IRequest<PaginatedResult<NotificationDto>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PaginatedResult<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetNotificationsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();
        int skip = (request.Page - 1) * request.PageSize;

        IQueryable<Domain.Entities.Notification> query = _context.Notifications
            .Where(n => n.RecipientId == userId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<NotificationProjection> rawItems = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(n => new NotificationProjection(n.Id, n.Type, n.Payload, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        IList<NotificationDto> items = rawItems
            .Select(item => new NotificationDto(item.Id, item.Type, ParsePayload(item.PayloadJson), item.IsRead, item.CreatedAt))
            .ToList();

        return new PaginatedResult<NotificationDto>(items, totalCount, request.Page, request.PageSize, skip + items.Count < totalCount);
    }

    private static JsonElement? ParsePayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record NotificationProjection(int Id, NotificationType Type, string? PayloadJson, bool IsRead, DateTimeOffset CreatedAt);
}
