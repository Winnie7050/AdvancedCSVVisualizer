using MemoryPack;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AdvancedCSVVisualizer.Core.Models
{
    /// <summary>
    /// Represents metadata extracted from a CSV file, especially from its filename and header.
    /// </summary>
    [MemoryPackable]
    public partial class CSVMetadata
    {
        /// <summary>
        /// Gets or sets the original filename.
        /// </summary>
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metric name extracted from the filename.
        /// </summary>
        public string MetricName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier extracted from the filename.
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start date of the data range.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the data range.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets the column headers from the CSV file.
        /// </summary>
        public List<string> Headers { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the index of the column containing date/time data.
        /// </summary>
        public int DateColumnIndex { get; set; } = -1;

        /// <summary>
        /// Gets or sets the indices of columns containing numeric data.
        /// </summary>
        public List<int> ValueColumnIndices { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the index of the column containing categories.
        /// </summary>
        public int CategoryColumnIndex { get; set; } = -1;

        /// <summary>
        /// Gets or sets additional properties extracted from the CSV file.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the last modified date of the file.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the character encoding of the file.
        /// </summary>
        public string Encoding { get; set; } = "utf-8";

        /// <summary>
        /// Gets or sets the delimiter used in the CSV file.
        /// </summary>
        public string Delimiter { get; set; } = ",";

        /// <summary>
        /// Gets or sets the number of rows in the CSV file (excluding header).
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Gets or sets the number of columns in the CSV file.
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVMetadata"/> class.
        /// </summary>
        public CSVMetadata()
        {
        }

        /// <summary>
        /// Attempts to extract metadata from a filename following the convention:
        /// [Metric Name] - [Identifier], [Start DateTime] to [End DateTime]
        /// </summary>
        /// <param name="filename">The filename to extract metadata from.</param>
        /// <returns>true if metadata was successfully extracted; otherwise, false.</returns>
        public bool TryExtractFromFilename(string filename)
        {
            try
            {
                Filename = filename;

                // Pattern to match: "MetricName - Identifier, StartDate to EndDate"
                var pattern = @"^(.+)\s*-\s*([^,]+),\s*(.+)\s+to\s+(.+)$";
                var match = Regex.Match(filename, pattern);

                if (!match.Success)
                    return false;

                MetricName = match.Groups[1].Value.Trim();
                Identifier = match.Groups[2].Value.Trim();

                // Try to parse the dates
                var startDateStr = match.Groups[3].Value.Trim();
                var endDateStr = match.Groups[4].Value.Trim();

                // Try to parse with various standard formats
                if (!TryParseDateTime(startDateStr, out var startDate) || 
                    !TryParseDateTime(endDateStr, out var endDate))
                    return false;

                StartDate = startDate;
                EndDate = endDate;

                return true;
            }
            catch
            {
                // If any parsing fails, return false
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse a date/time string using various standard formats.
        /// </summary>
        /// <param name="dateTimeStr">The date/time string to parse.</param>
        /// <param name="result">When this method returns, contains the parsed DateTime value.</param>
        /// <returns>true if parsing succeeded; otherwise, false.</returns>
        private bool TryParseDateTime(string dateTimeStr, out DateTime result)
        {
            // Replace common separators with standardized format
            var normalizedStr = dateTimeStr
                .Replace('T', ' ')
                .Replace('-', '/')
                .Replace('_', '/');

            // Try to parse with various formats
            string[] formats = {
                "yyyy/MM/dd HH:mm:ss.fff'Z'",
                "yyyy/MM/dd HH:mm:ss.fff",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy/MM/dd HH:mm",
                "yyyy/MM/dd",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy"
            };

            return DateTime.TryParseExact(normalizedStr, formats, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, 
                out result);
        }

        /// <summary>
        /// Returns a string that represents the current CSV metadata.
        /// </summary>
        /// <returns>A string that represents the current CSV metadata.</returns>
        public override string ToString()
        {
            return $"{MetricName} ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd})";
        }
    }
}
