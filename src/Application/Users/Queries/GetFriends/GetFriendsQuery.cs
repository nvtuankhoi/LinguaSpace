using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetFriends;

/// <summary>Returns accepted friends for the given userId.</summary>
[Authorize]
public record GetFriendsQuery(string UserId, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<UserSummaryDto>>;

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, PaginatedResult<UserSummaryDto>>
{
    private const int MaxPageSize = 50;
    private readonly IApplicationDbContext _context;

    public GetFriendsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<UserSummaryDto>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
    {
        int pageSize = Math.Min(request.PageSize, MaxPageSize);
        int skip = (request.Page - 1) * pageSize;

        IQueryable<UserSummaryDto> requesterFriendsQuery =
            from friendship in _context.Friendships.AsNoTracking()
            join profile in _context.UserProfiles.AsNoTracking() on friendship.AddresseeId equals profile.UserId
            where friendship.Status == FriendshipStatus.Accepted
                && friendship.RequesterId == request.UserId
            select new UserSummaryDto(profile.Id, profile.UserId, profile.DisplayName, profile.AvatarUrl, profile.IsOnline);

        IQueryable<UserSummaryDto> addresseeFriendsQuery =
            from friendship in _context.Friendships.AsNoTracking()
            join profile in _context.UserProfiles.AsNoTracking() on friendship.RequesterId equals profile.UserId
            where friendship.Status == FriendshipStatus.Accepted
                && friendship.AddresseeId == request.UserId
            select new UserSummaryDto(profile.Id, profile.UserId, profile.DisplayName, profile.AvatarUrl, profile.IsOnline);

        IQueryable<UserSummaryDto> query = requesterFriendsQuery
            .Concat(addresseeFriendsQuery)
            .OrderBy(profile => profile.DisplayName)
            .ThenBy(profile => profile.UserId);

        int totalCount = await query.CountAsync(cancellationToken);

        IList<UserSummaryDto> rawItems = await query
            .Skip(skip)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = rawItems.Count > pageSize;
        IList<UserSummaryDto> items = hasMore ? rawItems.Take(pageSize).ToList() : rawItems;

        return new PaginatedResult<UserSummaryDto>(items, totalCount, request.Page, pageSize, hasMore);
    }
}
