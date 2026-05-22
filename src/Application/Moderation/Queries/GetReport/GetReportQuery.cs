using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Moderation.DTOs;
using LinguaSpace.Domain.Constants;

namespace LinguaSpace.Application.Moderation.Queries.GetReport;

[Authorize(Roles = Roles.Administrator)]
public record GetReportQuery(int ReportId) : IRequest<ReportDto>;

public class GetReportQueryHandler : IRequestHandler<GetReportQuery, ReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportDto> Handle(GetReportQuery request, CancellationToken cancellationToken)
    {
        Report report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken)
            ?? throw new NotFoundException(nameof(Report), request.ReportId);

        return new ReportDto(
            report.Id,
            report.ReporterId,
            report.TargetId,
            report.TargetType,
            report.Reason,
            report.Status.ToString(),
            report.CreatedAt,
            report.ResolvedAt,
            report.ResolvedBy);
    }
}
