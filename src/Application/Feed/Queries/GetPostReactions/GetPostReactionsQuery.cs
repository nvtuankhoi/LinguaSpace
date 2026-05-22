using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetPostReactions;

[Authorize]
public record GetPostReactionsQuery(int PostId, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<ReactionDetailDto>>;

public class GetPostReactionsQueryHandler : IRequestHandler<GetPostReactionsQuery, PaginatedResult<ReactionDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPostReactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ReactionDetailDto>> Handle(
        GetPostReactionsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Reaction> query = _context.Reactions.AsNoTracking()
            .Where(r => r.TargetType == ReactionTargetType.Post && r.TargetId == request.PostId)
            .OrderByDescending(r => r.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);

        List<Reaction> pageReactions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        List<string> userIds = pageReactions.Select(r => r.UserId).Distinct().ToList();

        Dictionary<string, UserProfile> profiles = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        IList<ReactionDetailDto> items = pageReactions
            .Select(r =>
            {
                profiles.TryGetValue(r.UserId, out UserProfile? profile);
                return new ReactionDetailDto(
                    r.UserId,
                    profile?.DisplayName ?? r.UserId,
                    profile?.AvatarUrl,
                    r.Type.ToString(),
                    r.CreatedAt);
            })
            .ToList();

        bool hasMore = (request.Page - 1) * request.PageSize + items.Count < totalCount;
        return new PaginatedResult<ReactionDetailDto>(items, totalCount, request.Page, request.PageSize, hasMore);
    }
}
