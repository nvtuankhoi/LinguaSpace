using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Gamification.Common;

namespace LinguaSpace.Application.Gamification.Common;

/// <summary>
/// Checks XP + streak thresholds and awards any missing badges to a user.
/// Call this after updating UserXp and before saving changes.
/// </summary>
public static class BadgeAwarder
{
    /// <summary>
    /// Evaluates all badge conditions against <paramref name="userXp"/> and adds any
    /// newly-earned <see cref="UserBadge"/> entries to the context.
    /// Call <see cref="IApplicationDbContext.SaveChangesAsync"/> after this method.
    /// </summary>
    public static async Task AwardEligibleBadgesAsync(
        IApplicationDbContext context,
        UserXp userXp,
        CancellationToken cancellationToken)
    {
        // Load all badge master records (small table, safe to enumerate)
        List<Badge> allBadges = await context.Badges.ToListAsync(cancellationToken);

        // Load badge codes the user already has
        HashSet<string> earnedCodes = (await context.UserBadges
            .Where(ub => ub.UserId == userXp.UserId)
            .Select(ub => ub.Badge.Code)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (Badge badge in allBadges)
        {
            if (earnedCodes.Contains(badge.Code))
            {
                continue;
            }

            bool eligible = badge.Code switch
            {
                BadgeCodes.FirstRoom => userXp.TotalXp > 0,
                BadgeCodes.Streak3   => userXp.CurrentStreak >= 3,
                BadgeCodes.Streak7   => userXp.CurrentStreak >= 7,
                BadgeCodes.Streak30  => userXp.CurrentStreak >= 30,
                BadgeCodes.Xp100     => userXp.TotalXp >= 100,
                BadgeCodes.Xp500     => userXp.TotalXp >= 500,
                BadgeCodes.Xp1000    => userXp.TotalXp >= 1000,
                _ => false,
            };

            if (eligible)
            {
                context.UserBadges.Add(new UserBadge
                {
                    UserId = userXp.UserId,
                    BadgeId = badge.Id,
                    EarnedAt = now,
                });
            }
        }
    }

    /// <summary>
    /// Updates streak fields on <paramref name="userXp"/> by delegating to <see cref="UserXp.UpdateStreak"/>.
    /// Returns true if the streak was updated (new day activity).
    /// </summary>
    public static bool UpdateStreak(UserXp userXp) => userXp.UpdateStreak();
}
