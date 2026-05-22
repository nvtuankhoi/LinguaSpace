using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.BlockUser;

[Authorize]
public record BlockUserCommand(string TargetUserId) : IRequest;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public BlockUserCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        bool alreadyBlocked = await _context.UserBlocks
            .AnyAsync(b => b.BlockerId == userId && b.BlockedId == request.TargetUserId, cancellationToken);

        if (alreadyBlocked)
        {
            return;
        }

        _context.UserBlocks.Add(new UserBlock
        {
            BlockerId = userId,
            BlockedId = request.TargetUserId,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
