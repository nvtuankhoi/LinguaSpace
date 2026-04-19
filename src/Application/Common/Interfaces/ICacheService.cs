namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// Distributed cache abstraction used by Application layer.
/// Implemented by RedisCacheService in Infrastructure.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes all keys matching the given prefix (e.g., "rooms:*").</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
