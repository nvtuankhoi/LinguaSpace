using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Feed.DTOs;
using Microsoft.Extensions.Configuration;

namespace LinguaSpace.Application.Feed.Queries.GetFeed;

/// <summary>
/// Returns a paginated social feed for the current user.
/// Feed = posts by users the current user follows + their own posts.
/// Cursor-based pagination using CreatedAt timestamp.
/// </summary>
[Authorize]
public record GetFeedQuery(
    DateTimeOffset? BeforeCursor,
    int PageSize = 20) : IRequest<IList<PostSummaryDto>>;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, IList<PostSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ICacheService _cache;
    private readonly int _fanOutThreshold;

    private static readonly TimeSpan FeedCacheTtl = TimeSpan.FromMinutes(5);

    public GetFeedQueryHandler(
        IApplicationDbContext context,
        IUser currentUser,
        ICacheService cache,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _cache = cache;
        _fanOutThreshold = configuration.GetValue("FeedSettings:FanOutThreshold", 500);
    }

    public async Task<IList<PostSummaryDto>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        // Only use cache for first page (no cursor) to avoid stale paginated data
        if (request.BeforeCursor is null)
        {
            string cacheKey = $"feed:{userId}";
            IList<PostSummaryDto>? cached = await _cache.GetAsync<IList<PostSummaryDto>>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            IList<PostSummaryDto> results = await FetchFeedAsync(userId, null, request.PageSize, cancellationToken);
            await _cache.SetAsync(cacheKey, results, FeedCacheTtl, cancellationToken);
            return results;
        }

        return await FetchFeedAsync(userId, request.BeforeCursor, request.PageSize, cancellationToken);
    }

    private async Task<IList<PostSummaryDto>> FetchFeedAsync(
        string userId,
        DateTimeOffset? beforeCursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Get IDs of users the current user follows
        IList<string> followingIds = await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FolloweeId)
            .ToListAsync(cancellationToken);

        // Include own posts in feed
        followingIds.Add(userId);

        IQueryable<Post> query = _context.Posts
            .Where(p => followingIds.Contains(p.AuthorId) && !p.IsDeleted)
            .Include(p => p.Tags);

        if (beforeCursor.HasValue)
        {
            query = query.Where(p => p.Created < beforeCursor.Value);
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
