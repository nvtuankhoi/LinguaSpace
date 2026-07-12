using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.UpdateComment;

[Authorize]
public record UpdateCommentCommand(int CommentId, string Content) : IRequest;

public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly INotificationService _notifications;

    public UpdateCommentCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Comment comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), request.CommentId.ToString());

        if (comment.AuthorId != userId)
        {
            throw new ForbiddenAccessException();
        }

        comment.Content = request.Content;
        await _context.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyPostGroupAsync(
            comment.PostId,
            "CommentEdited",
            new { Id = comment.Id, PostId = comment.PostId, Content = comment.Content },
            cancellationToken);
    }
}
