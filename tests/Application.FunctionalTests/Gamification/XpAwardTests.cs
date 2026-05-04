using LinguaSpace.Application.Gamification.Queries.GetLeaderboard;
using LinguaSpace.Application.Gamification.Queries.GetMyBadges;
using LinguaSpace.Application.Gamification.Queries.GetMyXP;
using LinguaSpace.Application.Gamification.DTOs;
using LinguaSpace.Domain.Entities;

namespace LinguaSpace.Application.FunctionalTests.Gamification;

/// <summary>
/// Functional tests for gamification queries.
/// XP is seeded directly in the DB since the event handlers depend on room/media setup.
/// </summary>
public class XpAwardTests : TestBase
{
    [Test]
    public async Task GetMyXp_ReturnsZeroWhenNoXpRecord()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        XpSummaryDto dto = await TestApp.SendAsync(new GetMyXpQuery());

        dto.TotalXp.ShouldBe(0);
        dto.CurrentStreak.ShouldBe(0);
    }

    [Test]
    public async Task GetMyXp_ReturnsCorrectXpAfterRegister()
    {
        string userId = await TestApp.RegisterAndSetCurrentUserAsync();

        // Seed XP directly to simulate event handler result
        UserXp xp = new()
        {
            UserId = userId,
            TotalXp = 50,
            CurrentStreak = 3,
            LongestStreak = 5,
            LastActivityAt = DateTimeOffset.UtcNow,
        };
        await TestApp.AddAsync(xp);

        XpSummaryDto dto = await TestApp.SendAsync(new GetMyXpQuery());

        dto.TotalXp.ShouldBe(50);
        dto.CurrentStreak.ShouldBe(3);
        dto.LongestStreak.ShouldBe(5);
    }

    [Test]
    public async Task GetMyBadges_ReturnsEmptyWhenNoBadges()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        IList<BadgeDto> badges = await TestApp.SendAsync(new GetMyBadgesQuery());

        badges.ShouldBeEmpty();
    }

    [Test]
    public async Task GetLeaderboard_ReturnsTopUsers()
    {
        string userId = await TestApp.RegisterAndSetCurrentUserAsync();

        UserXp xp = new()
        {
            UserId = userId,
            TotalXp = 200,
            CurrentStreak = 2,
            LongestStreak = 2,
            LastActivityAt = DateTimeOffset.UtcNow,
        };
        await TestApp.AddAsync(xp);

        IList<LeaderboardEntryDto> board = await TestApp.SendAsync(new GetLeaderboardQuery("all", 10));

        board.ShouldNotBeEmpty();
        board[0].TotalXp.ShouldBe(200);
        board[0].Rank.ShouldBe(1);
    }

    [Test]
    public async Task GetLeaderboard_Weekly_ExcludesInactiveUsers()
    {
        // User active 10 days ago (outside weekly window)
        string oldUserId = await TestApp.RunAsDefaultUserAsync();
        UserXp oldXp = new()
        {
            UserId = oldUserId,
            TotalXp = 1000,
            CurrentStreak = 0,
            LongestStreak = 10,
            LastActivityAt = DateTimeOffset.UtcNow.AddDays(-10),
        };
        await TestApp.AddAsync(oldXp);

        // User active today
        string newUserId = await TestApp.RegisterAndSetCurrentUserAsync("active@local");
        UserXp newXp = new()
        {
            UserId = newUserId,
            TotalXp = 50,
            CurrentStreak = 1,
            LongestStreak = 1,
            LastActivityAt = DateTimeOffset.UtcNow,
        };
        await TestApp.AddAsync(newXp);

        IList<LeaderboardEntryDto> board = await TestApp.SendAsync(new GetLeaderboardQuery("weekly", 10));

        board.ShouldHaveSingleItem();
        board[0].TotalXp.ShouldBe(50);
    }
}
