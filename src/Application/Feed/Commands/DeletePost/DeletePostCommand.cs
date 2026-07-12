using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.DeletePost;

[Authorize]
public record DeletePostCommand(int PostId) : IRequest;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly INotificationService _notifications;

    public DeletePostCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Post post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Post), request.PostId.ToString());

        if (post.AuthorId != userId)
        {
            throw new ForbiddenAccessException();
        }

        post.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyPostGroupAsync(
            post.Id,
            "PostDeleted",
            new { Id = post.Id },
            cancellationToken);
    }
}
