using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Rooms.DTOs;

namespace LinguaSpace.Application.Rooms.Queries.GetRooms;

[Authorize]
public record GetRoomsQuery(string? LanguageCode, string? RoomType, string? Q, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedResult<RoomSummaryDto>>;

public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, PaginatedResult<RoomSummaryDto>>
{
    private const int MaxPageSize = 50;
    private readonly IApplicationDbContext _context;

    public GetRoomsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<RoomSummaryDto>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        int pageSize = Math.Min(request.PageSize, MaxPageSize);
        int skip = (request.Page - 1) * pageSize;

        IQueryable<Room> query = _context.Rooms
            .AsNoTracking()
            .Where(r => r.Status == RoomStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            query = query.Where(r => r.LanguageCode == request.LanguageCode);
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            query = query.Where(r => r.Title.Contains(request.Q));
        }

        if (!string.IsNullOrWhiteSpace(request.RoomType)
            && Enum.TryParse<RoomType>(request.RoomType, ignoreCase: true, out RoomType parsedType))
        {
            query = query.Where(r => r.RoomType == parsedType);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        IList<RoomSummaryDto> items = await query
            .OrderByDescending(r => r.Created)
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new RoomSummaryDto(
                r.Id,
                r.Title,
                r.LanguageCode,
                r.MaxParticipants,
                r.Participants.Count,
                r.RoomType,
                r.HostId))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<RoomSummaryDto>(items, totalCount, request.Page, pageSize, skip + items.Count < totalCount);
    }
}
