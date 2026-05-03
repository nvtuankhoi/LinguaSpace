using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;
using System.Text.Json;

namespace LinguaSpace.Application.Feed.EventHandlers;

/// <summary>
/// Handles PostCreatedEvent: creates notifications for the author's followers.
/// XP awards can be added here in a future iteration.
/// </summary>
public class PostCreatedEventHandler : INotificationHandler<PostCreatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public PostCreatedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Notify followers (fan-out-on-write for small accounts; read-time for large — handled by query)
        IList<string> followerIds = await _context.Follows
            .Where(f => f.FolloweeId == notification.AuthorId)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);

        string payload = JsonSerializer.Serialize(new { notification.PostId, notification.AuthorId });

        foreach (string followerId in followerIds)
        {
            // Invalidate the follower's feed cache so next read fetches fresh data
            await _notificationService.NotifyAsync(
                followerId,
                "NewPost",
                new { notification.PostId, notification.AuthorId },
                cancellationToken);
        }
    }
}
