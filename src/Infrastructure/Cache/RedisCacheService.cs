using System.Text.Json;
using LinguaSpace.Application.Common.Interfaces;
using StackExchange.Redis;

namespace LinguaSpace.Infrastructure.Cache;

/// <summary>
/// Redis-backed implementation of ICacheService using StackExchange.Redis.
///
/// Serialization: System.Text.Json (fast, no dependencies).
/// Keys follow the pattern: "lingua:{feature}:{id}", e.g. "lingua:rooms:42"
/// </summary>
public class RedisCacheService : ICacheService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisCacheService(IConnectionMultiplexer multiplexer)
    {
        // GetDatabase() returns a lightweight proxy — no extra connection is opened.
        // The multiplexer manages the actual TCP connection pool to Redis.
        _db = multiplexer.GetDatabase();
        _multiplexer = multiplexer;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        RedisValue value = await _db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return default;
        }

        // Deserialize from JSON. If the stored value is malformed, return default instead of throwing.
        return JsonSerializer.Deserialize<T>((string)value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(value);

        // StringSetAsync maps to Redis SET key value EX <seconds>
        await _db.StringSetAsync(key, json, expiry ?? DefaultExpiry);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // SCAN iterates keys matching a pattern without blocking Redis (unlike KEYS command).
        // IServer.KeysAsync uses SCAN internally — safe for production.
        IServer server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());

        await foreach (RedisKey key in server.KeysAsync(pattern: $"{prefix}*"))
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
