using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.EventHandlers;

/// <summary>
/// When a user leaves a room:
/// - Update their presence (LastSeenAt).
/// - If the user was the host, close the room and evict all remaining participants.
/// </summary>
public class UserLeftRoomEventHandler : INotificationHandler<UserLeftRoomEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public UserLeftRoomEventHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UserLeftRoomEvent notification, CancellationToken cancellationToken)
    {
        // Update presence
        UserProfile? profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is not null)
        {
            profile.LastSeenAt = _timeProvider.GetUtcNow();
            profile.IsOnline = false;
        }

        // If host left, close the room and remove remaining participants
        if (notification.WasHost)
        {
            Room? room = await _context.Rooms
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == notification.RoomId, cancellationToken);

            if (room is not null && room.Status == RoomStatus.Active)
            {
                room.Status = RoomStatus.Closed;
                room.Participants.Clear();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
