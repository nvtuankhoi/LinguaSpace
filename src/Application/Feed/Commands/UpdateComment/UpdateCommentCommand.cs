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

    public UpdateCommentCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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
    }
}
