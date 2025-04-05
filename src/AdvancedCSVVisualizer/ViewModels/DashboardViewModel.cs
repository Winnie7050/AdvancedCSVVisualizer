using AdvancedCSVVisualizer.Core.Interfaces;
using AdvancedCSVVisualizer.Core.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AdvancedCSVVisualizer.ViewModels
{
    /// <summary>
    /// View model for the dashboard view.
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly ICSVParserService _csvParserService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ICacheService _cacheService;
        private readonly IErrorHandlingService _errorHandlingService;

        /// <summary>
        /// Gets the collection of recent files.
        /// </summary>
        public ObservableCollection<RecentFileInfo> RecentFiles { get; } = new ObservableCollection<RecentFileInfo>();

        /// <summary>
        /// Gets the collection of available directories.
        /// </summary>
        public ObservableCollection<string> AvailableDirectories { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets or sets the collection of available files in the selected directory.
        /// </summary>
        public ObservableCollection<FileInfo> AvailableFiles { get; } = new ObservableCollection<FileInfo>();

        /// <summary>
        /// Gets or sets the default directory for loading files.
        /// </summary>
        public string DefaultDirectory { get; set; } = @"D:\1GameDev\Frankfurt Response\Statistics\";

        /// <summary>
        /// Gets or sets the selected directory.
        /// </summary>
        private string _selectedDirectory;
        public string SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (SetProperty(ref _selectedDirectory, value))
                {
                    // Load files for the selected directory
                    LoadFilesFromDirectoryAsync(value).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected file.
        /// </summary>
        private FileInfo _selectedFile;
        public FileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value))
                {
                    // Enable load commands when a file is selected
                    LoadAsLineChartCommand.NotifyCanExecuteChanged();
                    LoadAsBarChartCommand.NotifyCanExecuteChanged();
                    LoadAsPieChartCommand.NotifyCanExecuteChanged();
                    LoadAsDivergingBarChartCommand.NotifyCanExecuteChanged();
                    
                    // Load preview information for the selected file
                    LoadFilePreviewAsync(value).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the file preview information.
        /// </summary>
        private string _filePreview = "Select a file to see a preview.";
        public string FilePreview
        {
            get => _filePreview;
            set => SetProperty(ref _filePreview, value);
        }

        /// <summary>
        /// Gets or sets the file metadata information.
        /// </summary>
        private CSVMetadata _fileMetadata;
        public CSVMetadata FileMetadata
        {
            get => _fileMetadata;
            set => SetProperty(ref _fileMetadata, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a file is selected.
        /// </summary>
        public bool IsFileSelected => SelectedFile != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardViewModel"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="csvParserService">The CSV parser service.</param>
        /// <param name="fileSystemService">The file system service.</param>
        /// <param name="cacheService">The cache service.</param>
        /// <param name="errorHandlingService">The error handling service.</param>
        public DashboardViewModel(
            ILogger<DashboardViewModel> logger,
            ICSVParserService csvParserService,
            IFileSystemService fileSystemService,
            ICacheService cacheService,
            IErrorHandlingService errorHandlingService)
            : base(logger)
        {
            _csvParserService = csvParserService ?? throw new ArgumentNullException(nameof(csvParserService));
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            Title = "Dashboard";
            _selectedDirectory = DefaultDirectory;
            _fileMetadata = new CSVMetadata();
        }

        /// <summary>
        /// Initializes the view model.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // Load directories and files
            LoadDirectoriesAsync().ConfigureAwait(false);
            LoadFilesFromDirectoryAsync(SelectedDirectory).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the list of available directories.
        /// </summary>
        private async Task LoadDirectoriesAsync()
        {
            await ExecuteWithBusyIndicationAsync(async () =>
            {
                // Clear existing directories
                AvailableDirectories.Clear();

                // Check if default directory exists
                if (!await _fileSystemService.DirectoryExistsAsync(DefaultDirectory))
                {
                    // Use My Documents as fallback
                    DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    SelectedDirectory = DefaultDirectory;
                }

                // Add parent directory
                var parentDir = Directory.GetParent(DefaultDirectory)?.FullName;
                if (parentDir != null)
                {
                    AvailableDirectories.Add(parentDir);
                }

                // Add default directory
                AvailableDirectories.Add(DefaultDirectory);

                // Add subdirectories
                var directories = await _fileSystemService.GetDirectoriesAsync(DefaultDirectory);
                foreach (var dir in directories.OrderBy(d => d))
                {
                    AvailableDirectories.Add(dir);
                }

                StatusMessage = $"Loaded {AvailableDirectories.Count} directories";

            }, "LoadDirectories", "Loading directories...");
        }

        /// <summary>
        /// Loads the list of available files from the specified directory.
        /// </summary>
        /// <param name="directory">The directory to load files from.</param>
        private async Task LoadFilesFromDirectoryAsync(string directory)
        {
            await ExecuteWithBusyIndicationAsync(async () =>
            {
                // Clear existing files
                AvailableFiles.Clear();

                // Check if directory exists
                if (!await _fileSystemService.DirectoryExistsAsync(directory))
                {
                    StatusMessage = $"Directory does not exist: {directory}";
                    return;
                }

                // Get all CSV files in the directory
                var files = await _fileSystemService.GetFilesAsync(directory, "*.csv");
                
                // Convert to FileInfo objects and add to collection
                foreach (var file in files.OrderBy(f => f))
                {
                    AvailableFiles.Add(new FileInfo(file));
                }

                StatusMessage = $"Loaded {AvailableFiles.Count} CSV files from {directory}";

            }, "LoadFilesFromDirectory", $"Loading files from {directory}...");
        }

        /// <summary>
        /// Loads a preview of the selected file.
        /// </summary>
        /// <param name="file">The file to preview.</param>
        private async Task LoadFilePreviewAsync(FileInfo file)
        {
            if (file == null)
                return;

            await ExecuteWithBusyIndicationAsync(async () =>
            {
                // Get file metadata
                FileMetadata = await _csvParserService.ExtractMetadataAsync(file.FullName);

                // Get sample data
                var sampleData = await _csvParserService.GetSampleDataAsync(file.FullName, 20);
                FilePreview = sampleData;

                StatusMessage = $"Loaded preview for {file.Name}";

            }, "LoadFilePreview", $"Loading preview for {file?.Name}...");
        }

        /// <summary>
        /// Command to load the selected file as a line chart.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanLoadFile))]
        private void LoadAsLineChart()
        {
            // Implementation to load as line chart
            Logger.LogInformation("Loading file as line chart: {File}", SelectedFile?.FullName);
        }

        /// <summary>
        /// Command to load the selected file as a bar chart.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanLoadFile))]
        private void LoadAsBarChart()
        {
            // Implementation to load as bar chart
            Logger.LogInformation("Loading file as bar chart: {File}", SelectedFile?.FullName);
        }

        /// <summary>
        /// Command to load the selected file as a pie chart.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanLoadFile))]
        private void LoadAsPieChart()
        {
            // Implementation to load as pie chart
            Logger.LogInformation("Loading file as pie chart: {File}", SelectedFile?.FullName);
        }

        /// <summary>
        /// Command to load the selected file as a diverging bar chart.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanLoadFile))]
        private void LoadAsDivergingBarChart()
        {
            // Implementation to load as diverging bar chart
            Logger.LogInformation("Loading file as diverging bar chart: {File}", SelectedFile?.FullName);
        }

        /// <summary>
        /// Command to refresh the file list.
        /// </summary>
        [RelayCommand]
        private async Task RefreshFileList()
        {
            await LoadFilesFromDirectoryAsync(SelectedDirectory);
        }

        /// <summary>
        /// Command to navigate to the parent directory.
        /// </summary>
        [RelayCommand]
        private async Task NavigateToParentDirectory()
        {
            var parentDir = Directory.GetParent(SelectedDirectory)?.FullName;
            if (parentDir != null)
            {
                SelectedDirectory = parentDir;
                await LoadFilesFromDirectoryAsync(parentDir);
            }
        }

        /// <summary>
        /// Determines whether a file can be loaded.
        /// </summary>
        /// <returns>true if a file can be loaded; otherwise, false.</returns>
        private bool CanLoadFile() => SelectedFile != null && !IsBusy;
    }

    /// <summary>
    /// Represents information about a recently opened file.
    /// </summary>
    public class RecentFileInfo
    {
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the last access time.
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// Gets the formatted file size.
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                else if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                else
                    return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }
    }
}
