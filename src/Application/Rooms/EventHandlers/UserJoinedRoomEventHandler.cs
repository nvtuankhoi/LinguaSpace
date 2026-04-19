using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.EventHandlers;

/// <summary>
/// When a user joins a room, mark them as online in their UserProfile.
/// SignalR notification to room members is handled in Phase 4 (Infrastructure hub).
/// </summary>
public class UserJoinedRoomEventHandler : INotificationHandler<UserJoinedRoomEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public UserJoinedRoomEventHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UserJoinedRoomEvent notification, CancellationToken cancellationToken)
    {
        UserProfile? profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is null)
        {
            return;
        }

        profile.IsOnline = true;
        profile.LastSeenAt = _timeProvider.GetUtcNow();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
