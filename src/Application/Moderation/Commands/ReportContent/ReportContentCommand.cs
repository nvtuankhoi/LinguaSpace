using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Moderation.Commands.ReportContent;

/// <summary>
/// Submits a moderation report against a user, post, room, or message.
/// </summary>
public record ReportContentCommand(
    string TargetId,
    string TargetType,
    string Reason) : IRequest<int>;

public class ReportContentCommandValidator : AbstractValidator<ReportContentCommand>
{
    public ReportContentCommandValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.TargetType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

[Authorize]
public class ReportContentCommandHandler : IRequestHandler<ReportContentCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public ReportContentCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(ReportContentCommand request, CancellationToken cancellationToken)
    {
        string reporterId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Report report = new()
        {
            ReporterId = reporterId,
            TargetId = request.TargetId,
            TargetType = request.TargetType,
            Reason = request.Reason,
            Status = ReportStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.Reports.Add(report);

        await _context.SaveChangesAsync(cancellationToken);

        return report.Id;
    }
}
