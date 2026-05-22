using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.ResendEmailVerification;

[Authorize]
public record ResendEmailVerificationCommand : IRequest;

public class ResendEmailVerificationCommandHandler : IRequestHandler<ResendEmailVerificationCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public ResendEmailVerificationCommandHandler(IIdentityService identityService, IUser currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task Handle(ResendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (await _identityService.IsEmailConfirmedAsync(userId))
        {
            return;
        }

        await _identityService.GenerateEmailVerificationTokenAsync(userId);
    }
}
