using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Gamification.DTOs;

namespace LinguaSpace.Application.Gamification.Queries.GetLeaderboard;

/// <summary>
/// Returns the top users ranked by total XP.
/// </summary>
/// <param name="Period">"all" (default) or "weekly" (active in last 7 days).</param>
/// <param name="Limit">Number of entries to return (1–50, default 10).</param>
public record GetLeaderboardQuery(
    string Period = "all",
    int Limit = 10) : IRequest<IList<LeaderboardEntryDto>>;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, IList<LeaderboardEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLeaderboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<LeaderboardEntryDto>> Handle(
        GetLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(request.Limit, 1, 50);

        IQueryable<UserXp> query = _context.UserXps;

        if (string.Equals(request.Period, "weekly", StringComparison.OrdinalIgnoreCase))
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-7);
            query = query.Where(x => x.LastActivityAt >= cutoff);
        }

        List<UserXp> topXps = await query
            .OrderByDescending(x => x.TotalXp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (topXps.Count == 0)
        {
            return [];
        }

        List<string> userIds = topXps.Select(x => x.UserId).ToList();

        Dictionary<string, UserProfile> profiles = await _context.UserProfiles
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        List<LeaderboardEntryDto> result = [];

        for (int i = 0; i < topXps.Count; i++)
        {
            UserXp xp = topXps[i];
            profiles.TryGetValue(xp.UserId, out UserProfile? profile);

            result.Add(new LeaderboardEntryDto(
                Rank: i + 1,
                UserId: xp.UserId,
                DisplayName: profile?.DisplayName ?? xp.UserId,
                AvatarUrl: profile?.AvatarUrl,
                TotalXp: xp.TotalXp,
                CurrentStreak: xp.CurrentStreak));
        }

        return result;
    }
}
