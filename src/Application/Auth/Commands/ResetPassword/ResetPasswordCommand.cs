using LinguaSpace.Application.Common.Interfaces;

namespace LinguaSpace.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Resets the user's password using the token received via email (from ForgotPasswordCommand).
///
/// The client must pass userId (from the reset link) + token + new password.
/// No [Authorize] — user is not authenticated when resetting.
/// </summary>
public record ResetPasswordCommand(
    string UserId,
    string Token,
    string NewPassword) : IRequest;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        Result result = await _identityService.ResetPasswordAsync(
            request.UserId,
            request.Token,
            request.NewPassword);

        result.ThrowOnFailure();
    }
}
