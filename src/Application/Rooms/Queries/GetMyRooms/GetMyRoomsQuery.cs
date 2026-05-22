using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Rooms.DTOs;

namespace LinguaSpace.Application.Rooms.Queries.GetMyRooms;

[Authorize]
public record GetMyRoomsQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<RoomSummaryDto>>;

public class GetMyRoomsQueryHandler : IRequestHandler<GetMyRoomsQuery, PaginatedResult<RoomSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetMyRoomsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<RoomSummaryDto>> Handle(GetMyRoomsQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();
        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);
        int skip = (page - 1) * pageSize;

        IQueryable<RoomSummaryDto> query = _context.RoomParticipants
            .AsNoTracking()
            .Where(participant => participant.UserId == userId && participant.Room.Status == RoomStatus.Active)
            .OrderByDescending(participant => participant.JoinedAt)
            .Select(participant => new RoomSummaryDto(
                participant.Room.Id,
                participant.Room.Title,
                participant.Room.LanguageCode,
                participant.Room.MaxParticipants,
                participant.Room.Participants.Count,
                participant.Room.RoomType,
                participant.Room.HostId));

        int totalCount = await query.CountAsync(cancellationToken);
        IList<RoomSummaryDto> items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResult<RoomSummaryDto>(items, totalCount, page, pageSize, skip + items.Count < totalCount);
    }
}
