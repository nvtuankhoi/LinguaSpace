using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Social.EventHandlers;

/// <summary>
/// Creates a notification for the addressee when a friend request is sent.
/// </summary>
public class FriendRequestSentEventHandler : INotificationHandler<FriendRequestSentEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public FriendRequestSentEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(FriendRequestSentEvent notification, CancellationToken cancellationToken)
    {
        Domain.Entities.Notification notif = new()
        {
            RecipientId = notification.AddresseeId,
            Type = Domain.Enums.NotificationType.FriendRequest,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                notification.FriendshipId,
                SenderId = notification.RequesterId,
            }),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyAsync(
            notification.AddresseeId,
            "FriendRequest",
            new { notification.FriendshipId, SenderId = notification.RequesterId },
            cancellationToken);
    }
}
