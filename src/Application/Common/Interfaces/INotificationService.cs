namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// Real-time in-app notification service via SignalR.
/// Called when a user is currently connected; for offline users use <see cref="IPushNotificationService"/>.
/// </summary>
public interface INotificationService
{
    /// <summary>Push a notification payload to a specific user over SignalR.</summary>
    Task NotifyAsync(
        string userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);

    /// <summary>Broadcast an event to all members of a SignalR group (e.g., a room group).</summary>
    Task NotifyGroupAsync(
        string groupName,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);

    /// <summary>Broadcast an event to every client currently viewing a feed post
    /// (a PresenceHub group named <c>post-{postId}</c>).</summary>
    Task NotifyPostGroupAsync(
        int postId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);

    /// <summary>Check whether a user currently has an active SignalR connection (presence).</summary>
    Task<bool> IsUserOnlineAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
