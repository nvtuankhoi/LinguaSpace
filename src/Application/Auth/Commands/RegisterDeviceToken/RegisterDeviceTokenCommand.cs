using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Commands.RegisterDeviceToken;

/// <summary>
/// Registers (or refreshes) an FCM push notification token for the current user's device.
///
/// One user can have multiple devices (phone + tablet + web).
/// Upsert strategy: if the FCM token already exists (regardless of user), update LastSeenAt + IsActive.
/// This handles the case where a device was previously registered by a different account.
/// </summary>
[Authorize]
public record RegisterDeviceTokenCommand(
    string FcmToken,
    DevicePlatform Platform) : IRequest;

public class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RegisterDeviceTokenCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        DateTimeOffset now = _timeProvider.GetUtcNow();

        // Try to find an existing device record for this FCM token
        UserDevice? existing = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.FcmToken == request.FcmToken, cancellationToken);

        if (existing is not null)
        {
            // Token already registered — update owner and refresh activity
            existing.UserId = userId;
            existing.Platform = request.Platform;
            existing.LastSeenAt = now;
            existing.IsActive = true;
        }
        else
        {
            _context.UserDevices.Add(new UserDevice
            {
                UserId = userId,
                FcmToken = request.FcmToken,
                Platform = request.Platform,
                LastSeenAt = now,
                IsActive = true,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
