using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Gamification.Common;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Gamification.EventHandlers;

/// <summary>
/// Awards XP and updates the daily streak when a user joins a room.
/// Triggered by <see cref="UserJoinedRoomEvent"/> alongside the existing presence handler.
/// </summary>
public class AwardXpForRoomJoinHandler : INotificationHandler<UserJoinedRoomEvent>
{
    private readonly IApplicationDbContext _context;

    public AwardXpForRoomJoinHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UserJoinedRoomEvent notification, CancellationToken cancellationToken)
    {
        Room? room = await _context.Rooms.FindAsync([notification.RoomId], cancellationToken);

        if (room is null)
        {
            return;
        }

        int xpAmount = room.RoomType == RoomType.TextOnly
            ? XpConstants.JoinTextRoom
            : XpConstants.JoinVoiceRoom;

        UserXp? userXp = await _context.UserXps
            .FirstOrDefaultAsync(x => x.UserId == notification.UserId, cancellationToken);

        if (userXp is null)
        {
            // Fallback: create UserXp if the registration handler missed it
            userXp = new UserXp { UserId = notification.UserId };
            _context.UserXps.Add(userXp);
        }

        userXp.TotalXp += xpAmount;
        BadgeAwarder.UpdateStreak(userXp);

        _context.XpTransactions.Add(new XpTransaction
        {
            UserId = notification.UserId,
            Amount = xpAmount,
            Reason = room.RoomType == RoomType.TextOnly ? "Joined text room" : "Joined voice room",
            EarnedAt = DateTimeOffset.UtcNow
        });

        await BadgeAwarder.AwardEligibleBadgesAsync(_context, userXp, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
