using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Auth.EventHandlers;

/// <summary>
/// Creates a UserProfile when a new user registers.
/// Triggered by UserRegisteredEvent published from RegisterCommandHandler.
/// </summary>
public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IApplicationDbContext _context;

    public UserRegisteredEventHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        string displayName = notification.Email.Split('@')[0];

        UserProfile profile = new()
        {
            UserId = notification.UserId,
            DisplayName = displayName,
        };

        _context.UserProfiles.Add(profile);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
