using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.FollowUser;

[Authorize]
public record FollowUserCommand(string FolloweeId) : IRequest<int>;

public class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public FollowUserCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<int> Handle(FollowUserCommand request, CancellationToken cancellationToken)
    {
        string followerId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (followerId == request.FolloweeId)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.FolloweeId), "Cannot follow yourself.")
            ]);
        }

        bool alreadyFollowing = await _context.Follows.AnyAsync(
            f => f.FollowerId == followerId && f.FolloweeId == request.FolloweeId,
            cancellationToken);

        if (alreadyFollowing)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.FolloweeId), "Already following this user.")
            ]);
        }

        Follow follow = new()
        {
            FollowerId = followerId,
            FolloweeId = request.FolloweeId,
            FollowedAt = _timeProvider.GetUtcNow(),
        };

        _context.Follows.Add(follow);
        await _context.SaveChangesAsync(cancellationToken);

        return follow.Id;
    }
}
