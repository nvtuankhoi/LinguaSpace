using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.DeleteComment;

[Authorize]
public record DeleteCommentCommand(int CommentId) : IRequest;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeleteCommentCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Comment comment = await _context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), request.CommentId.ToString());

        // Allow comment author or post author to delete
        if (comment.AuthorId != userId && comment.Post.AuthorId != userId)
        {
            throw new ForbiddenAccessException();
        }

        comment.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
