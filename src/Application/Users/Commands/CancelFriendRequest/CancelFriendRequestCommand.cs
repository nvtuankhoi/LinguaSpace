using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.CancelFriendRequest;

[Authorize]
public record CancelFriendRequestCommand(int RequestId) : IRequest;

public class CancelFriendRequestCommandHandler : IRequestHandler<CancelFriendRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CancelFriendRequestCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelFriendRequestCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Friendship friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => f.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(Friendship), request.RequestId.ToString());

        if (friendship.Status != FriendshipStatus.Pending)
        {
            throw new NotFoundException(nameof(Friendship), request.RequestId.ToString());
        }

        if (friendship.RequesterId != userId)
        {
            throw new ForbiddenAccessException();
        }

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
