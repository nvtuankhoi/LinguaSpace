using LinguaSpace.Application.Moderation.Commands.ReportContent;
using LinguaSpace.Application.Moderation.DTOs;
using LinguaSpace.Application.Moderation.Queries.GetReports;
using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Application.FunctionalTests.Moderation;

/// <summary>
/// Functional tests for moderation: report content, get reports (admin), resolve report.
/// </summary>
public class ReportContentTests : TestBase
{
    [Test]
    public async Task ShouldCreateReport()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int reportId = await TestApp.SendAsync(new ReportContentCommand(
            TargetId: "some-user-id",
            TargetType: "User",
            Reason: "Inappropriate behaviour in the room."));

        reportId.ShouldBeGreaterThan(0);

        Report? report = await TestApp.FindAsync<Report>(reportId);
        report.ShouldNotBeNull();
        report.TargetType.ShouldBe("User");
        report.Status.ShouldBe(ReportStatus.Pending);
    }

    [Test]
    public async Task ShouldRequireAuthToReport()
    {
        // No user set — IUser.Id is null
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => TestApp.SendAsync(new ReportContentCommand("id", "User", "reason")));
    }

    [Test]
    public async Task AdminShouldGetPendingReports()
    {
        // Create a reporter and report
        string reporterId = await TestApp.RegisterAndSetCurrentUserAsync();
        await TestApp.SendAsync(new ReportContentCommand("target-1", "Post", "Spam content."));

        // Switch to admin
        await TestApp.RunAsAdministratorAsync();

        ReportSummaryDto summary = await TestApp.SendAsync(new GetReportsQuery());

        summary.TotalCount.ShouldBeGreaterThan(0);
        summary.Items[0].TargetType.ShouldBe("Post");
        summary.Items[0].Status.ShouldBe("Pending");
    }

    [Test]
    public async Task NonAdminCannotGetReports()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        await Should.ThrowAsync<ForbiddenAccessException>(
            () => TestApp.SendAsync(new GetReportsQuery()));
    }

    [Test]
    public async Task ShouldValidateEmptyReason()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(new ReportContentCommand("id", "User", "")));
    }
}
