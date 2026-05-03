using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Feed.Commands.CreateComment;

public record CreateCommentCommand(
    int PostId,
    string Content,
    int? ParentCommentId) : IRequest<int>;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}

[Authorize]
public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateCommentCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        string authorId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Post post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Post), request.PostId.ToString());

        if (request.ParentCommentId.HasValue)
        {
            bool parentExists = await _context.Comments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value && c.PostId == request.PostId && !c.IsDeleted,
                    cancellationToken);
            if (!parentExists)
            {
                throw new NotFoundException(nameof(Comment), request.ParentCommentId.Value.ToString());
            }
        }

        Comment comment = new()
        {
            PostId = request.PostId,
            AuthorId = authorId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId,
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        comment.AddDomainEvent(new CommentCreatedEvent(comment.Id, request.PostId, authorId));
        await _context.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }
}
