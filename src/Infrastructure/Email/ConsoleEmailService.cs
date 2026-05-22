using LinguaSpace.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LinguaSpace.Infrastructure.Email;

/// <summary>
/// Development stub for IEmailService — logs email content instead of sending.
/// Replace with SendGridEmailService in production (Phase 2).
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(
        string toEmail,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Verification email to {Email}\nLink: {Link}",
            toEmail,
            verificationLink);

        return Task.CompletedTask;  
    }

    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Password reset email to {Email}\nLink: {Link}",
            toEmail,
            resetLink);

        return Task.CompletedTask;
    }
}
