using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.SearchPosts;

/// <summary>
/// Search posts by keyword, using <b>offset-based pagination</b>.
/// </summary>
/// <remarks>
/// Offset pagination is intentional here: search results represent a static ranked snapshot
/// at query time, so page 2 reliably follows page 1 even as new posts are created.
/// The social feed endpoints (GetFeed, GetExplore, GetUserPosts) use cursor pagination instead
/// because they are live streams where new content is continuously inserted at the top.
/// </remarks>
public record SearchPostsQuery(string Q, int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<PostSummaryDto>>;

public class SearchPostsQueryHandler : IRequestHandler<SearchPostsQuery, PaginatedResult<PostSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchPostsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<PostSummaryDto>> Handle(
        SearchPostsQuery request,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 50);
        int skip = (page - 1) * pageSize;
        string queryText = request.Q.Trim();

        IQueryable<Post> query = _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Tags)
            .Include(p => p.MediaItems);

        if (!string.IsNullOrWhiteSpace(queryText))
        {
            string normalizedQuery = queryText.ToLower();
            query = query.Where(p => p.Content.ToLower().Contains(normalizedQuery));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        IList<Post> rawPosts = await query
            .OrderByDescending(p => p.Created)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        IList<PostSummaryDto> items = rawPosts
            .Select(p => new PostSummaryDto(
                p.Id,
                p.AuthorId,
                p.Content,
                p.PostType.ToString(),
                p.LanguageCode,
                PostMetadataDto.Deserialize(p.Metadata),
                p.LikeCount,
                p.CommentCount,
                p.Created,
                p.Tags.Select(t => t.Tag).ToList(),
                p.MediaItems.OrderBy(m => m.SortOrder).Select(m => new MediaItemDto(m.Id, m.Url, m.SortOrder)).ToList()))
            .ToList();

        return new PaginatedResult<PostSummaryDto>(
            items,
            totalCount,
            page,
            pageSize,
            skip + items.Count < totalCount);
    }
}
