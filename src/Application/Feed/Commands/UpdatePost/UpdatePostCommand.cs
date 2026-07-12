using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.UpdatePost;

public record UpdatePostCommand(
    int PostId,
    string Content,
    string? LanguageCode) : IRequest;

public class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

[Authorize]
public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly INotificationService _notifications;

    public UpdatePostCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Post post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Post), request.PostId.ToString());

        if (post.AuthorId != userId)
        {
            throw new ForbiddenAccessException();
        }

        post.Content = request.Content;
        post.LanguageCode = request.LanguageCode;

        await _context.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyPostGroupAsync(
            post.Id,
            "PostEdited",
            new { Id = post.Id, Content = post.Content, LanguageCode = post.LanguageCode },
            cancellationToken);
    }
}
