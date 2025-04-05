using AdvancedCSVVisualizer.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Interfaces
{
    /// <summary>
    /// Provides functionality for parsing CSV files.
    /// </summary>
    public interface ICSVParserService
    {
        /// <summary>
        /// Parses a CSV file and returns the resulting time series data.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to parse.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="TimeSeriesData"/> object containing the parsed data.</returns>
        Task<TimeSeriesData> ParseCSVFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts metadata from a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to extract metadata from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="CSVMetadata"/> object containing the extracted metadata.</returns>
        Task<CSVMetadata> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects the encoding of a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to detect encoding for.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The detected encoding name.</returns>
        Task<string> DetectEncodingAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects the delimiter used in a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to detect delimiter for.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The detected delimiter character.</returns>
        Task<char> DetectDelimiterAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies that a file is a valid CSV file.
        /// </summary>
        /// <param name="filePath">The path to the file to verify.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>true if the file is a valid CSV file; otherwise, false.</returns>
        Task<bool> IsValidCSVFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts the headers from a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to extract headers from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An array of header strings.</returns>
        Task<string[]> ExtractHeadersAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a sample of data from a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to get a sample from.</param>
        /// <param name="maxRows">The maximum number of rows to include in the sample.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A string containing the sample data.</returns>
        Task<string> GetSampleDataAsync(string filePath, int maxRows = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes a CSV file to identify relevant columns for data series.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to analyze.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="CSVMetadata"/> object containing the column analysis results.</returns>
        Task<CSVMetadata> AnalyzeColumnsAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of rows in a CSV file.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to count rows in.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The number of data rows (excluding header) in the CSV file.</returns>
        Task<int> GetRowCountAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes CSV parsing based on file characteristics.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to optimize parsing for.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An object containing optimal parsing parameters.</returns>
        Task<CSVParsingOptions> OptimizeParsingAsync(string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents options for CSV parsing.
    /// </summary>
    public class CSVParsingOptions
    {
        /// <summary>
        /// Gets or sets the delimiter character.
        /// </summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>
        /// Gets or sets the encoding name.
        /// </summary>
        public string Encoding { get; set; } = "utf-8";

        /// <summary>
        /// Gets or sets a value indicating whether the CSV has a header row.
        /// </summary>
        public bool HasHeaderRow { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to trim whitespace from values.
        /// </summary>
        public bool TrimWhitespace { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to support multi-line values.
        /// </summary>
        public bool SupportMultiLine { get; set; } = false;

        /// <summary>
        /// Gets or sets the buffer size for reading.
        /// </summary>
        public int BufferSize { get; set; } = 16384;

        /// <summary>
        /// Gets or sets a value indicating whether to dynamically type values.
        /// </summary>
        public bool DynamicTyping { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to skip empty rows.
        /// </summary>
        public bool SkipEmptyRows { get; set; } = true;

        /// <summary>
        /// Gets or sets the comment character.
        /// </summary>
        public char? CommentCharacter { get; set; }

        /// <summary>
        /// Gets or sets the quote character.
        /// </summary>
        public char QuoteCharacter { get; set; } = '"';

        /// <summary>
        /// Gets or sets the escape character.
        /// </summary>
        public char EscapeCharacter { get; set; } = '"';
    }
}
