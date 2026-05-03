using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace LinguaSpace.Infrastructure.Notifications;

/// <summary>
/// Implements <see cref="INotificationService"/> using SignalR.
/// User-targeted notifications go via <see cref="IHubContext{PresenceHub}"/>.
/// Room group notifications go via <see cref="IHubContext{RoomHub}"/>.
/// Online presence is checked via a Redis key set by <see cref="PresenceHub"/>.
/// </summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<PresenceHub> _presenceHub;
    private readonly IHubContext<RoomHub> _roomHub;
    private readonly IConnectionMultiplexer _redis;

    public SignalRNotificationService(
        IHubContext<PresenceHub> presenceHub,
        IHubContext<RoomHub> roomHub,
        IConnectionMultiplexer redis)
    {
        _presenceHub = presenceHub;
        _roomHub = roomHub;
        _redis = redis;
    }

    public Task NotifyAsync(
        string userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        return _presenceHub.Clients.User(userId).SendAsync(eventName, payload, cancellationToken);
    }

    public Task NotifyGroupAsync(
        string groupName,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        return _roomHub.Clients.Group(groupName).SendAsync(eventName, payload, cancellationToken);
    }

    public async Task<bool> IsUserOnlineAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        IDatabase db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"presence:{userId}");
    }
}
