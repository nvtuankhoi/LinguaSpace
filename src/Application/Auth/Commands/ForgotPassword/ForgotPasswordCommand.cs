using LinguaSpace.Application.Common.Interfaces;

namespace LinguaSpace.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Initiates the password reset flow by sending a reset link to the user's email.
///
/// Security note: Always returns success even if the email does not exist —
/// this prevents email enumeration attacks (attackers can't tell if an address is registered).
///
/// No [Authorize] needed — user is unauthenticated when they forgot their password.
/// </summary>
public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(IIdentityService identityService, IEmailService emailService)
    {
        _identityService = identityService;
        _emailService = emailService;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // GeneratePasswordResetTokenAsync returns null if email not found — we silently ignore.
        (string Token, string UserId)? result =
            await _identityService.GeneratePasswordResetTokenAsync(request.Email);

        if (result is null)
        {
            // Don't throw — this is intentional silent failure for security.
            return;
        }

        // Build reset link. In production this would be the frontend URL.
        // The userId is encoded in the link so the client can pass it to ResetPasswordCommand.
        string encodedToken = Uri.EscapeDataString(result.Value.Token);
        string resetLink = $"https://linguaspace.app/reset-password?userId={result.Value.UserId}&token={encodedToken}";

        await _emailService.SendPasswordResetEmailAsync(request.Email, resetLink, cancellationToken);
    }
}
