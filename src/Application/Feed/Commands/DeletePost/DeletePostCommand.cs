using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.DeletePost;

[Authorize]
public record DeletePostCommand(int PostId) : IRequest;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeletePostCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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
    }
}
