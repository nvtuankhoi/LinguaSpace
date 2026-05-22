using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetFollowing;

/// <summary>Returns users that the given userId is following.</summary>
[Authorize]
public record GetFollowingQuery(string UserId, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<UserSummaryDto>>;

public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, PaginatedResult<UserSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFollowingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<UserSummaryDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        int skip = (request.Page - 1) * request.PageSize;

        IQueryable<Follow> query = _context.Follows
            .Where(f => f.FollowerId == request.UserId);

        int totalCount = await query.CountAsync(cancellationToken);

        IList<string> followingIds = await query
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(f => f.FolloweeId)
            .ToListAsync(cancellationToken);

        IList<UserProfile> profiles = await _context.UserProfiles
            .Where(p => followingIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        IList<UserSummaryDto> items = followingIds
            .Select(id => profiles.FirstOrDefault(p => p.UserId == id))
            .Where(p => p is not null)
            .Select(p => new UserSummaryDto(p!.Id, p.UserId, p.DisplayName, p.AvatarUrl, p.IsOnline))
            .ToList();

        return new PaginatedResult<UserSummaryDto>(items, totalCount, request.Page, request.PageSize, skip + items.Count < totalCount);
    }
}
