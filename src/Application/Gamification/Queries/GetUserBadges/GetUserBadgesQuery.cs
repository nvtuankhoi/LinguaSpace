using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Gamification.DTOs;

namespace LinguaSpace.Application.Gamification.Queries.GetUserBadges;

/// <summary>Returns all badges earned by the specified user, newest first.</summary>
[Authorize]
public record GetUserBadgesQuery(string UserId) : IRequest<IList<BadgeDto>>;

public class GetUserBadgesQueryHandler : IRequestHandler<GetUserBadgesQuery, IList<BadgeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserBadgesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<BadgeDto>> Handle(GetUserBadgesQuery request, CancellationToken cancellationToken)
    {
        return await _context.UserBadges
            .Where(ub => ub.UserId == request.UserId)
            .Include(ub => ub.Badge)
            .OrderByDescending(ub => ub.EarnedAt)
            .Select(ub => new BadgeDto(
                ub.BadgeId,
                ub.Badge.Code,
                ub.Badge.Name,
                ub.Badge.Description,
                ub.Badge.IconUrl,
                ub.EarnedAt))
            .ToListAsync(cancellationToken);
    }
}
