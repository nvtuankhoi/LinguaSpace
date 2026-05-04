using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;
using System.Text.Json;

namespace LinguaSpace.Application.Feed.EventHandlers;

/// <summary>
/// Handles PostCreatedEvent: fan-out SignalR notifications to followers,
/// and invalidates feed caches for small accounts (&lt;500 followers).
/// For accounts with ≥500 followers, feed cache is left stale and refreshed on read (fan-out-on-read).
/// </summary>
public class PostCreatedEventHandler : INotificationHandler<PostCreatedEvent>
{
    private const int FanOutThreshold = 500;

    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;

    public PostCreatedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        ICacheService cacheService)
    {
        _context = context;
        _notificationService = notificationService;
        _cacheService = cacheService;
    }

    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        IList<string> followerIds = await _context.Follows
            .Where(f => f.FolloweeId == notification.AuthorId)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);

        bool invalidateCache = followerIds.Count < FanOutThreshold;

        foreach (string followerId in followerIds)
        {
            // Real-time signal to all followers regardless of threshold
            await _notificationService.NotifyAsync(
                followerId,
                "NewPost",
                new { notification.PostId, notification.AuthorId },
                cancellationToken);

            // Cache invalidation only for small accounts; large accounts use fan-out-on-read
            if (invalidateCache)
            {
                await _cacheService.RemoveAsync($"feed:{followerId}");
            }
        }
    }
}

