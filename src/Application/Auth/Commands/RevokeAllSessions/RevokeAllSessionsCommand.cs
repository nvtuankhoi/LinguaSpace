using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.RevokeAllSessions;

[Authorize]
public record RevokeAllSessionsCommand : IRequest;

public class RevokeAllSessionsCommandHandler : IRequestHandler<RevokeAllSessionsCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public RevokeAllSessionsCommandHandler(IIdentityService identityService, IUser currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task Handle(RevokeAllSessionsCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        await _identityService.UpdateRefreshTokenAsync(userId, null, null);
    }
}
