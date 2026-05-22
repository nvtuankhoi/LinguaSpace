using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetFollowers;

/// <summary>Returns users who follow the given userId.</summary>
[Authorize]
public record GetFollowersQuery(string UserId, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<UserSummaryDto>>;

public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, PaginatedResult<UserSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFollowersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<UserSummaryDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
    {
        int skip = (request.Page - 1) * request.PageSize;

        IQueryable<Follow> query = _context.Follows
            .Where(f => f.FolloweeId == request.UserId);

        int totalCount = await query.CountAsync(cancellationToken);

        IList<string> followerIds = await query
            .OrderByDescending(f => f.FollowedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);

        IList<UserProfile> profiles = await _context.UserProfiles
            .Where(p => followerIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        IList<UserSummaryDto> items = followerIds
            .Select(id => profiles.FirstOrDefault(p => p.UserId == id))
            .Where(p => p is not null)
            .Select(p => new UserSummaryDto(p!.Id, p.UserId, p.DisplayName, p.AvatarUrl, p.IsOnline))
            .ToList();

        return new PaginatedResult<UserSummaryDto>(items, totalCount, request.Page, request.PageSize, skip + items.Count < totalCount);
    }
}
