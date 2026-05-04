using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.UnfriendUser;

[Authorize]
public record UnfriendUserCommand(string TargetUserId) : IRequest;

public class UnfriendUserCommandHandler : IRequestHandler<UnfriendUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UnfriendUserCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UnfriendUserCommand request, CancellationToken cancellationToken)
    {
        string callerId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Friendship friendship = await _context.Friendships
            .FirstOrDefaultAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == callerId && f.AddresseeId == request.TargetUserId) ||
                 (f.RequesterId == request.TargetUserId && f.AddresseeId == callerId)),
                cancellationToken)
            ?? throw new NotFoundException(nameof(Friendship), $"{callerId}-{request.TargetUserId}");

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
