using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for UserXp streak logic (via entity method) and Report defaults.
/// </summary>
public class GamificationEntityTests
{
    // ─── Streak: first activity ───────────────────────────────────────────────

    [Test]
    public void UpdateStreak_FirstActivity_SetsStreakToOne()
    {
        UserXp xp = new() { UserId = "u1", LastActivityAt = null };

        bool changed = xp.UpdateStreak();

        changed.ShouldBeTrue();
        xp.CurrentStreak.ShouldBe(1);
        xp.LongestStreak.ShouldBe(1);
        xp.LastActivityAt.ShouldNotBeNull();
    }

    // ─── Streak: same-day activity ────────────────────────────────────────────

    [Test]
    public void UpdateStreak_SameDayActivity_DoesNotChangeStreak()
    {
        UserXp xp = new()
        {
            UserId = "u1",
            CurrentStreak = 3,
            LongestStreak = 3,
            LastActivityAt = DateTimeOffset.UtcNow,
        };

        bool changed = xp.UpdateStreak();

        changed.ShouldBeFalse();
        xp.CurrentStreak.ShouldBe(3);
    }

    // ─── Streak: consecutive day ──────────────────────────────────────────────

    [Test]
    public void UpdateStreak_NextDay_IncrementsStreak()
    {
        UserXp xp = new()
        {
            UserId = "u1",
            CurrentStreak = 6,
            LongestStreak = 6,
            LastActivityAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

        bool changed = xp.UpdateStreak();

        changed.ShouldBeTrue();
        xp.CurrentStreak.ShouldBe(7);
        xp.LongestStreak.ShouldBe(7);
    }

    // ─── Streak: gap resets ───────────────────────────────────────────────────

    [Test]
    public void UpdateStreak_GapMoreThanOneDay_ResetsStreakToOne()
    {
        UserXp xp = new()
        {
            UserId = "u1",
            CurrentStreak = 10,
            LongestStreak = 15,
            LastActivityAt = DateTimeOffset.UtcNow.AddDays(-3),
        };

        bool changed = xp.UpdateStreak();

        changed.ShouldBeTrue();
        xp.CurrentStreak.ShouldBe(1);
        // LongestStreak must NOT be reduced
        xp.LongestStreak.ShouldBe(15);
    }

    // ─── Streak: longest streak updated ──────────────────────────────────────

    [Test]
    public void UpdateStreak_NewStreak_UpdatesLongestStreak()
    {
        UserXp xp = new()
        {
            UserId = "u1",
            CurrentStreak = 9,
            LongestStreak = 9,
            LastActivityAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

        xp.UpdateStreak();

        xp.CurrentStreak.ShouldBe(10);
        xp.LongestStreak.ShouldBe(10);
    }

    // ─── XP entity defaults ───────────────────────────────────────────────────

    [Test]
    public void NewUserXp_HasExpectedDefaults()
    {
        UserXp xp = new();

        xp.TotalXp.ShouldBe(0);
        xp.CurrentStreak.ShouldBe(0);
        xp.LongestStreak.ShouldBe(0);
        xp.LastActivityAt.ShouldBeNull();
    }

    // ─── Report entity defaults ───────────────────────────────────────────────

    [Test]
    public void NewReport_HasPendingStatus()
    {
        Report report = new();

        report.Status.ShouldBe(ReportStatus.Pending);
        report.ResolvedAt.ShouldBeNull();
        report.ResolvedBy.ShouldBeNull();
    }
}
