using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Interfaces
{
    /// <summary>
    /// Provides caching functionality for the application.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets a cached item of the specified type with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the cached item.</typeparam>
        /// <param name="key">The key of the cached item.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The cached item, or default(T) if the item is not in the cache.</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Sets a cached item with the specified key and value.
        /// </summary>
        /// <typeparam name="T">The type of the item to cache.</typeparam>
        /// <param name="key">The key to use for the cached item.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="absoluteExpiration">The absolute expiration time for the cached item, or null for no expiration.</param>
        /// <param name="slidingExpiration">The sliding expiration time for the cached item, or null for no sliding expiration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous set operation.</returns>
        Task SetAsync<T>(string key, T value, DateTimeOffset? absoluteExpiration = null, 
            TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Removes a cached item with the specified key.
        /// </summary>
        /// <param name="key">The key of the cached item to remove.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous remove operation.</returns>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a cached item or, if the item is not in the cache, adds it using the specified factory function.
        /// </summary>
        /// <typeparam name="T">The type of the cached item.</typeparam>
        /// <param name="key">The key of the cached item.</param>
        /// <param name="factory">A function that produces the value to cache if it's not already cached.</param>
        /// <param name="absoluteExpiration">The absolute expiration time for the cached item, or null for no expiration.</param>
        /// <param name="slidingExpiration">The sliding expiration time for the cached item, or null for no sliding expiration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The cached item, either from the cache or newly created.</returns>
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, DateTimeOffset? absoluteExpiration = null, 
            TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Determines whether the cache contains an item with the specified key.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>true if the cache contains an item with the specified key; otherwise, false.</returns>
        Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes a cached item, extending its expiration time.
        /// </summary>
        /// <param name="key">The key of the cached item to refresh.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous refresh operation.</returns>
        Task RefreshAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all items from the cache.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous clear operation.</returns>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all cache keys that match the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern to match against keys.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A collection of matching keys.</returns>
        Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the size of the cache in bytes.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The size of the cache in bytes.</returns>
        Task<long> GetSizeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes least recently used items from the cache until it is below the specified size.
        /// </summary>
        /// <param name="maxSizeBytes">The maximum size of the cache in bytes.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous trim operation.</returns>
        Task TrimAsync(long maxSizeBytes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the number of items in the cache.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The number of items in the cache.</returns>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    }
}
