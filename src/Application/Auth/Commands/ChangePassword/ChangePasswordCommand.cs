using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.ChangePassword;

[Authorize]
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public ChangePasswordCommandHandler(IIdentityService identityService, IUser currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Result result = await _identityService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword);

        result.ThrowOnFailure();
    }
}
