using MemoryPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AdvancedCSVVisualizer.Core.Models
{
    /// <summary>
    /// Represents a series of data points for visualization.
    /// </summary>
    [MemoryPackable]
    public partial class DataSeries
    {
        /// <summary>
        /// Gets or sets the name of the data series.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the data series.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the list of data points in this series.
        /// </summary>
        public List<DataPoint> Points { get; set; } = new List<DataPoint>();

        /// <summary>
        /// Gets or sets the color for this series in visualizations.
        /// </summary>
        [MemoryPackIgnore]
        public Color Color { get; set; } = Color.Blue;

        /// <summary>
        /// Gets or sets the hex color code for serialization purposes.
        /// </summary>
        public string HexColor
        {
            get => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";
            set
            {
                if (string.IsNullOrEmpty(value) || !value.StartsWith("#") || value.Length != 7)
                    return;

                try
                {
                    var r = Convert.ToByte(value.Substring(1, 2), 16);
                    var g = Convert.ToByte(value.Substring(3, 2), 16);
                    var b = Convert.ToByte(value.Substring(5, 2), 16);
                    Color = Color.FromArgb(r, g, b);
                }
                catch
                {
                    // Failed to parse, keep default color
                }
            }
        }

        /// <summary>
        /// Gets or sets the aggregation method to use for this series.
        /// </summary>
        public AggregationMethod AggregationMethod { get; set; } = AggregationMethod.Average;

        /// <summary>
        /// Gets or sets additional metadata for the series.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets the minimum value in the data series.
        /// </summary>
        [MemoryPackIgnore]
        public double MinValue => Points.Any() ? Points.Min(p => p.Value) : 0;

        /// <summary>
        /// Gets the maximum value in the data series.
        /// </summary>
        [MemoryPackIgnore]
        public double MaxValue => Points.Any() ? Points.Max(p => p.Value) : 0;

        /// <summary>
        /// Gets the average value in the data series.
        /// </summary>
        [MemoryPackIgnore]
        public double AverageValue => Points.Any() ? Points.Average(p => p.Value) : 0;

        /// <summary>
        /// Gets the earliest date in the data series.
        /// </summary>
        [MemoryPackIgnore]
        public DateTime StartDate => Points.Any() ? Points.Min(p => p.DateTime) : DateTime.MinValue;

        /// <summary>
        /// Gets the latest date in the data series.
        /// </summary>
        [MemoryPackIgnore]
        public DateTime EndDate => Points.Any() ? Points.Max(p => p.DateTime) : DateTime.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSeries"/> class.
        /// </summary>
        public DataSeries()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSeries"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the data series.</param>
        public DataSeries(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSeries"/> class with the specified name and color.
        /// </summary>
        /// <param name="name">The name of the data series.</param>
        /// <param name="color">The color for the data series.</param>
        public DataSeries(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        /// <summary>
        /// Gets the percentage change between the first and last data points.
        /// </summary>
        /// <returns>The percentage change or 0 if there are less than 2 data points.</returns>
        public double GetPercentageChange()
        {
            if (Points.Count < 2)
                return 0;

            var orderedPoints = Points.OrderBy(p => p.DateTime).ToList();
            var firstValue = orderedPoints.First().Value;
            var lastValue = orderedPoints.Last().Value;

            if (Math.Abs(firstValue) < 0.0000001)
                return 0;

            return ((lastValue - firstValue) / Math.Abs(firstValue)) * 100;
        }

        /// <summary>
        /// Returns a string that represents the current data series.
        /// </summary>
        /// <returns>A string that represents the current data series.</returns>
        public override string ToString()
        {
            return $"{Name} ({Points.Count} points)";
        }
    }

    /// <summary>
    /// Specifies the method used to aggregate data points.
    /// </summary>
    public enum AggregationMethod
    {
        /// <summary>
        /// Use the average of all values.
        /// </summary>
        Average,

        /// <summary>
        /// Use the sum of all values.
        /// </summary>
        Sum,

        /// <summary>
        /// Use the minimum value.
        /// </summary>
        Min,

        /// <summary>
        /// Use the maximum value.
        /// </summary>
        Max,

        /// <summary>
        /// Use the first value.
        /// </summary>
        First,

        /// <summary>
        /// Use the last value.
        /// </summary>
        Last,

        /// <summary>
        /// Use the median value.
        /// </summary>
        Median,

        /// <summary>
        /// Use the count of values.
        /// </summary>
        Count
    }
}
