namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// Email sending abstraction. Implemented by ConsoleEmailService (dev) or SendGridEmailService (prod).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link to the user.
    /// </summary>
    Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset link to the user.
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
}
