using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Auth.EventHandlers;

/// <summary>
/// Creates a UserXp row when a new user registers.
/// Triggered alongside <see cref="UserRegisteredEventHandler"/> by <see cref="UserRegisteredEvent"/>.
/// MediatR supports multiple handlers for the same notification.
/// </summary>
public class CreateUserXpOnRegisterHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IApplicationDbContext _context;

    public CreateUserXpOnRegisterHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        UserXp xp = new()
        {
            UserId = notification.UserId,
            TotalXp = 0,
            CurrentStreak = 0,
            LongestStreak = 0,
        };

        _context.UserXps.Add(xp);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
