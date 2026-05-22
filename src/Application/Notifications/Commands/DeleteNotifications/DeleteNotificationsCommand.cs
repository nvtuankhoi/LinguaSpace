using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Notifications.Commands.DeleteNotifications;

[Authorize]
public record DeleteNotificationsCommand(IList<int>? NotificationIds = null) : IRequest;

public class DeleteNotificationsCommandHandler : IRequestHandler<DeleteNotificationsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeleteNotificationsCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteNotificationsCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (request.NotificationIds is { Count: > 0 })
        {
            await _context.Notifications
                .Where(n => n.RecipientId == userId && request.NotificationIds.Contains(n.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
