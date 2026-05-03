using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Media.EventHandlers;

/// <summary>
/// Finalizes the <see cref="RoomMediaSession"/> (sets LeftAt, computes DurationSeconds).
/// Triggered by <see cref="ParticipantLeftMediaEvent"/>.
/// </summary>
public class ParticipantLeftMediaHandler : INotificationHandler<ParticipantLeftMediaEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public ParticipantLeftMediaHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(ParticipantLeftMediaEvent notification, CancellationToken cancellationToken)
    {
        RoomMediaSession? session = await _context.RoomMediaSessions
            .FirstOrDefaultAsync(s => s.Id == notification.MediaSessionId, cancellationToken);

        if (session is not null && session.LeftAt is null)
        {
            DateTimeOffset leftAt = DateTimeOffset.UtcNow;
            session.LeftAt = leftAt;
            session.DurationSeconds = (int)(leftAt - session.JoinedAt).TotalSeconds;

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _notificationService.NotifyGroupAsync(
            $"room-{notification.RoomId}",
            "UserLeftMedia",
            new { notification.RoomId, notification.UserId },
            cancellationToken);
    }
}
