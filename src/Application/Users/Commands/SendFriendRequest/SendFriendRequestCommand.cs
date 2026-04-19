using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.SendFriendRequest;

[Authorize]
public record SendFriendRequestCommand(string AddresseeId) : IRequest<int>;

public class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SendFriendRequestCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<int> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        string requesterId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (requesterId == request.AddresseeId)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.AddresseeId), "Cannot send a friend request to yourself.")
            ]);
        }

        bool exists = await _context.Friendships.AnyAsync(
            f => (f.RequesterId == requesterId && f.AddresseeId == request.AddresseeId)
              || (f.RequesterId == request.AddresseeId && f.AddresseeId == requesterId),
            cancellationToken);

        if (exists)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.AddresseeId), "A friend relationship or request already exists.")
            ]);
        }

        Friendship friendship = new()
        {
            RequesterId = requesterId,
            AddresseeId = request.AddresseeId,
            Status = FriendshipStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow(),
        };

        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync(cancellationToken);

        return friendship.Id;
    }
}
