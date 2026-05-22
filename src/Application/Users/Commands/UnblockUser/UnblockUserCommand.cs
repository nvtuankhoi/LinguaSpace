using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.UnblockUser;

[Authorize]
public record UnblockUserCommand(string TargetUserId) : IRequest;

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UnblockUserCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserBlock? block = await _context.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == userId && b.BlockedId == request.TargetUserId, cancellationToken);

        if (block is not null)
        {
            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
