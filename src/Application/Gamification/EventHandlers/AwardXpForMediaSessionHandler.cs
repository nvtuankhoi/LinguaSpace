using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Gamification.Common;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Gamification.EventHandlers;

/// <summary>
/// Awards XP based on voice session duration when a user leaves a media room.
/// Triggered by <see cref="ParticipantLeftMediaEvent"/>.
/// XP = clamp(durationSeconds / 60 * VoiceXpPerMinute, VoiceSessionMinXp, VoiceSessionMaxXp).
/// </summary>
public class AwardXpForMediaSessionHandler : INotificationHandler<ParticipantLeftMediaEvent>
{
    private readonly IApplicationDbContext _context;

    public AwardXpForMediaSessionHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ParticipantLeftMediaEvent notification, CancellationToken cancellationToken)
    {
        RoomMediaSession? session = await _context.RoomMediaSessions
            .FirstOrDefaultAsync(s => s.Id == notification.MediaSessionId, cancellationToken);

        if (session is null || session.DurationSeconds is null or 0)
        {
            return;
        }

        int minutes = session.DurationSeconds.Value / 60;
        int xpAmount = Math.Clamp(
            minutes * XpConstants.VoiceXpPerMinute,
            XpConstants.VoiceSessionMinXp,
            XpConstants.VoiceSessionMaxXp);

        UserXp? userXp = await _context.UserXps
            .FirstOrDefaultAsync(x => x.UserId == notification.UserId, cancellationToken);

        if (userXp is null)
        {
            userXp = new UserXp { UserId = notification.UserId };
            _context.UserXps.Add(userXp);
        }

        userXp.TotalXp += xpAmount;
        BadgeAwarder.UpdateStreak(userXp);

        _context.XpTransactions.Add(new XpTransaction
        {
            UserId = notification.UserId,
            Amount = xpAmount,
            Reason = $"Voice session ({minutes} min)",
            EarnedAt = DateTimeOffset.UtcNow
        });

        await BadgeAwarder.AwardEligibleBadgesAsync(_context, userXp, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
