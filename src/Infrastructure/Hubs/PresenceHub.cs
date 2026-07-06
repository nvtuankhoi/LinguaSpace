using LinguaSpace.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LinguaSpace.Infrastructure.Hubs;

/// <summary>
/// Tracks user online presence via SignalR connection lifecycle.
///
/// Strategy: Redis SET "presence:{userId}" = "1" with TTL 5 min on connect.
/// A background job or heartbeat renews it. On disconnect, DEL the key.
/// This is more reliable than DB writes on every connect/disconnect (race conditions).
///
/// For MVP: we write directly to DB via IApplicationDbContext.
/// Redis presence key is a fast lookup for "is user online right now?"
/// </summary>
[Authorize]
public class PresenceHub : Hub
{
    private readonly IUser _user;
    private readonly IConnectionMultiplexer _redis;
    private readonly IApplicationDbContext _context;

    public PresenceHub(IUser user, IConnectionMultiplexer redis, IApplicationDbContext context)
    {
        _user = user;
        _redis = redis;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        if (_user.Id is not null)
        {
            IDatabase db = _redis.GetDatabase();
            // Redis key acts as fast presence lookup (TTL = 10 minutes, renewed by heartbeat)
            await db.StringSetAsync(PresenceKey(_user.Id), "1", TimeSpan.FromMinutes(10));

            await UpdateDbPresenceAsync(_user.Id, isOnline: true);

            // Notify others that this user is online
            await Clients.Others.SendAsync("UserOnline", _user.Id);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_user.Id is not null)
        {
            IDatabase db = _redis.GetDatabase();

            // Only mark offline if this was the last connection for this user.
            // Multiple browser tabs = multiple connections.
            // For simplicity in MVP, we mark offline on any disconnect.
            await db.KeyDeleteAsync(PresenceKey(_user.Id));

            await UpdateDbPresenceAsync(_user.Id, isOnline: false);

            await Clients.Others.SendAsync("UserOffline", _user.Id);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ─── Client-callable methods ─────────────────────────────────────────────

    /// <summary>
    /// Called by clients every ~3 minutes to renew the Redis presence TTL.
    /// This prevents the key from expiring while the user is still connected.
    /// </summary>
    public async Task Heartbeat()
    {
        if (_user.Id is not null)
        {
            IDatabase db = _redis.GetDatabase();
            await db.KeyExpireAsync(PresenceKey(_user.Id), TimeSpan.FromMinutes(10));
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task UpdateDbPresenceAsync(string userId, bool isOnline)
    {
        // UserProfile's primary key is the int BaseAuditableEntity.Id; the Identity
        // user id lives on the separate UserId (string) property. FindAsync keys on
        // the PK, so look up by UserId instead (same pattern as GetCurrentUserQuery).
        UserProfile? profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, CancellationToken.None);

        if (profile is null)
        {
            return;
        }

        profile.IsOnline = isOnline;
        profile.LastSeenAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(CancellationToken.None);
    }

    private static string PresenceKey(string userId) => $"presence:{userId}";
}
