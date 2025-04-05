using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Interfaces
{
    /// <summary>
    /// Provides access to file system operations.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Gets all directories within the specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <returns>A collection of directory paths.</returns>
        Task<IEnumerable<string>> GetDirectoriesAsync(string directoryPath);

        /// <summary>
        /// Gets all files matching the specified pattern within the specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to search in.</param>
        /// <param name="searchPattern">The search pattern to match against file names.</param>
        /// <param name="recursive">true to search subdirectories; otherwise, false.</param>
        /// <returns>A collection of file paths.</returns>
        Task<IEnumerable<string>> GetFilesAsync(string directoryPath, string searchPattern = "*.csv", bool recursive = false);

        /// <summary>
        /// Reads the contents of a file as a string.
        /// </summary>
        /// <param name="filePath">The file path to read from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The file contents as a string.</returns>
        Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the contents of a file as a byte array.
        /// </summary>
        /// <param name="filePath">The file path to read from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The file contents as a byte array.</returns>
        Task<byte[]> ReadFileBytesAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes a string to a file.
        /// </summary>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="contents">The string to write.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteFileAsync(string filePath, string contents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes a byte array to a file.
        /// </summary>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="contents">The byte array to write.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteFileBytesAsync(string filePath, byte[] contents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a file.
        /// </summary>
        /// <param name="filePath">The file path to get information for.</param>
        /// <returns>A <see cref="FileInfo"/> object containing information about the file.</returns>
        Task<FileInfo> GetFileInfoAsync(string filePath);

        /// <summary>
        /// Determines whether a file exists.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>true if the file exists; otherwise, false.</returns>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Determines whether a directory exists.
        /// </summary>
        /// <param name="directoryPath">The directory path to check.</param>
        /// <returns>true if the directory exists; otherwise, false.</returns>
        Task<bool> DirectoryExistsAsync(string directoryPath);

        /// <summary>
        /// Creates a directory if it does not already exist.
        /// </summary>
        /// <param name="directoryPath">The directory path to create.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateDirectoryAsync(string directoryPath);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="filePath">The file path to delete.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteFileAsync(string filePath);

        /// <summary>
        /// Watches a directory for changes to files.
        /// </summary>
        /// <param name="directoryPath">The directory path to watch.</param>
        /// <param name="filter">The filter to apply to file names.</param>
        /// <returns>An observable sequence of file system events.</returns>
        IObservable<FileSystemEventArgs> WatchDirectory(string directoryPath, string filter = "*.csv");

        /// <summary>
        /// Gets the size of a file in bytes.
        /// </summary>
        /// <param name="filePath">The file path to get the size for.</param>
        /// <returns>The size of the file in bytes.</returns>
        Task<long> GetFileSizeAsync(string filePath);

        /// <summary>
        /// Gets the creation time of a file.
        /// </summary>
        /// <param name="filePath">The file path to get the creation time for.</param>
        /// <returns>The creation time of the file.</returns>
        Task<DateTime> GetFileCreationTimeAsync(string filePath);

        /// <summary>
        /// Gets the last write time of a file.
        /// </summary>
        /// <param name="filePath">The file path to get the last write time for.</param>
        /// <returns>The last write time of the file.</returns>
        Task<DateTime> GetFileLastWriteTimeAsync(string filePath);
    }
}
