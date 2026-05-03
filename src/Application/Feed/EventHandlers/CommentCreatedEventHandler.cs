using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Feed.EventHandlers;

/// <summary>
/// Handles CommentCreatedEvent: increments Post.CommentCount and creates a notification for the post author.
/// </summary>
public class CommentCreatedEventHandler : INotificationHandler<CommentCreatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public CommentCreatedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(CommentCreatedEvent notification, CancellationToken cancellationToken)
    {
        Post? post = await _context.Posts.FindAsync([notification.PostId], cancellationToken);
        if (post is null)
        {
            return;
        }

        post.CommentCount++;

        // Notify the post author (unless they commented on their own post)
        if (post.AuthorId != notification.AuthorId)
        {
            Domain.Entities.Notification notif = new()
            {
                RecipientId = post.AuthorId,
                Type = Domain.Enums.NotificationType.PostComment,
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    notification.CommentId,
                    notification.PostId,
                    SenderId = notification.AuthorId,
                }),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _context.Notifications.Add(notif);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (post.AuthorId != notification.AuthorId)
        {
            await _notificationService.NotifyAsync(
                post.AuthorId,
                "NewComment",
                new { notification.CommentId, notification.PostId, SenderId = notification.AuthorId },
                cancellationToken);
        }
    }
}
