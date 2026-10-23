using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.ResendEmailVerification;

/// <summary>
/// Re-sends the email-verification link to the authenticated user (the
/// "Resend verification email" action in Settings). Generates a fresh token and
/// emails the link; no-op if the address is already confirmed. Mirrors the link
/// format that SendVerificationEmailHandler uses at registration so the FE
/// verify-email page consumes both identically.
/// </summary>
[Authorize]
public record ResendEmailVerificationCommand : IRequest;

public class ResendEmailVerificationCommandHandler : IRequestHandler<ResendEmailVerificationCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IUser _currentUser;

    public ResendEmailVerificationCommandHandler(IIdentityService identityService, IEmailService emailService, IUser currentUser)
    {
        _identityService = identityService;
        _emailService = emailService;
        _currentUser = currentUser;
    }

    public async Task Handle(ResendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (await _identityService.IsEmailConfirmedAsync(userId))
        {
            return;
        }

        // Generate a fresh token and email the verification link. The FE page
        // reads only `token`, but userId is included for parity with the
        // registration email's link shape.
        string token = await _identityService.GenerateEmailVerificationTokenAsync(userId);
        string? email = await _identityService.GetEmailAsync(userId);
        if (email is null)
        {
            return;
        }

        string encodedToken = Uri.EscapeDataString(token);
        string verificationLink =
            $"https://linguaspace.app/verify-email?userId={userId}&token={encodedToken}";

        await _emailService.SendVerificationEmailAsync(email, verificationLink, cancellationToken);
    }
}
