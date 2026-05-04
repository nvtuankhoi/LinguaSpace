using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Gamification.DTOs;

namespace LinguaSpace.Application.Gamification.Queries.GetMyXP;

/// <summary>Returns the current user's XP summary (total XP, streaks, rank, badge count).</summary>
[Authorize]
public record GetMyXpQuery : IRequest<XpSummaryDto>;

public class GetMyXpQueryHandler : IRequestHandler<GetMyXpQuery, XpSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetMyXpQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<XpSummaryDto> Handle(GetMyXpQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserXp? userXp = await _context.UserXps
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (userXp is null)
        {
            return new XpSummaryDto(0, 0, 0, null, 0, 0);
        }

        int badgeCount = await _context.UserBadges
            .CountAsync(ub => ub.UserId == userId, cancellationToken);

        // Rank = count of users with strictly more XP + 1
        int rank = await _context.UserXps
            .CountAsync(x => x.TotalXp > userXp.TotalXp, cancellationToken) + 1;

        return new XpSummaryDto(
            userXp.TotalXp,
            userXp.CurrentStreak,
            userXp.LongestStreak,
            userXp.LastActivityAt,
            badgeCount,
            rank);
    }
}
