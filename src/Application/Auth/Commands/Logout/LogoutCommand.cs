using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.Logout;

[Authorize]
public record LogoutCommand : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public LogoutCommandHandler(IIdentityService identityService, IUser currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not null)
        {
            // Invalidate the refresh token in DB so the old cookie becomes useless immediately.
            await _identityService.UpdateRefreshTokenAsync(_currentUser.Id, null, null);
        }
    }
}
