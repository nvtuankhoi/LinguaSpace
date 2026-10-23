using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetUserPosts;

/// <summary>
/// Returns posts authored by a specific user, cursor-paginated by Created DESC.
/// </summary>
public record GetUserPostsQuery(
    string UserId,
    DateTimeOffset? BeforeCursor,
    int PageSize = 20) : IRequest<CursorPagedResult<PostSummaryDto>>;

public class GetUserPostsQueryHandler : IRequestHandler<GetUserPostsQuery, CursorPagedResult<PostSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserPostsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CursorPagedResult<PostSummaryDto>> Handle(
        GetUserPostsQuery request,
        CancellationToken cancellationToken)
    {
        int pageSize = Math.Clamp(request.PageSize, 1, 50);

        IQueryable<Post> query = _context.Posts
            .Where(p => p.AuthorId == request.UserId && !p.IsDeleted)
            .Include(p => p.Tags)
            .Include(p => p.MediaItems);

        if (request.BeforeCursor.HasValue)
        {
            query = query.Where(p => p.Created < request.BeforeCursor.Value);
        }

        IList<Post> rawPosts = await query
            .OrderByDescending(p => p.Created)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        IList<PostSummaryDto> raw = rawPosts
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

        bool hasMore = raw.Count > pageSize;
        IList<PostSummaryDto> items = hasMore ? raw.Take(pageSize).ToList() : raw;
        DateTimeOffset? nextCursor = hasMore ? items[^1].CreatedAt : null;

        return new CursorPagedResult<PostSummaryDto>(items, hasMore, nextCursor);
    }
}
