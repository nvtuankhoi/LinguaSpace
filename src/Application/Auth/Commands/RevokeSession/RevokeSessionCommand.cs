using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.RevokeSession;

[Authorize]
public record RevokeSessionCommand(int SessionId) : IRequest;

public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public RevokeSessionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserSession? session = await _context.UserSessions
            .FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            throw new NotFoundException(nameof(UserSession), request.SessionId);
        }

        session.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
