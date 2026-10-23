using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.EventHandlers;

/// <summary>
/// When a room message is deleted: broadcast a MessageDeleted SignalR event to the
/// room group so every participant's chat reflects the deletion live.
/// </summary>
public class RoomMessageDeletedEventHandler : INotificationHandler<RoomMessageDeletedEvent>
{
    private readonly INotificationService _notificationService;

    public RoomMessageDeletedEventHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task Handle(RoomMessageDeletedEvent notification, CancellationToken cancellationToken)
    {
        return _notificationService.NotifyGroupAsync(
            $"room-{notification.RoomId}",
            "MessageDeleted",
            new { notification.RoomId, notification.MessageId },
            cancellationToken);
    }
}
