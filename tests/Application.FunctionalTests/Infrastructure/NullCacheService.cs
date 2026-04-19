using LinguaSpace.Application.Common.Interfaces;

namespace LinguaSpace.Application.FunctionalTests.Infrastructure;

/// <summary>
/// No-op ICacheService used in functional tests.
/// Returns default values for Get, silently ignores Set/Remove.
/// Keeps tests fast and independent of Redis availability.
/// </summary>
internal sealed class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(default(T));

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
