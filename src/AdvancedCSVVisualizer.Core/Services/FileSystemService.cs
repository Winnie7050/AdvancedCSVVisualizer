using AdvancedCSVVisualizer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Services
{
    /// <summary>
    /// Provides file system operations for the application.
    /// </summary>
    public class FileSystemService : IFileSystemService, IDisposable
    {
        private readonly ILogger<FileSystemService> _logger;
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly Subject<FileSystemEventArgs> _fileSystemEvents = new Subject<FileSystemEventArgs>();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public FileSystemService(ILogger<FileSystemService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<string>> GetDirectoriesAsync(string directoryPath)
        {
            try
            {
                _logger.LogDebug("Getting directories in {DirectoryPath}", directoryPath);
                
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
                    return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
                }

                return Task.FromResult<IEnumerable<string>>(Directory.GetDirectories(directoryPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting directories in {DirectoryPath}", directoryPath);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<string>> GetFilesAsync(string directoryPath, string searchPattern = "*.csv", bool recursive = false)
        {
            try
            {
                _logger.LogDebug("Getting files in {DirectoryPath} with pattern {SearchPattern}, Recursive: {Recursive}", 
                    directoryPath, searchPattern, recursive);
                
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
                    return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
                }

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Task.FromResult<IEnumerable<string>>(Directory.GetFiles(directoryPath, searchPattern, searchOption));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files in {DirectoryPath} with pattern {SearchPattern}", 
                    directoryPath, searchPattern);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Reading file: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", filePath);
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
                }

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    bufferSize: 4096, useAsync: true);
                using var reader = new StreamReader(fileStream);
                
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReadFileBytesAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Reading file bytes: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", filePath);
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
                }

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    bufferSize: 4096, useAsync: true);
                
                var buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                
                return buffer;
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                _logger.LogError(ex, "Error reading file bytes: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task WriteFileAsync(string filePath, string contents, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Writing file: {FilePath}", filePath);
                
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogInformation("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 
                    bufferSize: 4096, useAsync: true);
                using var writer = new StreamWriter(fileStream);
                
                await writer.WriteAsync(contents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task WriteFileBytesAsync(string filePath, byte[] contents, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Writing file bytes: {FilePath}", filePath);
                
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogInformation("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 
                    bufferSize: 4096, useAsync: true);
                
                await fileStream.WriteAsync(contents, 0, contents.Length, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file bytes: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<FileInfo> GetFileInfoAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("Getting file info: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", filePath);
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
                }

                return Task.FromResult(new FileInfo(filePath));
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                _logger.LogError(ex, "Error getting file info: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<bool> FileExistsAsync(string filePath)
        {
            _logger.LogDebug("Checking if file exists: {FilePath}", filePath);
            return Task.FromResult(File.Exists(filePath));
        }

        /// <inheritdoc/>
        public Task<bool> DirectoryExistsAsync(string directoryPath)
        {
            _logger.LogDebug("Checking if directory exists: {DirectoryPath}", directoryPath);
            return Task.FromResult(Directory.Exists(directoryPath));
        }

        /// <inheritdoc/>
        public Task CreateDirectoryAsync(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogInformation("Creating directory: {DirectoryPath}", directoryPath);
                    Directory.CreateDirectory(directoryPath);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory: {DirectoryPath}", directoryPath);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    _logger.LogInformation("Deleting file: {FilePath}", filePath);
                    File.Delete(filePath);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent file: {FilePath}", filePath);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public IObservable<FileSystemEventArgs> WatchDirectory(string directoryPath, string filter = "*.csv")
        {
            try
            {
                _logger.LogDebug("Setting up directory watcher for {DirectoryPath} with filter {Filter}", 
                    directoryPath, filter);
                
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
                    return Observable.Empty<FileSystemEventArgs>();
                }

                var watcher = new FileSystemWatcher(directoryPath, filter)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true
                };

                watcher.Changed += OnFileSystemEvent;
                watcher.Created += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Renamed += OnFileSystemEvent;

                _watchers.Add(watcher);

                return _fileSystemEvents.AsObservable()
                    .Where(e => e.FullPath.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up directory watcher for {DirectoryPath}", directoryPath);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("Getting file size: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", filePath);
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
                }

                var fileInfo = new FileInfo(filePath);
                return Task.FromResult(fileInfo.Length);
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                _logger.LogError(ex, "Error getting file size: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<DateTime> GetFileCreationTimeAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("Getting file creation time: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", filePath);
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
                }

                return Task.FromResult(File.GetCreationTime(filePath));
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                _logger.LogError(ex, "Error getting file creation time: {FilePath}", filePath);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<DateTime> GetFileLastWriteTimeAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("Getting file last write time: {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", filePath);
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
                }

                return Task.FromResult(File.GetLastWriteTime(filePath));
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                _logger.LogError(ex, "Error getting file last write time: {FilePath}", filePath);
                throw;
            }
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            _logger.LogTrace("File system event: {ChangeType} - {FullPath}", e.ChangeType, e.FullPath);
            _fileSystemEvents.OnNext(e);
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
                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Changed -= OnFileSystemEvent;
                    watcher.Created -= OnFileSystemEvent;
                    watcher.Deleted -= OnFileSystemEvent;
                    watcher.Renamed -= OnFileSystemEvent;
                    watcher.Dispose();
                }

                _watchers.Clear();
                _fileSystemEvents.Dispose();
            }

            _disposed = true;
        }
    }
}
