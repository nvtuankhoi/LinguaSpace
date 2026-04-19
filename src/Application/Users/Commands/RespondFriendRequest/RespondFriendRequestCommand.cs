using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.RespondFriendRequest;

[Authorize]
public record RespondFriendRequestCommand(int FriendshipId, bool Accept) : IRequest;

public class RespondFriendRequestCommandHandler : IRequestHandler<RespondFriendRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RespondFriendRequestCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task Handle(RespondFriendRequestCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Friendship friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => f.Id == request.FriendshipId, cancellationToken)
            ?? throw new NotFoundException(nameof(Friendship), request.FriendshipId.ToString());

        // Only the addressee can respond
        if (friendship.AddresseeId != userId)
        {
            throw new ForbiddenAccessException();
        }

        if (friendship.Status != FriendshipStatus.Pending)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.FriendshipId), "This friend request has already been responded to.")
            ]);
        }

        friendship.Status = request.Accept ? FriendshipStatus.Accepted : FriendshipStatus.Declined;
        friendship.UpdatedAt = _timeProvider.GetUtcNow();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
