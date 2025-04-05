using MemoryPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AdvancedCSVVisualizer.Core.Models
{
    /// <summary>
    /// Represents a collection of data series for time series analysis and visualization.
    /// </summary>
    [MemoryPackable]
    public partial class TimeSeriesData
    {
        /// <summary>
        /// Gets or sets the title of the time series data.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the time series data.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the source of the time series data.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the collection of data series.
        /// </summary>
        public ObservableCollection<DataSeries> Series { get; set; } = new ObservableCollection<DataSeries>();

        /// <summary>
        /// Gets or sets the metadata associated with this time series data.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the creation date of this time series data.
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last modified date of this time series data.
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets the earliest date across all series.
        /// </summary>
        [MemoryPackIgnore]
        public DateTime StartDate => Series.Any() && Series.All(s => s.Points.Any())
            ? Series.Min(s => s.Points.Min(p => p.DateTime))
            : DateTime.MinValue;

        /// <summary>
        /// Gets the latest date across all series.
        /// </summary>
        [MemoryPackIgnore]
        public DateTime EndDate => Series.Any() && Series.All(s => s.Points.Any())
            ? Series.Max(s => s.Points.Max(p => p.DateTime))
            : DateTime.MaxValue;

        /// <summary>
        /// Gets the minimum value across all series.
        /// </summary>
        [MemoryPackIgnore]
        public double MinValue => Series.Any() && Series.All(s => s.Points.Any())
            ? Series.Min(s => s.Points.Min(p => p.Value))
            : 0;

        /// <summary>
        /// Gets the maximum value across all series.
        /// </summary>
        [MemoryPackIgnore]
        public double MaxValue => Series.Any() && Series.All(s => s.Points.Any())
            ? Series.Max(s => s.Points.Max(p => p.Value))
            : 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesData"/> class.
        /// </summary>
        public TimeSeriesData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesData"/> class with the specified title.
        /// </summary>
        /// <param name="title">The title of the time series data.</param>
        public TimeSeriesData(string title)
        {
            Title = title;
        }

        /// <summary>
        /// Gets a filtered subset of the time series data within the specified date range.
        /// </summary>
        /// <param name="startDate">The start date for filtering.</param>
        /// <param name="endDate">The end date for filtering.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with filtered data.</returns>
        public TimeSeriesData GetFilteredData(DateTime startDate, DateTime endDate)
        {
            var filteredData = new TimeSeriesData
            {
                Title = Title,
                Description = Description,
                Source = Source,
                Metadata = Metadata,
                CreationDate = CreationDate,
                LastModifiedDate = DateTime.Now
            };

            foreach (var series in Series)
            {
                var filteredSeries = new DataSeries
                {
                    Name = series.Name,
                    Description = series.Description,
                    Color = series.Color,
                    AggregationMethod = series.AggregationMethod,
                    Metadata = series.Metadata,
                    Points = series.Points
                        .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
                        .ToList()
                };

                if (filteredSeries.Points.Any())
                {
                    filteredData.Series.Add(filteredSeries);
                }
            }

            return filteredData;
        }

        /// <summary>
        /// Removes all data points outside the specified date range from all series.
        /// </summary>
        /// <param name="startDate">The start date to keep.</param>
        /// <param name="endDate">The end date to keep.</param>
        public void FilterInPlace(DateTime startDate, DateTime endDate)
        {
            foreach (var series in Series)
            {
                series.Points = series.Points
                    .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
                    .ToList();
            }

            // Remove any series that no longer have data points
            var seriesToRemove = Series.Where(s => !s.Points.Any()).ToList();
            foreach (var series in seriesToRemove)
            {
                Series.Remove(series);
            }

            LastModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Gets data from all series for a specific date.
        /// </summary>
        /// <param name="date">The date to get data for.</param>
        /// <returns>A dictionary mapping series name to value.</returns>
        public Dictionary<string, double> GetValuesForDate(DateTime date)
        {
            var result = new Dictionary<string, double>();

            foreach (var series in Series)
            {
                var point = series.Points.FirstOrDefault(p => p.DateTime.Date == date.Date);
                if (point != null)
                {
                    result[series.Name] = point.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a string that represents the current time series data.
        /// </summary>
        /// <returns>A string that represents the current time series data.</returns>
        public override string ToString()
        {
            return $"{Title} ({Series.Count} series, {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd})";
        }
    }
}
