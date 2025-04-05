using MemoryPack;
using System;
using System.Collections.Generic;
using System.IO;

namespace AdvancedCSVVisualizer.Core.Models
{
    /// <summary>
    /// Represents user-specific application settings and preferences.
    /// </summary>
    [MemoryPackable]
    public partial class UserPreferences
    {
        /// <summary>
        /// Gets or sets the default data directory.
        /// </summary>
        public string DefaultDataDirectory { get; set; } = @"D:\1GameDev\Frankfurt Response\Statistics\";

        /// <summary>
        /// Gets or sets the default time period for visualizations.
        /// </summary>
        public TimePeriod DefaultTimePeriod { get; set; } = TimePeriod.ThirtyDays;

        /// <summary>
        /// Gets or sets the default visualization type.
        /// </summary>
        public VisualizationType DefaultVisualizationType { get; set; } = VisualizationType.LineChart;

        /// <summary>
        /// Gets or sets the default visualization theme.
        /// </summary>
        public VisualizationTheme DefaultTheme { get; set; } = VisualizationTheme.Dark;

        /// <summary>
        /// Gets or sets the default color palette.
        /// </summary>
        public ColorPalette DefaultColorPalette { get; set; } = ColorPalette.Default;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically detect CSV structure.
        /// </summary>
        public bool AutoDetectCsvStructure { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to remember last viewed files.
        /// </summary>
        public bool RememberLastViewedFiles { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to check for updates at startup.
        /// </summary>
        public bool CheckForUpdatesAtStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use system date format.
        /// </summary>
        public bool UseSystemDateFormat { get; set; } = false;

        /// <summary>
        /// Gets or sets the custom date format.
        /// </summary>
        public string CustomDateFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>
        /// Gets or sets a value indicating whether to use system number format.
        /// </summary>
        public bool UseSystemNumberFormat { get; set; } = false;

        /// <summary>
        /// Gets or sets the custom number format.
        /// </summary>
        public string CustomNumberFormat { get; set; } = "0.##";

        /// <summary>
        /// Gets or sets the maximum number of files to remember in history.
        /// </summary>
        public int MaxRecentFilesHistory { get; set; } = 10;

        /// <summary>
        /// Gets or sets the list of recently viewed files.
        /// </summary>
        public List<string> RecentFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the cache directory for processed data.
        /// </summary>
        public string CacheDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AdvancedCSVVisualizer", "Cache");

        /// <summary>
        /// Gets or sets the maximum cache size in megabytes.
        /// </summary>
        public int MaxCacheSizeMB { get; set; } = 500;

        /// <summary>
        /// Gets or sets a value indicating whether to enable background processing.
        /// </summary>
        public bool EnableBackgroundProcessing { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable file watching for automatic updates.
        /// </summary>
        public bool EnableFileWatching { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of background processing threads.
        /// </summary>
        public int MaxBackgroundThreads { get; set; } = Math.Max(1, Environment.ProcessorCount - 1);

        /// <summary>
        /// Gets or sets a value indicating whether to use hardware acceleration.
        /// </summary>
        public bool UseHardwareAcceleration { get; set; } = true;

        /// <summary>
        /// Gets or sets the window width.
        /// </summary>
        public double WindowWidth { get; set; } = 1280;

        /// <summary>
        /// Gets or sets the window height.
        /// </summary>
        public double WindowHeight { get; set; } = 720;

        /// <summary>
        /// Gets or sets a value indicating whether the window is maximized.
        /// </summary>
        public bool IsWindowMaximized { get; set; } = false;

        /// <summary>
        /// Gets or sets the last application version used.
        /// </summary>
        public string LastAppVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the dictionary of custom settings.
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UserPreferences"/> class.
        /// </summary>
        public UserPreferences()
        {
            // Ensure cache directory exists
            Directory.CreateDirectory(CacheDirectory);
        }

        /// <summary>
        /// Adds a file to the recent files list.
        /// </summary>
        /// <param name="filePath">The file path to add.</param>
        public void AddRecentFile(string filePath)
        {
            // Remove the file if it already exists in the list
            RecentFiles.Remove(filePath);

            // Add the file to the beginning of the list
            RecentFiles.Insert(0, filePath);

            // Trim the list if it exceeds the maximum size
            while (RecentFiles.Count > MaxRecentFilesHistory)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
        }

        /// <summary>
        /// Clears the recent files list.
        /// </summary>
        public void ClearRecentFiles()
        {
            RecentFiles.Clear();
        }

        /// <summary>
        /// Creates a default instance of the <see cref="UserPreferences"/> class.
        /// </summary>
        /// <returns>A new instance with default settings.</returns>
        public static UserPreferences CreateDefault()
        {
            return new UserPreferences();
        }
    }

    /// <summary>
    /// Specifies the time period for visualization.
    /// </summary>
    public enum TimePeriod
    {
        /// <summary>
        /// Last 7 days.
        /// </summary>
        SevenDays,

        /// <summary>
        /// Last 30 days.
        /// </summary>
        ThirtyDays,

        /// <summary>
        /// Last 60 days.
        /// </summary>
        SixtyDays,

        /// <summary>
        /// Last 90 days.
        /// </summary>
        NinetyDays,

        /// <summary>
        /// Custom date range.
        /// </summary>
        Custom,

        /// <summary>
        /// All available data.
        /// </summary>
        All
    }
}
