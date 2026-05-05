using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace LinguaSpace.Application.Feed.EventHandlers;

/// <summary>
/// Handles PostCreatedEvent: fan-out SignalR notifications to followers,
/// and invalidates feed caches for small accounts (&lt;FanOutThreshold followers).
/// For accounts at or above the threshold, feed cache is left stale and refreshed on read (fan-out-on-read).
/// Threshold is configurable via appsettings.json "FeedSettings:FanOutThreshold" (default 500).
/// </summary>
public class PostCreatedEventHandler : INotificationHandler<PostCreatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly int _fanOutThreshold;

    public PostCreatedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        ICacheService cacheService,
        IConfiguration configuration)
    {
        _context = context;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _fanOutThreshold = configuration.GetValue("FeedSettings:FanOutThreshold", 500);
    }

    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        IList<string> followerIds = await _context.Follows
            .Where(f => f.FolloweeId == notification.AuthorId)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);

        bool invalidateCache = followerIds.Count < _fanOutThreshold;

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
