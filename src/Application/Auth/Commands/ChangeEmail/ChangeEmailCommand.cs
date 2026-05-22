using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.ChangeEmail;

[Authorize]
public record ChangeEmailCommand(string NewEmail) : IRequest;

public class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public ChangeEmailCommandHandler(IIdentityService identityService, IUser currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Result result = await _identityService.ChangeEmailAsync(userId, request.NewEmail);

        result.ThrowOnFailure();
    }
}
