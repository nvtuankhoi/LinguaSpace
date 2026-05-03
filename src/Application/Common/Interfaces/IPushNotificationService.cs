namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// Push notification service for reaching users when they are offline (app in background/closed).
/// Implemented via Firebase Cloud Messaging (FCM) in Infrastructure.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>Send a push notification to all registered devices of a single user.</summary>
    Task SendAsync(
        string userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>Batch send the same notification to multiple users (uses FCM multicast).</summary>
    Task SendMulticastAsync(
        IEnumerable<string> userIds,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
