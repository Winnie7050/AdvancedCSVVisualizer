using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AdvancedCSVVisualizer.Core.Utilities
{
    /// <summary>
    /// Provides functionality for parsing date/time strings in various formats.
    /// </summary>
    public static class DateTimeParser
    {
        // Common date/time formats
        private static readonly string[] CommonFormats = {
            // ISO 8601
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss",
            
            // Date only
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "dd-MMM-yyyy",
            "MMMM dd, yyyy",
            
            // Date and time
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            
            // Other common formats
            "yyyyMMdd",
            "yyyyMMddHHmmss",
            "yyyy_MM_dd",
            "dd_MM_yyyy",
            "yyyy.MM.dd",
            "dd.MM.yyyy"
        };

        /// <summary>
        /// Attempts to parse a date/time string using various common formats.
        /// </summary>
        /// <param name="dateTimeString">The date/time string to parse.</param>
        /// <param name="result">When this method returns, contains the parsed DateTime value if parsing succeeded, or DateTime.MinValue if parsing failed.</param>
        /// <returns>true if parsing succeeded; otherwise, false.</returns>
        public static bool TryParse(string dateTimeString, out DateTime result)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
            {
                result = DateTime.MinValue;
                return false;
            }

            // Try a few pre-cleaning steps
            string normalized = dateTimeString.Trim();
            
            // Replace variations of 'T' separator in ISO 8601
            normalized = Regex.Replace(normalized, @"\s+", " ");

            // Try built-in DateTime parsing first
            if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }

            // Try common formats
            foreach (var format in CommonFormats)
            {
                if (DateTime.TryParseExact(normalized, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return true;
                }
            }

            // Try with different separators
            foreach (var format in GenerateFormatVariations(normalized))
            {
                if (DateTime.TryParseExact(normalized, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return true;
                }
            }

            // Special case for Unix timestamps (seconds since epoch)
            if (long.TryParse(normalized, out long unixTime))
            {
                try
                {
                    // Check if it's a reasonable Unix timestamp (between 1970 and 2100)
                    if (unixTime > 0 && unixTime < 4102444800) // 4102444800 is 2100-01-01 in Unix time
                    {
                        result = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                        return true;
                    }
                }
                catch
                {
                    // Ignore and continue
                }
            }

            // Special case for Unix timestamps in milliseconds
            if (long.TryParse(normalized, out long unixTimeMs))
            {
                try
                {
                    // Check if it's a reasonable Unix timestamp in milliseconds
                    if (unixTimeMs > 1000000000000 && unixTimeMs < 4102444800000) // Millisecond range for 1970-2100
                    {
                        result = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMs).DateTime;
                        return true;
                    }
                }
                catch
                {
                    // Ignore and continue
                }
            }

            result = DateTime.MinValue;
            return false;
        }

        /// <summary>
        /// Attempts to parse a date/time string using various common formats and a specific culture.
        /// </summary>
        /// <param name="dateTimeString">The date/time string to parse.</param>
        /// <param name="culture">The culture to use for parsing.</param>
        /// <param name="result">When this method returns, contains the parsed DateTime value if parsing succeeded, or DateTime.MinValue if parsing failed.</param>
        /// <returns>true if parsing succeeded; otherwise, false.</returns>
        public static bool TryParse(string dateTimeString, CultureInfo culture, out DateTime result)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
            {
                result = DateTime.MinValue;
                return false;
            }

            // Try built-in DateTime parsing first with the specified culture
            if (DateTime.TryParse(dateTimeString, culture, DateTimeStyles.None, out result))
            {
                return true;
            }

            // Fall back to invariant culture parsing
            return TryParse(dateTimeString, out result);
        }

        /// <summary>
        /// Gets date/time format variations based on a sample string.
        /// </summary>
        /// <param name="sample">The sample date/time string.</param>
        /// <returns>A collection of potential format strings.</returns>
        private static IEnumerable<string> GenerateFormatVariations(string sample)
        {
            var formats = new List<string>();
            
            // Replace separators with placeholders
            var datePattern = @"(\d{1,4})[.\-/](\d{1,2})[.\-/](\d{1,4})";
            var timePattern = @"(\d{1,2}):(\d{1,2})(?::(\d{1,2}))?(?:\.(\d{1,3}))?";
            
            var dateMatch = Regex.Match(sample, datePattern);
            if (dateMatch.Success)
            {
                // Year first (e.g., 2023-01-01)
                if (dateMatch.Groups[1].Value.Length == 4)
                {
                    formats.Add($"yyyy{dateMatch.Value[4]}MM{dateMatch.Value[7]}dd");
                    formats.Add($"yyyy-MM-dd");
                    formats.Add($"yyyy/MM/dd");
                    formats.Add($"yyyy.MM.dd");
                }
                // Year last (e.g., 01/01/2023)
                else if (dateMatch.Groups[3].Value.Length == 4)
                {
                    formats.Add($"dd{dateMatch.Value[2]}MM{dateMatch.Value[5]}yyyy");
                    formats.Add($"MM{dateMatch.Value[2]}dd{dateMatch.Value[5]}yyyy");
                    formats.Add($"dd-MM-yyyy");
                    formats.Add($"MM-dd-yyyy");
                    formats.Add($"dd/MM/yyyy");
                    formats.Add($"MM/dd/yyyy");
                    formats.Add($"dd.MM.yyyy");
                    formats.Add($"MM.dd.yyyy");
                }
            }
            
            var timeMatch = Regex.Match(sample, timePattern);
            if (timeMatch.Success)
            {
                var timePart = "HH:mm";
                if (timeMatch.Groups[3].Success)
                {
                    timePart += ":ss";
                    if (timeMatch.Groups[4].Success)
                    {
                        timePart += ".fff";
                    }
                }
                
                foreach (var dateFormat in formats.ToArray())
                {
                    formats.Add($"{dateFormat} {timePart}");
                }
                
                formats.Add(timePart);
            }
            
            return formats;
        }

        /// <summary>
        /// Detects the most likely date/time format in a collection of strings.
        /// </summary>
        /// <param name="dateStrings">The collection of date/time strings to analyze.</param>
        /// <returns>The most likely format string, or null if no consistent format is detected.</returns>
        public static string? DetectDateTimeFormat(IEnumerable<string> dateStrings)
        {
            var formatCounts = new Dictionary<string, int>();
            var successCount = 0;
            
            foreach (var dateString in dateStrings)
            {
                if (TryParse(dateString, out var dateTime))
                {
                    successCount++;
                    
                    // Try to determine which format was actually used
                    foreach (var format in CommonFormats)
                    {
                        try
                        {
                            var formatted = dateTime.ToString(format, CultureInfo.InvariantCulture);
                            if (string.Equals(formatted, dateString, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(formatted, dateString.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                if (formatCounts.ContainsKey(format))
                                    formatCounts[format]++;
                                else
                                    formatCounts[format] = 1;
                                
                                break;
                            }
                        }
                        catch
                        {
                            // Ignore format exceptions
                        }
                    }
                }
            }
            
            // If at least 50% of strings were parsed successfully
            if (successCount >= dateStrings.Count() / 2)
            {
                // Return the most common format
                return formatCounts.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
            }
            
            return null;
        }
    }
}
