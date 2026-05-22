using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Moderation.Commands.BanUser;
using LinguaSpace.Application.Moderation.Commands.ReportContent;
using LinguaSpace.Application.Moderation.Commands.ResolveReport;
using LinguaSpace.Application.Moderation.Commands.UnbanUser;
using LinguaSpace.Application.Moderation.DTOs;
using LinguaSpace.Application.Moderation.Queries.GetReport;
using LinguaSpace.Application.Moderation.Queries.GetReports;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Moderation: report content, view reports, resolve reports, ban users.
/// Route: /api/Moderation
/// </summary>
public class Moderation : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(ReportContent, "report").RequireAuthorization();
        group.MapGet(GetReports, "reports").RequireAuthorization();
        group.MapGet(GetReport, "reports/{reportId}").RequireAuthorization();
        group.MapPost(ResolveReport, "reports/{reportId}/resolve").RequireAuthorization();
        group.MapPost(BanUser, "users/{userId}/ban").RequireAuthorization();
        group.MapDelete(UnbanUser, "users/{userId}/ban").RequireAuthorization();
    }

    // ─── POST /api/Moderation/report ─────────────────────────────────────────

    [EndpointSummary("Report content")]
    [EndpointDescription("Submit a moderation report against a user, post, room, or message.")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<Created<int>> ReportContent(
        [FromBody] ReportContentCommand command,
        ISender sender)
    {
        int reportId = await sender.Send(command);
        return TypedResults.Created($"/api/Moderation/reports", reportId);
    }

    // ─── GET /api/Moderation/reports ─────────────────────────────────────────

    [EndpointSummary("Get moderation reports (admin)")]
    [EndpointDescription("Returns paginated reports. Defaults to Pending status. Admin only.")]
    [ProducesResponseType(typeof(PaginatedResult<ReportDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<ReportDto>>> GetReports(
        ISender sender,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<ReportDto> result = await sender.Send(new GetReportsQuery(status, page, pageSize));
        return TypedResults.Ok(result);
    }

    // ─── GET /api/Moderation/reports/{reportId} ───────────────────────────────

    [EndpointSummary("Get a specific report (admin)")]
    [EndpointDescription("Returns the details of a single moderation report by ID.")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Ok<ReportDto>> GetReport(
        [FromRoute] int reportId,
        ISender sender)
    {
        ReportDto dto = await sender.Send(new GetReportQuery(reportId));
        return TypedResults.Ok(dto);
    }

    // ─── POST /api/Moderation/reports/{reportId}/resolve ─────────────────────

    [EndpointSummary("Resolve or dismiss a report (admin)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> ResolveReport(
        [FromRoute] int reportId,
        [FromBody] ResolveReportBody body,
        ISender sender)
    {
        await sender.Send(new ResolveReportCommand(reportId, body.Action));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Moderation/users/{userId}/ban ──────────────────────────────

    [EndpointSummary("Ban a user (admin)")]
    [EndpointDescription("Locks out the user account. Omit 'until' for a permanent ban.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> BanUser(
        [FromRoute] string userId,
        [FromBody] BanUserBody body,
        ISender sender)
    {
        await sender.Send(new BanUserCommand(userId, body.Until));
        return TypedResults.NoContent();
    }

    // ─── DELETE /api/Moderation/users/{userId}/ban ────────────────────────────

    [EndpointSummary("Unban a user (admin)")]
    [EndpointDescription("Removes the lockout from a user account, restoring normal access.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> UnbanUser(
        [FromRoute] string userId,
        ISender sender)
    {
        await sender.Send(new UnbanUserCommand(userId));
        return TypedResults.NoContent();
    }

    public record ResolveReportBody(ReportAction Action);
    public record BanUserBody(DateTimeOffset? Until = null);
}
