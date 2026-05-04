using LinguaSpace.Application.Gamification.DTOs;
using LinguaSpace.Application.Gamification.Queries.GetLeaderboard;
using LinguaSpace.Application.Gamification.Queries.GetMyBadges;
using LinguaSpace.Application.Gamification.Queries.GetMyXP;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Gamification: XP, streaks, badges, and leaderboard.
/// Route: /api/Gamification
/// </summary>
public class Gamification : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetLeaderboard, "leaderboard");
        group.MapGet(GetMyXp, "me/xp").RequireAuthorization();
        group.MapGet(GetMyBadges, "me/badges").RequireAuthorization();
    }

    // ─── GET /api/Gamification/leaderboard ───────────────────────────────────

    [EndpointSummary("Get XP leaderboard")]
    [EndpointDescription("Returns top users by total XP. period=all|weekly, limit=1–50.")]
    [ProducesResponseType(typeof(IList<LeaderboardEntryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<LeaderboardEntryDto>>> GetLeaderboard(
        ISender sender,
        [FromQuery] string period = "all",
        [FromQuery] int limit = 10)
    {
        IList<LeaderboardEntryDto> entries = await sender.Send(new GetLeaderboardQuery(period, limit));
        return TypedResults.Ok(entries);
    }

    // ─── GET /api/Gamification/me/xp ─────────────────────────────────────────

    [EndpointSummary("Get my XP summary")]
    [EndpointDescription("Returns the current user's total XP, streak, longest streak, and rank.")]
    [ProducesResponseType(typeof(XpSummaryDto), StatusCodes.Status200OK)]
    public static async Task<Ok<XpSummaryDto>> GetMyXp(ISender sender)
    {
        XpSummaryDto dto = await sender.Send(new GetMyXpQuery());
        return TypedResults.Ok(dto);
    }

    // ─── GET /api/Gamification/me/badges ─────────────────────────────────────

    [EndpointSummary("Get my badges")]
    [EndpointDescription("Returns all badges earned by the current user, most recent first.")]
    [ProducesResponseType(typeof(IList<BadgeDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<BadgeDto>>> GetMyBadges(ISender sender)
    {
        IList<BadgeDto> badges = await sender.Send(new GetMyBadgesQuery());
        return TypedResults.Ok(badges);
    }
}
