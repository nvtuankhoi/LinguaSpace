using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Gamification.DTOs;

namespace LinguaSpace.Application.Gamification.Queries.GetMyBadges;

/// <summary>Returns all badges earned by the current user, newest first.</summary>
[Authorize]
public record GetMyBadgesQuery : IRequest<IList<BadgeDto>>;

public class GetMyBadgesQueryHandler : IRequestHandler<GetMyBadgesQuery, IList<BadgeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetMyBadgesQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IList<BadgeDto>> Handle(GetMyBadgesQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        return await _context.UserBadges
            .Where(ub => ub.UserId == userId)
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
