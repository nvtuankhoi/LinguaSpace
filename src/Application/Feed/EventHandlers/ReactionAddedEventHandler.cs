using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Feed.EventHandlers;

/// <summary>
/// Handles ReactionAddedEvent: increments LikeCount on the target and notifies the target owner.
/// </summary>
public class ReactionAddedEventHandler : INotificationHandler<ReactionAddedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public ReactionAddedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(ReactionAddedEvent notification, CancellationToken cancellationToken)
    {
        string? targetOwnerId = null;
        Domain.Enums.NotificationType notifType;

        if (notification.TargetType == "Post")
        {
            Post? post = await _context.Posts.FindAsync([notification.TargetId], cancellationToken);
            if (post is null)
            {
                return;
            }

            post.LikeCount++;
            targetOwnerId = post.AuthorId;
            notifType = Domain.Enums.NotificationType.PostLike;
        }
        else
        {
            Comment? comment = await _context.Comments.FindAsync([notification.TargetId], cancellationToken);
            if (comment is null)
            {
                return;
            }

            comment.LikeCount++;
            targetOwnerId = comment.AuthorId;
            notifType = Domain.Enums.NotificationType.CommentLike;
        }

        // Don't notify if user reacted to their own content
        if (targetOwnerId == notification.ReactorId)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        Domain.Entities.Notification notif = new()
        {
            RecipientId = targetOwnerId,
            Type = notifType,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                notification.ReactionId,
                notification.TargetId,
                notification.TargetType,
                SenderId = notification.ReactorId,
            }),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyAsync(
            targetOwnerId,
            "NewReaction",
            new { notification.TargetId, notification.TargetType, SenderId = notification.ReactorId },
            cancellationToken);
    }
}
