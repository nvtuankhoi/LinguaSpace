using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.UnfollowUser;

[Authorize]
public record UnfollowUserCommand(string FolloweeId) : IRequest;

public class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UnfollowUserCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
    {
        string followerId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Follow follow = await _context.Follows
            .FirstOrDefaultAsync(
                f => f.FollowerId == followerId && f.FolloweeId == request.FolloweeId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Follow), request.FolloweeId);

        _context.Follows.Remove(follow);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
