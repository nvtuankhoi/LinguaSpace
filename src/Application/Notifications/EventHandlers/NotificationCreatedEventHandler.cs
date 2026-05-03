using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Notifications.EventHandlers;

/// <summary>
/// When a Notification entity is created, push it to the recipient in real-time via SignalR.
/// If the user is offline, send a FCM push notification to their registered devices.
/// </summary>
public class NotificationCreatedEventHandler : INotificationHandler<NotificationCreatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IPushNotificationService _pushNotificationService;

    public NotificationCreatedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IPushNotificationService pushNotificationService)
    {
        _context = context;
        _notificationService = notificationService;
        _pushNotificationService = pushNotificationService;
    }

    public async Task Handle(NotificationCreatedEvent notification, CancellationToken cancellationToken)
    {
        Domain.Entities.Notification? notif = await _context.Notifications
            .FindAsync([notification.NotificationId], cancellationToken);

        if (notif is null)
        {
            return;
        }

        // Try SignalR first (online user)
        await _notificationService.NotifyAsync(
            notification.RecipientId,
            "Notification",
            new { notif.Id, Type = notif.Type.ToString(), notif.Payload, notif.CreatedAt },
            cancellationToken);

        // Send push if user is offline
        bool isOnline = await _notificationService.IsUserOnlineAsync(notification.RecipientId, cancellationToken);

        if (!isOnline)
        {
            await _pushNotificationService.SendAsync(
                notification.RecipientId,
                title: notif.Type.ToString(),
                body: notif.Payload ?? string.Empty,
                cancellationToken: cancellationToken);
        }
    }
}
