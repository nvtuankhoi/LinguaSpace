using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Moderation.DTOs;
using LinguaSpace.Domain.Constants;

namespace LinguaSpace.Application.Moderation.Queries.GetReports;

/// <summary>Returns a paginated list of moderation reports. Admin only.</summary>
/// <param name="Status">Filter by status name, or null for all pending reports.</param>
public record GetReportsQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<ReportDto>>;

[Authorize(Roles = Roles.Administrator)]
public class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, PaginatedResult<ReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReportsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ReportDto>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Report> query = _context.Reports;

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ReportStatus>(request.Status, ignoreCase: true, out ReportStatus parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }
        else
        {
            // Default: pending reports only
            query = query.Where(r => r.Status == ReportStatus.Pending);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<ReportDto> items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReportDto(
                r.Id,
                r.ReporterId,
                r.TargetId,
                r.TargetType,
                r.Reason,
                r.Status.ToString(),
                r.CreatedAt,
                r.ResolvedAt,
                r.ResolvedBy))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ReportDto>(items, totalCount, request.Page, request.PageSize, (request.Page - 1) * request.PageSize + items.Count < totalCount);
    }
}
