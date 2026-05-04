using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetFollowing;

/// <summary>Returns users that the given userId is following.</summary>
[Authorize]
public record GetFollowingQuery(string UserId, int Page = 1, int PageSize = 20) : IRequest<IList<UserSummaryDto>>;

public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, IList<UserSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFollowingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<UserSummaryDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        IList<string> followingIds = await _context.Follows
            .Where(f => f.FollowerId == request.UserId)
            .OrderByDescending(f => f.FollowedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => f.FolloweeId)
            .ToListAsync(cancellationToken);

        IList<UserProfile> profiles = await _context.UserProfiles
            .Where(p => followingIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        // Preserve ordering from followingIds
        return followingIds
            .Select(id => profiles.FirstOrDefault(p => p.UserId == id))
            .Where(p => p is not null)
            .Select(p => new UserSummaryDto(p!.Id, p.UserId, p.DisplayName, p.AvatarUrl, p.IsOnline))
            .ToList();
    }
}
