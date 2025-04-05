using AdvancedCSVVisualizer.Core.Interfaces;
using MemoryPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Services
{
    /// <summary>
    /// Provides caching functionality for the application with both memory and disk caching.
    /// </summary>
    public class CacheService : ICacheService, IDisposable
    {
        private readonly ILogger<CacheService> _logger;
        private readonly IFileSystemService _fileSystemService;
        private readonly string _cacheDirectory;
        private readonly ConcurrentDictionary<string, CacheItem> _memoryCache = new ConcurrentDictionary<string, CacheItem>();
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystemService">The file system service.</param>
        /// <param name="cacheDirectory">The directory to use for disk caching.</param>
        public CacheService(
            ILogger<CacheService> logger,
            IFileSystemService fileSystemService,
            string? cacheDirectory = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            
            // Use default cache directory if not specified
            _cacheDirectory = cacheDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AdvancedCSVVisualizer", "Cache");
            
            // Ensure cache directory exists
            Task.Run(() => _fileSystemService.CreateDirectoryAsync(_cacheDirectory)).Wait();
            
            _logger.LogInformation("CacheService initialized with cache directory: {CacheDirectory}", _cacheDirectory);
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(key);
            
            try
            {
                // Check in-memory cache first
                if (_memoryCache.TryGetValue(key, out var cacheItem))
                {
                    if (!IsCacheItemExpired(cacheItem))
                    {
                        _logger.LogDebug("Cache hit (memory) for key: {Key}", key);
                        
                        // Update last access time
                        cacheItem.LastAccessTime = DateTime.Now;
                        
                        try
                        {
                            return (T)cacheItem.Value;
                        }
                        catch (InvalidCastException)
                        {
                            _logger.LogWarning("Cached item of type {ActualType} could not be cast to {RequestedType}", 
                                cacheItem.Value.GetType().Name, typeof(T).Name);
                            return null;
                        }
                    }
                    else
                    {
                        // Remove expired item
                        _logger.LogDebug("Removing expired cache item for key: {Key}", key);
                        _memoryCache.TryRemove(key, out _);
                    }
                }
                
                // Check disk cache
                var filePath = GetCacheFilePath(key);
                if (await _fileSystemService.FileExistsAsync(filePath))
                {
                    try
                    {
                        _logger.LogDebug("Cache hit (disk) for key: {Key}", key);
                        
                        // Read cache metadata
                        var metadataPath = filePath + ".meta";
                        CacheItemMetadata? metadata = null;
                        
                        if (await _fileSystemService.FileExistsAsync(metadataPath))
                        {
                            var metadataBytes = await _fileSystemService.ReadFileBytesAsync(metadataPath, cancellationToken);
                            metadata = MemoryPackSerializer.Deserialize<CacheItemMetadata>(metadataBytes);
                        }
                        
                        // Check if item is expired
                        if (metadata != null && IsCacheItemExpired(metadata))
                        {
                            _logger.LogDebug("Removing expired disk cache item for key: {Key}", key);
                            await _fileSystemService.DeleteFileAsync(filePath);
                            await _fileSystemService.DeleteFileAsync(metadataPath);
                            return null;
                        }
                        
                        // Read the actual data
                        var bytes = await _fileSystemService.ReadFileBytesAsync(filePath, cancellationToken);
                        
                        // Deserialize the data
                        var value = MemoryPackSerializer.Deserialize<T>(bytes);
                        
                        // Add to memory cache
                        if (value != null)
                        {
                            var absoluteExpiration = metadata?.AbsoluteExpiration;
                            var slidingExpiration = metadata?.SlidingExpiration;
                            
                            _memoryCache.TryAdd(key, new CacheItem
                            {
                                Value = value,
                                AbsoluteExpiration = absoluteExpiration,
                                SlidingExpiration = slidingExpiration,
                                LastAccessTime = DateTime.Now,
                                Size = bytes.Length
                            });
                        }
                        
                        return value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading cache from disk for key: {Key}", key);
                        
                        // Try to delete the potentially corrupted cache file
                        try
                        {
                            await _fileSystemService.DeleteFileAsync(filePath);
                            await _fileSystemService.DeleteFileAsync(filePath + ".meta");
                        }
                        catch
                        {
                            // Ignore errors in cleanup
                        }
                    }
                }
                
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAsync for key: {Key}", key);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, DateTimeOffset? absoluteExpiration = null, 
            TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            
            try
            {
                var bytes = MemoryPackSerializer.Serialize(value);
                var size = bytes.Length;
                
                // Add to memory cache
                _memoryCache[key] = new CacheItem
                {
                    Value = value,
                    AbsoluteExpiration = absoluteExpiration,
                    SlidingExpiration = slidingExpiration,
                    LastAccessTime = DateTime.Now,
                    Size = size
                };
                
                _logger.LogDebug("Added item to memory cache with key: {Key}, Size: {Size} bytes", key, size);
                
                // Write to disk cache
                await _cacheLock.WaitAsync(cancellationToken);
                try
                {
                    var filePath = GetCacheFilePath(key);
                    await _fileSystemService.WriteFileBytesAsync(filePath, bytes, cancellationToken);
                    
                    // Write metadata
                    var metadata = new CacheItemMetadata
                    {
                        Key = key,
                        AbsoluteExpiration = absoluteExpiration,
                        SlidingExpiration = slidingExpiration,
                        LastAccessTime = DateTime.Now,
                        CreationTime = DateTime.Now,
                        Size = size
                    };
                    
                    var metadataBytes = MemoryPackSerializer.Serialize(metadata);
                    await _fileSystemService.WriteFileBytesAsync(filePath + ".meta", metadataBytes, cancellationToken);
                    
                    _logger.LogDebug("Added item to disk cache with key: {Key}, Size: {Size} bytes", key, size);
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetAsync for key: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            
            try
            {
                // Remove from memory cache
                _memoryCache.TryRemove(key, out _);
                
                // Remove from disk cache
                await _cacheLock.WaitAsync(cancellationToken);
                try
                {
                    var filePath = GetCacheFilePath(key);
                    
                    if (await _fileSystemService.FileExistsAsync(filePath))
                    {
                        await _fileSystemService.DeleteFileAsync(filePath);
                    }
                    
                    var metadataPath = filePath + ".meta";
                    if (await _fileSystemService.FileExistsAsync(metadataPath))
                    {
                        await _fileSystemService.DeleteFileAsync(metadataPath);
                    }
                    
                    _logger.LogDebug("Removed cache item with key: {Key}", key);
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveAsync for key: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, DateTimeOffset? absoluteExpiration = null, 
            TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(factory);
            
            // Check if in cache
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
            {
                return cachedValue;
            }
            
            // Not in cache, generate and add
            var value = await factory();
            
            if (value != null)
            {
                await SetAsync(key, value, absoluteExpiration, slidingExpiration, cancellationToken);
            }
            
            return value;
        }

        /// <inheritdoc/>
        public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            
            // Check memory cache first
            if (_memoryCache.TryGetValue(key, out var cacheItem))
            {
                if (!IsCacheItemExpired(cacheItem))
                {
                    return true;
                }
                
                // Remove expired item
                _memoryCache.TryRemove(key, out _);
            }
            
            // Check disk cache
            var filePath = GetCacheFilePath(key);
            
            if (await _fileSystemService.FileExistsAsync(filePath))
            {
                var metadataPath = filePath + ".meta";
                
                if (await _fileSystemService.FileExistsAsync(metadataPath))
                {
                    try
                    {
                        var metadataBytes = await _fileSystemService.ReadFileBytesAsync(metadataPath, cancellationToken);
                        var metadata = MemoryPackSerializer.Deserialize<CacheItemMetadata>(metadataBytes);
                        
                        if (metadata != null && !IsCacheItemExpired(metadata))
                        {
                            return true;
                        }
                        
                        // Remove expired items
                        await _fileSystemService.DeleteFileAsync(filePath);
                        await _fileSystemService.DeleteFileAsync(metadataPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking disk cache metadata for key: {Key}", key);
                    }
                }
            }
            
            return false;
        }

        /// <inheritdoc/>
        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            
            // Update in-memory cache
            if (_memoryCache.TryGetValue(key, out var cacheItem))
            {
                cacheItem.LastAccessTime = DateTime.Now;
            }
            
            // Update disk cache metadata
            var filePath = GetCacheFilePath(key);
            var metadataPath = filePath + ".meta";
            
            if (await _fileSystemService.FileExistsAsync(metadataPath))
            {
                await _cacheLock.WaitAsync(cancellationToken);
                try
                {
                    var metadataBytes = await _fileSystemService.ReadFileBytesAsync(metadataPath, cancellationToken);
                    var metadata = MemoryPackSerializer.Deserialize<CacheItemMetadata>(metadataBytes);
                    
                    if (metadata != null)
                    {
                        metadata.LastAccessTime = DateTime.Now;
                        
                        var updatedMetadataBytes = MemoryPackSerializer.Serialize(metadata);
                        await _fileSystemService.WriteFileBytesAsync(metadataPath, updatedMetadataBytes, cancellationToken);
                        
                        _logger.LogDebug("Refreshed cache item with key: {Key}", key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing cache item with key: {Key}", key);
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            // Clear memory cache
            _memoryCache.Clear();
            
            // Clear disk cache
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                var files = await _fileSystemService.GetFilesAsync(_cacheDirectory, "*", false);
                
                foreach (var file in files)
                {
                    try
                    {
                        await _fileSystemService.DeleteFileAsync(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting cache file: {File}", file);
                    }
                }
                
                _logger.LogInformation("Cache cleared");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
        {
            var keys = new HashSet<string>();
            
            // Get keys from memory cache
            foreach (var key in _memoryCache.Keys)
            {
                if (IsPatternMatch(key, pattern))
                {
                    keys.Add(key);
                }
            }
            
            // Get keys from disk cache
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                var files = await _fileSystemService.GetFilesAsync(_cacheDirectory, "*.meta", false);
                
                foreach (var file in files)
                {
                    try
                    {
                        var metadataBytes = await _fileSystemService.ReadFileBytesAsync(file, cancellationToken);
                        var metadata = MemoryPackSerializer.Deserialize<CacheItemMetadata>(metadataBytes);
                        
                        if (metadata != null && IsPatternMatch(metadata.Key, pattern))
                        {
                            keys.Add(metadata.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading cache metadata file: {File}", file);
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            return keys;
        }

        /// <inheritdoc/>
        public async Task<long> GetSizeAsync(CancellationToken cancellationToken = default)
        {
            // Calculate memory cache size
            long memorySize = _memoryCache.Values.Sum(item => item.Size);
            
            // Calculate disk cache size
            long diskSize = 0;
            
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                var files = await _fileSystemService.GetFilesAsync(_cacheDirectory, "*", false);
                
                foreach (var file in files)
                {
                    try
                    {
                        var fileSize = await _fileSystemService.GetFileSizeAsync(file);
                        diskSize += fileSize;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting size of cache file: {File}", file);
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            return memorySize + diskSize;
        }

        /// <inheritdoc/>
        public async Task TrimAsync(long maxSizeBytes, CancellationToken cancellationToken = default)
        {
            var currentSize = await GetSizeAsync(cancellationToken);
            
            if (currentSize <= maxSizeBytes)
            {
                return;
            }
            
            _logger.LogInformation("Trimming cache. Current size: {CurrentSize} bytes, Max size: {MaxSize} bytes", 
                currentSize, maxSizeBytes);
            
            // Get all metadata for disk cache items
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                var metadataFiles = await _fileSystemService.GetFilesAsync(_cacheDirectory, "*.meta", false);
                var metadataList = new List<CacheItemMetadata>();
                
                foreach (var file in metadataFiles)
                {
                    try
                    {
                        var metadataBytes = await _fileSystemService.ReadFileBytesAsync(file, cancellationToken);
                        var metadata = MemoryPackSerializer.Deserialize<CacheItemMetadata>(metadataBytes);
                        
                        if (metadata != null)
                        {
                            metadataList.Add(metadata);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading cache metadata file: {File}", file);
                    }
                }
                
                // Sort by last access time (oldest first)
                metadataList.Sort((x, y) => x.LastAccessTime.CompareTo(y.LastAccessTime));
                
                // Remove oldest items until we're under the limit
                foreach (var metadata in metadataList)
                {
                    if (currentSize <= maxSizeBytes)
                    {
                        break;
                    }
                    
                    var filePath = GetCacheFilePath(metadata.Key);
                    var metadataPath = filePath + ".meta";
                    
                    try
                    {
                        // Remove from memory cache
                        if (_memoryCache.TryRemove(metadata.Key, out var cacheItem))
                        {
                            currentSize -= cacheItem.Size;
                        }
                        
                        // Remove from disk cache
                        if (await _fileSystemService.FileExistsAsync(filePath))
                        {
                            var fileSize = await _fileSystemService.GetFileSizeAsync(filePath);
                            await _fileSystemService.DeleteFileAsync(filePath);
                            currentSize -= fileSize;
                        }
                        
                        if (await _fileSystemService.FileExistsAsync(metadataPath))
                        {
                            var metaSize = await _fileSystemService.GetFileSizeAsync(metadataPath);
                            await _fileSystemService.DeleteFileAsync(metadataPath);
                            currentSize -= metaSize;
                        }
                        
                        _logger.LogDebug("Removed cache item during trim: {Key}", metadata.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error removing cache item during trim: {Key}", metadata.Key);
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            _logger.LogInformation("Cache trimmed. New size: {NewSize} bytes", await GetSizeAsync(cancellationToken));
        }

        /// <inheritdoc/>
        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            // Count memory cache items
            int memoryCount = _memoryCache.Count;
            
            // Count disk cache items (unique files, not including metadata files)
            int diskCount = 0;
            
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                var files = await _fileSystemService.GetFilesAsync(_cacheDirectory, "*", false);
                
                // Count only data files (not .meta files)
                diskCount = files.Count(f => !f.EndsWith(".meta"));
            }
            finally
            {
                _cacheLock.Release();
            }
            
            return Math.Max(memoryCount, diskCount); // Use the larger count since memory cache is a subset of disk cache
        }

        /// <summary>
        /// Gets the file path for a cache item with the given key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The full file path.</returns>
        private string GetCacheFilePath(string key)
        {
            // Replace invalid characters and encode the key to make it safe for file system
            var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            
            // Use a hash to ensure the filename isn't too long
            var hash = key.GetHashCode().ToString("X8");
            var filename = $"{hash}_{safeKey}";
            
            // Truncate if still too long
            if (filename.Length > 100)
            {
                filename = $"{hash}_{safeKey.Substring(0, 90)}";
            }
            
            return Path.Combine(_cacheDirectory, filename);
        }

        /// <summary>
        /// Determines whether a cache item has expired.
        /// </summary>
        /// <param name="item">The cache item to check.</param>
        /// <returns>true if the cache item has expired; otherwise, false.</returns>
        private bool IsCacheItemExpired(CacheItem item)
        {
            if (item.AbsoluteExpiration.HasValue && DateTime.Now >= item.AbsoluteExpiration.Value)
            {
                return true;
            }
            
            if (item.SlidingExpiration.HasValue)
            {
                var lastAccess = item.LastAccessTime;
                var expirationTime = lastAccess.Add(item.SlidingExpiration.Value);
                
                if (DateTime.Now >= expirationTime)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Determines whether a cache item has expired.
        /// </summary>
        /// <param name="metadata">The cache item metadata to check.</param>
        /// <returns>true if the cache item has expired; otherwise, false.</returns>
        private bool IsCacheItemExpired(CacheItemMetadata metadata)
        {
            if (metadata.AbsoluteExpiration.HasValue && DateTime.Now >= metadata.AbsoluteExpiration.Value)
            {
                return true;
            }
            
            if (metadata.SlidingExpiration.HasValue)
            {
                var lastAccess = metadata.LastAccessTime;
                var expirationTime = lastAccess.Add(metadata.SlidingExpiration.Value);
                
                if (DateTime.Now >= expirationTime)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Determines whether a key matches a pattern.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="pattern">The pattern to match against.</param>
        /// <returns>true if the key matches the pattern; otherwise, false.</returns>
        private bool IsPatternMatch(string key, string pattern)
        {
            if (pattern == "*")
            {
                return true;
            }
            
            // Convert glob pattern to regex
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(key, regex);
        }

        /// <summary>
        /// Disposes the resources used by the service.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resources used by the service.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _cacheLock.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Represents an item in the cache.
        /// </summary>
        private class CacheItem
        {
            /// <summary>
            /// Gets or sets the cached value.
            /// </summary>
            public object Value { get; set; } = null!;

            /// <summary>
            /// Gets or sets the absolute expiration date and time.
            /// </summary>
            public DateTimeOffset? AbsoluteExpiration { get; set; }

            /// <summary>
            /// Gets or sets the sliding expiration time span.
            /// </summary>
            public TimeSpan? SlidingExpiration { get; set; }

            /// <summary>
            /// Gets or sets the last access time.
            /// </summary>
            public DateTime LastAccessTime { get; set; }

            /// <summary>
            /// Gets or sets the size of the cached item in bytes.
            /// </summary>
            public long Size { get; set; }
        }

        /// <summary>
        /// Represents metadata for a cached item.
        /// </summary>
        [MemoryPackable]
        private partial class CacheItemMetadata
        {
            /// <summary>
            /// Gets or sets the cache key.
            /// </summary>
            public string Key { get; set; } = null!;

            /// <summary>
            /// Gets or sets the absolute expiration date and time.
            /// </summary>
            public DateTimeOffset? AbsoluteExpiration { get; set; }

            /// <summary>
            /// Gets or sets the sliding expiration time span.
            /// </summary>
            public TimeSpan? SlidingExpiration { get; set; }

            /// <summary>
            /// Gets or sets the last access time.
            /// </summary>
            public DateTime LastAccessTime { get; set; }

            /// <summary>
            /// Gets or sets the creation time.
            /// </summary>
            public DateTime CreationTime { get; set; }

            /// <summary>
            /// Gets or sets the size of the cached item in bytes.
            /// </summary>
            public long Size { get; set; }
        }
    }
}
