using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.EventHandlers;

/// <summary>
/// When a user joins a room: mark them online, then broadcast a UserJoinedRoom
/// SignalR event to the room group so other members' participant lists update live.
/// </summary>
public class UserJoinedRoomEventHandler : INotificationHandler<UserJoinedRoomEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public UserJoinedRoomEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        TimeProvider timeProvider)
    {
        _context = context;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UserJoinedRoomEvent notification, CancellationToken cancellationToken)
    {
        UserProfile? profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is not null)
        {
            profile.IsOnline = true;
            profile.LastSeenAt = _timeProvider.GetUtcNow();
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Fan out to everyone connected to the room so participant lists refresh.
        await _notificationService.NotifyGroupAsync(
            $"room-{notification.RoomId}",
            "UserJoinedRoom",
            new { notification.RoomId, notification.UserId, notification.Role },
            cancellationToken);
    }
}
