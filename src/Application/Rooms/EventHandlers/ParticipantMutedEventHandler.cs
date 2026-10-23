using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.EventHandlers;

/// <summary>
/// When a host mutes/unmutes a participant: broadcast a ParticipantMuted SignalR
/// event to the room group so every client (including the affected user) updates
/// the participant's mute state live.
/// </summary>
public class ParticipantMutedEventHandler : INotificationHandler<ParticipantMutedEvent>
{
    private readonly INotificationService _notificationService;

    public ParticipantMutedEventHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task Handle(ParticipantMutedEvent notification, CancellationToken cancellationToken)
    {
        return _notificationService.NotifyGroupAsync(
            $"room-{notification.RoomId}",
            "ParticipantMuted",
            new { notification.RoomId, notification.UserId, notification.IsMuted },
            cancellationToken);
    }
}
