using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Gamification.DTOs;

namespace LinguaSpace.Application.Gamification.Queries.GetUserXP;

/// <summary>Returns a specific user's XP summary (total XP, streaks, rank, badge count).</summary>
[Authorize]
public record GetUserXpQuery(string UserId) : IRequest<XpSummaryDto>;

public class GetUserXpQueryHandler : IRequestHandler<GetUserXpQuery, XpSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserXpQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<XpSummaryDto> Handle(GetUserXpQuery request, CancellationToken cancellationToken)
    {
        UserXp? userXp = await _context.UserXps
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (userXp is null)
        {
            return new XpSummaryDto(0, 0, 0, null, 0, 0);
        }

        int badgeCount = await _context.UserBadges
            .CountAsync(ub => ub.UserId == request.UserId, cancellationToken);

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
