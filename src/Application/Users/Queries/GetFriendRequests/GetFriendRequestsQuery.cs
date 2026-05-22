using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetFriendRequests;

[Authorize]
public record GetFriendRequestsQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<FriendRequestDto>>;

public class GetFriendRequestsQueryHandler : IRequestHandler<GetFriendRequestsQuery, PaginatedResult<FriendRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetFriendRequestsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<FriendRequestDto>> Handle(GetFriendRequestsQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();
        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);
        int skip = (page - 1) * pageSize;

        IQueryable<FriendRequestDto> query =
            from friendship in _context.Friendships.AsNoTracking()
            join requester in _context.UserProfiles.AsNoTracking() on friendship.RequesterId equals requester.UserId
            join addressee in _context.UserProfiles.AsNoTracking() on friendship.AddresseeId equals addressee.UserId
            where friendship.Status == FriendshipStatus.Pending
                && (friendship.RequesterId == userId || friendship.AddresseeId == userId)
            orderby friendship.CreatedAt descending
            select new FriendRequestDto(
                friendship.Id,
                requester.UserId,
                requester.DisplayName,
                requester.AvatarUrl,
                addressee.UserId,
                addressee.DisplayName,
                addressee.AvatarUrl,
                friendship.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);
        IList<FriendRequestDto> items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResult<FriendRequestDto>(items, totalCount, page, pageSize, skip + items.Count < totalCount);
    }
}
