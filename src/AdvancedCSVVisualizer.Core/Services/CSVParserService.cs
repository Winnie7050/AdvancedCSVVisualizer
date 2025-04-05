using AdvancedCSVVisualizer.Core.Interfaces;
using AdvancedCSVVisualizer.Core.Models;
using AdvancedCSVVisualizer.Core.Utilities;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Services
{
    /// <summary>
    /// Provides functionality for parsing CSV files.
    /// </summary>
    public class CSVParserService : ICSVParserService
    {
        private readonly ILogger<CSVParserService> _logger;
        private readonly IFileSystemService _fileSystemService;
        private readonly IErrorHandlingService _errorHandlingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVParserService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystemService">The file system service.</param>
        /// <param name="errorHandlingService">The error handling service.</param>
        public CSVParserService(
            ILogger<CSVParserService> logger,
            IFileSystemService fileSystemService,
            IErrorHandlingService errorHandlingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        }

        /// <inheritdoc/>
        public async Task<TimeSeriesData> ParseCSVFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogInformation("Parsing CSV file: {FilePath}", filePath);

                // Extract metadata from the file and its name
                var metadata = await ExtractMetadataAsync(filePath, cancellationToken);
                var parsingOptions = await OptimizeParsingAsync(filePath, cancellationToken);

                // Read file content
                var fileContent = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);

                // Create TimeSeriesData with metadata
                var timeSeriesData = new TimeSeriesData
                {
                    Title = metadata.MetricName,
                    Source = filePath,
                    Description = $"{metadata.MetricName} data from {metadata.StartDate:yyyy-MM-dd} to {metadata.EndDate:yyyy-MM-dd}",
                    CreationDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    Metadata = new Dictionary<string, object>
                    {
                        { "FilePath", filePath },
                        { "FileName", metadata.Filename },
                        { "MetricName", metadata.MetricName },
                        { "Identifier", metadata.Identifier },
                        { "StartDate", metadata.StartDate },
                        { "EndDate", metadata.EndDate },
                        { "RowCount", metadata.RowCount },
                        { "LastModifiedDate", metadata.LastModifiedDate }
                    }
                };

                using (var stringReader = new StringReader(fileContent))
                using (var csvReader = new CsvReader(stringReader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = parsingOptions.Delimiter.ToString(),
                    HasHeaderRecord = parsingOptions.HasHeaderRow,
                    TrimOptions = parsingOptions.TrimWhitespace ? TrimOptions.Trim : TrimOptions.None,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    Mode = parsingOptions.SupportMultiLine ? CsvMode.RFC4180 : CsvMode.NoEscape,
                    DetectColumnCountChanges = true,
                    IgnoreBlankLines = parsingOptions.SkipEmptyRows,
                    DynamicTyping = parsingOptions.DynamicTyping,
                    BufferSize = parsingOptions.BufferSize
                }))
                {
                    // Read header
                    csvReader.Read();
                    csvReader.ReadHeader();
                    var headers = csvReader.HeaderRecord?.ToList() ?? new List<string>();

                    // Set headers in metadata
                    metadata.Headers = headers;

                    // Analyze columns to detect date and value columns
                    metadata = await AnalyzeColumnsAsync(filePath, cancellationToken);
                    _logger.LogDebug("Column analysis: DateColumn={DateColumn}, ValueColumns={ValueColumns}, CategoryColumn={CategoryColumn}",
                        metadata.DateColumnIndex, string.Join(", ", metadata.ValueColumnIndices), metadata.CategoryColumnIndex);

                    // Check if we have sufficient data
                    if (metadata.DateColumnIndex < 0 || metadata.ValueColumnIndices.Count == 0)
                    {
                        throw new InvalidDataException("Could not identify date and value columns in the CSV file.");
                    }

                    // Create series for each value column
                    var seriesByName = new Dictionary<string, DataSeries>();
                    var dateColumnName = headers[metadata.DateColumnIndex];

                    // Read the data rows
                    int rowCounter = 0;
                    while (csvReader.Read() && !cancellationToken.IsCancellationRequested)
                    {
                        rowCounter++;
                        if (rowCounter % 1000 == 0)
                        {
                            _logger.LogDebug("Processed {RowCount} rows", rowCounter);
                        }

                        // Extract date/time value
                        var dateString = csvReader.GetField(metadata.DateColumnIndex)?.Trim();
                        if (string.IsNullOrWhiteSpace(dateString))
                        {
                            continue; // Skip rows with empty date
                        }

                        if (!DateTimeParser.TryParse(dateString, out var dateTime))
                        {
                            _logger.LogWarning("Failed to parse date: {DateString} at row {RowNumber}", dateString, rowCounter);
                            continue;
                        }

                        // Process each value column
                        foreach (var valueColumnIndex in metadata.ValueColumnIndices)
                        {
                            if (valueColumnIndex >= headers.Count)
                            {
                                continue; // Skip invalid indices
                            }

                            var seriesName = headers[valueColumnIndex];
                            var valueString = csvReader.GetField(valueColumnIndex)?.Trim();

                            if (string.IsNullOrWhiteSpace(valueString))
                            {
                                continue; // Skip empty values
                            }

                            if (!double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                            {
                                _logger.LogWarning("Failed to parse value: {ValueString} at row {RowNumber}, column {ColumnName}",
                                    valueString, rowCounter, seriesName);
                                continue;
                            }

                            // Get or create the series
                            if (!seriesByName.TryGetValue(seriesName, out var series))
                            {
                                series = new DataSeries(seriesName)
                                {
                                    Description = $"{seriesName} values over time",
                                    Color = GetColorForSeries(seriesName, seriesByName.Count)
                                };
                                seriesByName.Add(seriesName, series);
                            }

                            // Add the data point
                            series.Points.Add(new DataPoint(dateTime, value));
                        }
                    }

                    // Add series to the time series data
                    foreach (var series in seriesByName.Values)
                    {
                        // Sort points by date
                        series.Points = series.Points.OrderBy(p => p.DateTime).ToList();
                        timeSeriesData.Series.Add(series);
                    }

                    _logger.LogInformation("CSV parsing completed. Extracted {SeriesCount} series with {DataPointsCount} total data points",
                        timeSeriesData.Series.Count, timeSeriesData.Series.Sum(s => s.Points.Count));

                    return timeSeriesData;
                }
            }, "ParseCSVFile", true) ?? new TimeSeriesData();
        }

        /// <inheritdoc/>
        public async Task<CSVMetadata> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Extracting metadata from CSV file: {FilePath}", filePath);

                var metadata = new CSVMetadata
                {
                    FilePath = filePath,
                    Filename = Path.GetFileName(filePath)
                };

                // Extract metadata from filename
                metadata.TryExtractFromFilename(metadata.Filename);

                // Get file information
                var fileInfo = await _fileSystemService.GetFileInfoAsync(filePath);
                metadata.FileSize = fileInfo.Length;
                metadata.LastModifiedDate = fileInfo.LastWriteTime;

                // Detect encoding and delimiter
                metadata.Encoding = await DetectEncodingAsync(filePath, cancellationToken);
                var delimiter = await DetectDelimiterAsync(filePath, cancellationToken);
                metadata.Delimiter = delimiter.ToString();

                // Get row count
                metadata.RowCount = await GetRowCountAsync(filePath, cancellationToken);

                // Get headers to determine column count
                var headers = await ExtractHeadersAsync(filePath, cancellationToken);
                metadata.Headers = headers.ToList();
                metadata.ColumnCount = headers.Length;

                return metadata;
            }, "ExtractMetadata", false) ?? new CSVMetadata { FilePath = filePath, Filename = Path.GetFileName(filePath) };
        }

        /// <inheritdoc/>
        public async Task<string> DetectEncodingAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Detecting encoding for CSV file: {FilePath}", filePath);

                // Read the first few KB of the file to detect encoding
                byte[] buffer = new byte[4096];
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                }

                // Check for BOM
                if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                {
                    return "utf-8";
                }
                if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
                {
                    return "utf-16BE";
                }
                if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
                {
                    return "utf-16LE";
                }

                // Default to UTF-8 if no BOM is detected
                // A more sophisticated encoding detection could be implemented here
                return "utf-8";
            }, "DetectEncoding", false) ?? "utf-8";
        }

        /// <inheritdoc/>
        public async Task<char> DetectDelimiterAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Detecting delimiter for CSV file: {FilePath}", filePath);

                // Read the first few lines of the file
                var fileContent = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
                var lines = fileContent.Split('\n').Take(5).ToArray();

                if (lines.Length == 0)
                {
                    return ','; // Default to comma
                }

                // Common delimiters to check
                var delimiters = new[] { ',', ';', '\t', '|' };
                var counts = new Dictionary<char, int>();

                foreach (var delimiter in delimiters)
                {
                    counts[delimiter] = 0;
                }

                // Count occurrences of each delimiter in the first line
                var firstLine = lines[0].Trim();
                foreach (var delimiter in delimiters)
                {
                    counts[delimiter] = firstLine.Count(c => c == delimiter);
                }

                // If multiple lines, check that the delimiter count is consistent
                if (lines.Length > 1)
                {
                    var delimiterCounts = new Dictionary<char, List<int>>();
                    foreach (var delimiter in delimiters)
                    {
                        delimiterCounts[delimiter] = lines.Select(line => line.Trim().Count(c => c == delimiter)).ToList();
                    }

                    // Find the delimiter with the most consistent count across lines
                    var mostConsistent = delimiters
                        .OrderByDescending(d => delimiterCounts[d].Skip(1).Count(c => c == delimiterCounts[d][0]))
                        .ThenByDescending(d => counts[d])
                        .FirstOrDefault();

                    if (counts[mostConsistent] > 0)
                    {
                        return mostConsistent;
                    }
                }
                else
                {
                    // Just one line, return the most common delimiter
                    var mostCommon = delimiters.OrderByDescending(d => counts[d]).FirstOrDefault();
                    if (counts[mostCommon] > 0)
                    {
                        return mostCommon;
                    }
                }

                // Default to comma if no delimiter is found
                return ',';
            }, "DetectDelimiter", false) ?? ',';
        }

        /// <inheritdoc/>
        public async Task<bool> IsValidCSVFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Checking if file is a valid CSV: {FilePath}", filePath);

                if (!await _fileSystemService.FileExistsAsync(filePath))
                {
                    return false;
                }

                try
                {
                    // First, check file extension
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    if (extension != ".csv")
                    {
                        return false;
                    }

                    // Try to read the first few lines
                    var fileContent = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
                    var lines = fileContent.Split('\n').Take(3).ToArray();

                    if (lines.Length == 0)
                    {
                        return false;
                    }

                    // Detect delimiter
                    var delimiter = await DetectDelimiterAsync(filePath, cancellationToken);

                    // Check if all lines have the same number of columns
                    var firstLineColumnCount = lines[0].Split(delimiter).Length;
                    return lines.All(line => line.Trim().Split(delimiter).Length == firstLineColumnCount);
                }
                catch
                {
                    return false;
                }
            }, "IsValidCSVFile", false);
        }

        /// <inheritdoc/>
        public async Task<string[]> ExtractHeadersAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Extracting headers from CSV file: {FilePath}", filePath);

                var fileContent = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
                var firstLine = fileContent.Split('\n')[0].Trim();

                var delimiter = await DetectDelimiterAsync(filePath, cancellationToken);
                var headers = firstLine.Split(delimiter)
                    .Select(h => h.Trim().Trim('"').Trim())
                    .ToArray();

                _logger.LogDebug("Extracted {HeaderCount} headers from CSV file", headers.Length);
                return headers;
            }, "ExtractHeaders", false) ?? Array.Empty<string>();
        }

        /// <inheritdoc/>
        public async Task<string> GetSampleDataAsync(string filePath, int maxRows = 10, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Getting sample data from CSV file: {FilePath}, MaxRows: {MaxRows}", filePath, maxRows);

                var fileContent = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
                var lines = fileContent.Split('\n')
                    .Take(maxRows + 1) // +1 for header row
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();

                return string.Join(Environment.NewLine, lines);
            }, "GetSampleData", false) ?? string.Empty;
        }

        /// <inheritdoc/>
        public async Task<CSVMetadata> AnalyzeColumnsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Analyzing columns in CSV file: {FilePath}", filePath);

                var metadata = await ExtractMetadataAsync(filePath, cancellationToken);
                var headers = await ExtractHeadersAsync(filePath, cancellationToken);

                // Get a sample of data for analysis
                var sample = await GetSampleDataAsync(filePath, 50, cancellationToken);
                var lines = sample.Split('\n').Skip(1).ToArray(); // Skip header row

                // Initialize column type counters
                var columnTypes = new Dictionary<int, Dictionary<string, int>>();
                for (int i = 0; i < headers.Length; i++)
                {
                    columnTypes[i] = new Dictionary<string, int>
                    {
                        { "Date", 0 },
                        { "Numeric", 0 },
                        { "Text", 0 },
                        { "Empty", 0 }
                    };
                }

                // Analyze each column in the sample
                var delimiter = metadata.Delimiter[0];
                foreach (var line in lines)
                {
                    var fields = line.Split(delimiter).Select(f => f.Trim().Trim('"').Trim()).ToArray();
                    if (fields.Length != headers.Length)
                    {
                        continue; // Skip malformed lines
                    }

                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        if (string.IsNullOrWhiteSpace(field))
                        {
                            columnTypes[i]["Empty"]++;
                            continue;
                        }

                        if (DateTimeParser.TryParse(field, out _))
                        {
                            columnTypes[i]["Date"]++;
                        }
                        else if (double.TryParse(field, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        {
                            columnTypes[i]["Numeric"]++;
                        }
                        else
                        {
                            columnTypes[i]["Text"]++;
                        }
                    }
                }

                // Determine column types based on majority
                var dateColumnIndex = -1;
                var valueColumnIndices = new List<int>();
                var categoryColumnIndex = -1;

                for (int i = 0; i < headers.Length; i++)
                {
                    var header = headers[i].ToLowerInvariant();
                    var types = columnTypes[i];
                    var totalNonEmpty = types["Date"] + types["Numeric"] + types["Text"];
                    if (totalNonEmpty == 0)
                    {
                        continue; // Skip completely empty columns
                    }

                    // Check if this is a date column
                    bool isDateColumn = false;
                    if (types["Date"] > 0)
                    {
                        // If more than 50% of non-empty values are dates, or header suggests it's a date
                        isDateColumn = types["Date"] >= totalNonEmpty * 0.5 ||
                                      header.Contains("date") ||
                                      header.Contains("time") ||
                                      Regex.IsMatch(header, @"\b(year|month|day)\b");
                    }

                    // Check if this is a numeric/value column
                    bool isValueColumn = false;
                    if (types["Numeric"] > 0)
                    {
                        // If more than 50% of non-empty values are numeric, or header suggests it's a value
                        isValueColumn = types["Numeric"] >= totalNonEmpty * 0.5 ||
                                       header.Contains("value") ||
                                       header.Contains("count") ||
                                       header.Contains("rate") ||
                                       header.Contains("percentage") ||
                                       header.Contains("total");
                    }

                    // Check if this is a category column
                    bool isCategoryColumn = false;
                    if (types["Text"] > 0)
                    {
                        // If more than 50% of non-empty values are text, or header suggests it's a category
                        isCategoryColumn = types["Text"] >= totalNonEmpty * 0.5 ||
                                         header.Contains("category") ||
                                         header.Contains("type") ||
                                         header.Contains("group") ||
                                         header.Contains("breakdown");
                    }

                    // Assign column roles by priority
                    if (isDateColumn && dateColumnIndex == -1)
                    {
                        dateColumnIndex = i;
                    }
                    else if (isValueColumn)
                    {
                        valueColumnIndices.Add(i);
                    }
                    else if (isCategoryColumn && categoryColumnIndex == -1)
                    {
                        categoryColumnIndex = i;
                    }
                }

                // If no date column was found, try to find one based on header name
                if (dateColumnIndex == -1)
                {
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var header = headers[i].ToLowerInvariant();
                        if (header.Contains("date") || header.Contains("time"))
                        {
                            dateColumnIndex = i;
                            break;
                        }
                    }
                }

                // If still no date column and we have a Breakdown column, assume that's the date
                if (dateColumnIndex == -1 && headers.Length > 0)
                {
                    if (headers[0].Equals("Breakdown", StringComparison.OrdinalIgnoreCase))
                    {
                        dateColumnIndex = 0;
                    }
                    else if (categoryColumnIndex != -1)
                    {
                        // Use the category column as date if we couldn't find a dedicated date column
                        dateColumnIndex = categoryColumnIndex;
                        categoryColumnIndex = -1;
                    }
                    else
                    {
                        // Default to first column
                        dateColumnIndex = 0;
                    }
                }

                // Make sure we have at least one value column
                if (valueColumnIndices.Count == 0)
                {
                    // If no numeric columns were found, try to include any columns that aren't date or category
                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (i != dateColumnIndex && i != categoryColumnIndex)
                        {
                            valueColumnIndices.Add(i);
                        }
                    }
                }

                // Update metadata
                metadata.DateColumnIndex = dateColumnIndex;
                metadata.ValueColumnIndices = valueColumnIndices;
                metadata.CategoryColumnIndex = categoryColumnIndex;

                _logger.LogInformation("Column analysis complete. Date column: {DateColumn}, Value columns: {ValueColumns}, Category column: {CategoryColumn}",
                    dateColumnIndex >= 0 ? headers[dateColumnIndex] : "None",
                    string.Join(", ", valueColumnIndices.Select(i => headers[i])),
                    categoryColumnIndex >= 0 ? headers[categoryColumnIndex] : "None");

                return metadata;
            }, "AnalyzeColumns", false) ?? new CSVMetadata { FilePath = filePath, Filename = Path.GetFileName(filePath) };
        }

        /// <inheritdoc/>
        public async Task<int> GetRowCountAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Counting rows in CSV file: {FilePath}", filePath);

                var fileContent = await _fileSystemService.ReadFileAsync(filePath, cancellationToken);
                var lines = fileContent.Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Count();

                // Subtract 1 for header row
                return Math.Max(0, lines - 1);
            }, "GetRowCount", false);
        }

        /// <inheritdoc/>
        public async Task<CSVParsingOptions> OptimizeParsingAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _errorHandlingService.TryExecuteAsync(async () =>
            {
                _logger.LogDebug("Optimizing parsing options for CSV file: {FilePath}", filePath);

                var options = new CSVParsingOptions
                {
                    Delimiter = await DetectDelimiterAsync(filePath, cancellationToken),
                    Encoding = await DetectEncodingAsync(filePath, cancellationToken),
                    HasHeaderRow = true, // Assume CSV has headers
                    TrimWhitespace = true,
                    SkipEmptyRows = true,
                    DynamicTyping = true
                };

                // Check if file is large and adjust buffer size
                var fileInfo = await _fileSystemService.GetFileInfoAsync(filePath);
                if (fileInfo.Length > 10 * 1024 * 1024) // > 10 MB
                {
                    options.BufferSize = 65536; // 64 KB buffer for large files
                }

                // Check for multiline content by looking for quotes
                var sampleContent = await GetSampleDataAsync(filePath, 100, cancellationToken);
                options.SupportMultiLine = sampleContent.Contains("\"") && 
                                         sampleContent.Split('\n').Any(line => 
                                            line.Count(c => c == '"') % 2 != 0);

                return options;
            }, "OptimizeParsing", false) ?? new CSVParsingOptions();
        }

        /// <summary>
        /// Gets a color for a series based on its name and index.
        /// </summary>
        /// <param name="seriesName">The name of the series.</param>
        /// <param name="index">The index of the series.</param>
        /// <returns>A color for the series.</returns>
        private System.Drawing.Color GetColorForSeries(string seriesName, int index)
        {
            // Standard color palette with good contrast
            var colors = new[]
            {
                System.Drawing.Color.FromArgb(51, 102, 204),   // Blue
                System.Drawing.Color.FromArgb(220, 57, 18),    // Red
                System.Drawing.Color.FromArgb(16, 150, 24),    // Green
                System.Drawing.Color.FromArgb(153, 0, 153),    // Purple
                System.Drawing.Color.FromArgb(255, 153, 0),    // Orange
                System.Drawing.Color.FromArgb(0, 153, 198),    // Light Blue
                System.Drawing.Color.FromArgb(221, 68, 119),   // Pink
                System.Drawing.Color.FromArgb(102, 170, 0),    // Lime
                System.Drawing.Color.FromArgb(184, 46, 46),    // Dark Red
                System.Drawing.Color.FromArgb(49, 99, 149),    // Dark Blue
                System.Drawing.Color.FromArgb(153, 68, 153),   // Dark Purple
                System.Drawing.Color.FromArgb(34, 170, 153),   // Teal
                System.Drawing.Color.FromArgb(170, 170, 17),   // Olive
                System.Drawing.Color.FromArgb(102, 51, 204),   // Indigo
                System.Drawing.Color.FromArgb(230, 115, 0),    // Dark Orange
                System.Drawing.Color.FromArgb(139, 7, 7)       // Brown
            };

            // Use name hash for consistent coloring
            int nameHash = seriesName.GetHashCode();
            int colorIndex = Math.Abs(nameHash) % colors.Length;

            // If the series is named "Total" or similar, use a distinctive color (the first one)
            if (seriesName.Equals("Total", StringComparison.OrdinalIgnoreCase) ||
                seriesName.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                seriesName.Equals("Sum", StringComparison.OrdinalIgnoreCase))
            {
                return colors[0];
            }

            return colors[colorIndex];
        }
    }
}
