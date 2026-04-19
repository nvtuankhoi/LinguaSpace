using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.SearchUsers;

[Authorize]
public record SearchUsersQuery(string? Term, string? LanguageCode, int Page = 1, int PageSize = 20)
    : IRequest<IList<UserSummaryDto>>;

public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, IList<UserSummaryDto>>
{
    private const int MaxPageSize = 50;
    private readonly IApplicationDbContext _context;

    public SearchUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<UserSummaryDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        int pageSize = Math.Min(request.PageSize, MaxPageSize);
        int skip = (request.Page - 1) * pageSize;

        IQueryable<UserProfile> query = _context.UserProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            string term = request.Term.ToLower();
            query = query.Where(p => p.DisplayName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            query = query.Where(p =>
                p.Languages.Any(l => l.LanguageCode == request.LanguageCode));
        }

        return await query
            .OrderBy(p => p.DisplayName)
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new UserSummaryDto(p.Id, p.UserId, p.DisplayName, p.AvatarUrl, p.IsOnline))
            .ToListAsync(cancellationToken);
    }
}
