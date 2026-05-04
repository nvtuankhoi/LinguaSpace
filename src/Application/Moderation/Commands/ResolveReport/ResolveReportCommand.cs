using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Constants;

namespace LinguaSpace.Application.Moderation.Commands.ResolveReport;

/// <summary>
/// Resolves or dismisses a pending moderation report. Admin only.
/// </summary>
/// <param name="ReportId">The report to resolve.</param>
/// <param name="Action">"Resolve" or "Dismiss".</param>
public record ResolveReportCommand(int ReportId, string Action) : IRequest;

public class ResolveReportCommandValidator : AbstractValidator<ResolveReportCommand>
{
    public ResolveReportCommandValidator()
    {
        RuleFor(x => x.ReportId).GreaterThan(0);
        RuleFor(x => x.Action)
            .NotEmpty()
            .Must(a => a is "Resolve" or "Dismiss")
            .WithMessage("Action must be 'Resolve' or 'Dismiss'.");
    }
}

[Authorize(Roles = Roles.Administrator)]
public class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public ResolveReportCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ResolveReportCommand request, CancellationToken cancellationToken)
    {
        Report? report = await _context.Reports.FindAsync([request.ReportId], cancellationToken);

        if (report is null)
        {
            throw new NotFoundException(nameof(Report), request.ReportId);
        }

        report.Status = request.Action == "Dismiss" ? ReportStatus.Dismissed : ReportStatus.Resolved;
        report.ResolvedAt = DateTimeOffset.UtcNow;
        report.ResolvedBy = _currentUser.Id;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
