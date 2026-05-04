using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetFollowers;

/// <summary>Returns users who follow the given userId.</summary>
[Authorize]
public record GetFollowersQuery(string UserId, int Page = 1, int PageSize = 20) : IRequest<IList<UserSummaryDto>>;

public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, IList<UserSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFollowersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<UserSummaryDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
    {
        IList<string> followerIds = await _context.Follows
            .Where(f => f.FolloweeId == request.UserId)
            .OrderByDescending(f => f.FollowedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);

        IList<UserProfile> profiles = await _context.UserProfiles
            .Where(p => followerIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        // Preserve ordering from followerIds
        return followerIds
            .Select(id => profiles.FirstOrDefault(p => p.UserId == id))
            .Where(p => p is not null)
            .Select(p => new UserSummaryDto(p!.Id, p.UserId, p.DisplayName, p.AvatarUrl, p.IsOnline))
            .ToList();
    }
}
