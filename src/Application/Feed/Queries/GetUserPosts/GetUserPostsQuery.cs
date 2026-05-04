using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetUserPosts;

/// <summary>
/// Returns posts authored by a specific user, cursor-paginated by Created DESC.
/// </summary>
public record GetUserPostsQuery(
    string UserId,
    DateTimeOffset? BeforeCursor,
    int PageSize = 20) : IRequest<IList<PostSummaryDto>>;

public class GetUserPostsQueryHandler : IRequestHandler<GetUserPostsQuery, IList<PostSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserPostsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<PostSummaryDto>> Handle(
        GetUserPostsQuery request,
        CancellationToken cancellationToken)
    {
        int pageSize = Math.Clamp(request.PageSize, 1, 50);

        IQueryable<Post> query = _context.Posts
            .Where(p => p.AuthorId == request.UserId && !p.IsDeleted)
            .Include(p => p.Tags);

        if (request.BeforeCursor.HasValue)
        {
            query = query.Where(p => p.Created < request.BeforeCursor.Value);
        }

        return await query
            .OrderByDescending(p => p.Created)
            .Take(pageSize)
            .Select(p => new PostSummaryDto(
                p.Id,
                p.AuthorId,
                p.Content,
                p.PostType.ToString(),
                p.LanguageCode,
                p.Metadata,
                p.LikeCount,
                p.CommentCount,
                p.Created,
                p.Tags.Select(t => t.Tag).ToList()))
            .ToListAsync(cancellationToken);
    }
}
