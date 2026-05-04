using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetExplore;

/// <summary>
/// Returns all public (non-deleted) posts for the Explore feed.
/// No authentication required. Cursor-based pagination by Created DESC.
/// </summary>
/// <param name="LanguageCode">Optional filter by language code (ISO 639-1).</param>
/// <param name="PostType">Optional filter by post type name (Text, VocabCard, QuestionAnswer).</param>
/// <param name="BeforeCursor">Cursor for next page (Created timestamp of last item).</param>
/// <param name="PageSize">Number of posts to return (1–50, default 20).</param>
public record GetExploreQuery(
    string? LanguageCode,
    string? PostType,
    DateTimeOffset? BeforeCursor,
    int PageSize = 20) : IRequest<IList<PostSummaryDto>>;

public class GetExploreQueryHandler : IRequestHandler<GetExploreQuery, IList<PostSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetExploreQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<PostSummaryDto>> Handle(
        GetExploreQuery request,
        CancellationToken cancellationToken)
    {
        int pageSize = Math.Clamp(request.PageSize, 1, 50);

        IQueryable<Post> query = _context.Posts
            .Where(p => !p.IsDeleted)
            .Include(p => p.Tags);

        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            query = query.Where(p => p.LanguageCode == request.LanguageCode);
        }

        if (!string.IsNullOrWhiteSpace(request.PostType) &&
            Enum.TryParse<PostType>(request.PostType, ignoreCase: true, out PostType postType))
        {
            query = query.Where(p => p.PostType == postType);
        }

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
