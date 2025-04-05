using MemoryPack;
using System;

namespace AdvancedCSVVisualizer.Core.Models
{
    /// <summary>
    /// Represents a single data point in a time series.
    /// </summary>
    [MemoryPackable]
    public partial class DataPoint : IEquatable<DataPoint>
    {
        /// <summary>
        /// Gets or sets the date and time of the data point.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the value of the data point.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets optional additional metadata for the data point.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPoint"/> class.
        /// </summary>
        public DataPoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPoint"/> class with the specified date time and value.
        /// </summary>
        /// <param name="dateTime">The date and time of the data point.</param>
        /// <param name="value">The value of the data point.</param>
        public DataPoint(DateTime dateTime, double value)
        {
            DateTime = dateTime;
            Value = value;
        }

        /// <summary>
        /// Determines whether this data point is equal to another data point.
        /// </summary>
        /// <param name="other">The data point to compare with.</param>
        /// <returns>true if the data points are equal; otherwise, false.</returns>
        public bool Equals(DataPoint? other)
        {
            if (other == null)
                return false;

            return DateTime.Equals(other.DateTime) && Math.Abs(Value - other.Value) < 0.0000001;
        }

        /// <summary>
        /// Determines whether this data point is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>true if the objects are equal; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is DataPoint other)
                return Equals(other);

            return false;
        }

        /// <summary>
        /// Returns the hash code for this data point.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(DateTime, Value);
        }

        /// <summary>
        /// Returns a string that represents the current data point.
        /// </summary>
        /// <returns>A string that represents the current data point.</returns>
        public override string ToString()
        {
            return $"{DateTime:yyyy-MM-dd HH:mm:ss}: {Value}";
        }
    }
}
