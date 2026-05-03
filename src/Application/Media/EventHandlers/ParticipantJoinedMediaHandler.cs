using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Media.EventHandlers;

/// <summary>
/// Creates a <see cref="RoomMediaSession"/> row and broadcasts to the room via SignalR.
/// Triggered by <see cref="ParticipantJoinedMediaEvent"/> (raised from the LiveKit webhook handler).
/// </summary>
public class ParticipantJoinedMediaHandler : INotificationHandler<ParticipantJoinedMediaEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public ParticipantJoinedMediaHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(ParticipantJoinedMediaEvent notification, CancellationToken cancellationToken)
    {
        // Avoid duplicates if webhook fires twice
        bool already = await _context.RoomMediaSessions
            .AnyAsync(
                s => s.RoomId == notification.RoomId
                  && s.UserId == notification.UserId
                  && s.LeftAt == null,
                cancellationToken);

        if (!already)
        {
            RoomMediaSession session = new()
            {
                RoomId = notification.RoomId,
                UserId = notification.UserId,
                JoinedAt = DateTimeOffset.UtcNow,
            };

            _context.RoomMediaSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Broadcast to all SignalR clients in the room
        await _notificationService.NotifyGroupAsync(
            $"room-{notification.RoomId}",
            "UserJoinedMedia",
            new { notification.RoomId, notification.UserId },
            cancellationToken);
    }
}
