using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LinguaSpace.Application.Auth.EventHandlers;

/// <summary>
/// Sends an email verification link after a new user registers.
/// Triggered by UserRegisteredEvent, which is also handled by UserRegisteredEventHandler
/// (which creates the UserProfile). Both handlers run independently.
///
/// Flow:
///   RegisterCommand → UserRegisteredEvent → [UserRegisteredEventHandler creates UserProfile]
///                                         → [SendVerificationEmailHandler sends email]
/// </summary>
public class SendVerificationEmailHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendVerificationEmailHandler> _logger;

    public SendVerificationEmailHandler(
        IIdentityService identityService,
        IEmailService emailService,
        ILogger<SendVerificationEmailHandler> logger)
    {
        _identityService = identityService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            string token = await _identityService.GenerateEmailVerificationTokenAsync(notification.UserId);

            string encodedToken = Uri.EscapeDataString(token);

            // In production, this would link to the Angular frontend verification page.
            string verificationLink =
                $"https://linguaspace.app/verify-email?userId={notification.UserId}&token={encodedToken}";

            await _emailService.SendVerificationEmailAsync(
                notification.Email,
                verificationLink,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't fail registration if email sending fails — log and continue.
            // The user can request a new verification email later.
            _logger.LogError(ex, "Failed to send verification email to {Email}.", notification.Email);
        }
    }
}
