namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Stores FCM push token per device for push notifications.
/// One user can have multiple devices.
/// </summary>
public class UserDevice : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public string FcmToken { get; set; } = string.Empty;

    public DevicePlatform Platform { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public bool IsActive { get; set; } = true;
}
