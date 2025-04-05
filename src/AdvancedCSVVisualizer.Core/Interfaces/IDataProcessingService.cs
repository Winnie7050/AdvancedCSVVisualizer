using AdvancedCSVVisualizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Interfaces
{
    /// <summary>
    /// Provides functionality for processing time series data.
    /// </summary>
    public interface IDataProcessingService
    {
        /// <summary>
        /// Aggregates time series data based on the specified period.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="period">The aggregation period.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with aggregated data.</returns>
        Task<TimeSeriesData> AggregateTimeSeriesAsync(TimeSeriesData sourceData, TimeGranularity period, CancellationToken cancellationToken = default);

        /// <summary>
        /// Filters time series data to include only data within the specified date range.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="startDate">The start date of the range to include.</param>
        /// <param name="endDate">The end date of the range to include.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with filtered data.</returns>
        Task<TimeSeriesData> FilterTimeSeriesAsync(TimeSeriesData sourceData, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates statistical information for time series data.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A dictionary mapping statistic names to values.</returns>
        Task<Dictionary<string, Dictionary<string, double>>> CalculateStatisticsAsync(TimeSeriesData sourceData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates period-over-period changes for time series data.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="period">The period to calculate changes over.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with period-over-period change data.</returns>
        Task<TimeSeriesData> CalculatePercentageChangesAsync(TimeSeriesData sourceData, TimeGranularity period, CancellationToken cancellationToken = default);

        /// <summary>
        /// Smooths time series data using the specified method.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="method">The smoothing method to use.</param>
        /// <param name="windowSize">The window size for smoothing.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with smoothed data.</returns>
        Task<TimeSeriesData> SmoothTimeSeriesAsync(TimeSeriesData sourceData, SmoothingMethod method, int windowSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Normalizes time series data to a 0-1 range or Z-scores.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="method">The normalization method to use.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with normalized data.</returns>
        Task<TimeSeriesData> NormalizeTimeSeriesAsync(TimeSeriesData sourceData, NormalizationMethod method, CancellationToken cancellationToken = default);

        /// <summary>
        /// Interpolates missing values in time series data.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="method">The interpolation method to use.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with interpolated data.</returns>
        Task<TimeSeriesData> InterpolateMissingValuesAsync(TimeSeriesData sourceData, InterpolationMethod method, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates moving statistics for time series data.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="statistic">The statistic to calculate.</param>
        /// <param name="windowSize">The window size for calculation.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with moving statistic data.</returns>
        Task<TimeSeriesData> CalculateMovingStatisticAsync(TimeSeriesData sourceData, MovingStatisticType statistic, int windowSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downsamples time series data to reduce the number of data points.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="targetPointCount">The target number of data points.</param>
        /// <param name="method">The downsampling method to use.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with downsampled data.</returns>
        Task<TimeSeriesData> DownsampleTimeSeriesAsync(TimeSeriesData sourceData, int targetPointCount, DownsamplingMethod method, CancellationToken cancellationToken = default);

        /// <summary>
        /// Merges multiple time series data instances into a single instance.
        /// </summary>
        /// <param name="dataSources">The time series data instances to merge.</param>
        /// <param name="mergeMethod">The method to use for merging.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A new <see cref="TimeSeriesData"/> instance with merged data.</returns>
        Task<TimeSeriesData> MergeTimeSeriesAsync(IEnumerable<TimeSeriesData> dataSources, MergeMethod mergeMethod, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects anomalies in time series data.
        /// </summary>
        /// <param name="sourceData">The source time series data.</param>
        /// <param name="method">The anomaly detection method to use.</param>
        /// <param name="sensitivity">The sensitivity of the anomaly detection (0.0-1.0).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A collection of detected anomalies.</returns>
        Task<IEnumerable<Anomaly>> DetectAnomaliesAsync(TimeSeriesData sourceData, AnomalyDetectionMethod method, double sensitivity, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents an anomaly detected in time series data.
    /// </summary>
    public class Anomaly
    {
        /// <summary>
        /// Gets or sets the date and time of the anomaly.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the series name the anomaly was detected in.
        /// </summary>
        public string SeriesName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected value at the anomaly point.
        /// </summary>
        public double ExpectedValue { get; set; }

        /// <summary>
        /// Gets or sets the actual value at the anomaly point.
        /// </summary>
        public double ActualValue { get; set; }

        /// <summary>
        /// Gets or sets the score of the anomaly (higher means more anomalous).
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Gets or sets the type of the anomaly.
        /// </summary>
        public AnomalyType Type { get; set; }
    }

    /// <summary>
    /// Specifies the method to use for time series smoothing.
    /// </summary>
    public enum SmoothingMethod
    {
        /// <summary>
        /// Moving average smoothing.
        /// </summary>
        MovingAverage,

        /// <summary>
        /// Exponential smoothing.
        /// </summary>
        ExponentialSmoothing,

        /// <summary>
        /// Savitzky-Golay filter.
        /// </summary>
        SavitzkyGolay,

        /// <summary>
        /// Gaussian smoothing.
        /// </summary>
        Gaussian
    }

    /// <summary>
    /// Specifies the method to use for time series normalization.
    /// </summary>
    public enum NormalizationMethod
    {
        /// <summary>
        /// Min-max normalization to 0-1 range.
        /// </summary>
        MinMax,

        /// <summary>
        /// Z-score normalization (mean 0, standard deviation 1).
        /// </summary>
        ZScore,

        /// <summary>
        /// Decimal scaling normalization.
        /// </summary>
        DecimalScaling,

        /// <summary>
        /// Log normalization.
        /// </summary>
        Log
    }

    /// <summary>
    /// Specifies the method to use for time series interpolation.
    /// </summary>
    public enum InterpolationMethod
    {
        /// <summary>
        /// Linear interpolation.
        /// </summary>
        Linear,

        /// <summary>
        /// Nearest neighbor interpolation.
        /// </summary>
        NearestNeighbor,

        /// <summary>
        /// Cubic spline interpolation.
        /// </summary>
        CubicSpline,

        /// <summary>
        /// PCHIP (Piecewise Cubic Hermite Interpolating Polynomial) interpolation.
        /// </summary>
        PCHIP
    }

    /// <summary>
    /// Specifies the type of moving statistic to calculate.
    /// </summary>
    public enum MovingStatisticType
    {
        /// <summary>
        /// Moving average.
        /// </summary>
        Average,

        /// <summary>
        /// Moving standard deviation.
        /// </summary>
        StandardDeviation,

        /// <summary>
        /// Moving minimum.
        /// </summary>
        Minimum,

        /// <summary>
        /// Moving maximum.
        /// </summary>
        Maximum,

        /// <summary>
        /// Moving sum.
        /// </summary>
        Sum,

        /// <summary>
        /// Moving median.
        /// </summary>
        Median
    }

    /// <summary>
    /// Specifies the method to use for time series downsampling.
    /// </summary>
    public enum DownsamplingMethod
    {
        /// <summary>
        /// Largest-Triangle-Three-Buckets algorithm.
        /// </summary>
        LTTB,

        /// <summary>
        /// Mode-Median-Bucket algorithm.
        /// </summary>
        MMB,

        /// <summary>
        /// Min-Max downsampling.
        /// </summary>
        MinMax,

        /// <summary>
        /// Simple averaging.
        /// </summary>
        Average
    }

    /// <summary>
    /// Specifies the method to use for merging time series data.
    /// </summary>
    public enum MergeMethod
    {
        /// <summary>
        /// Concatenate series as they are.
        /// </summary>
        Concatenate,

        /// <summary>
        /// Add values from series with the same name.
        /// </summary>
        Sum,

        /// <summary>
        /// Average values from series with the same name.
        /// </summary>
        Average,

        /// <summary>
        /// Take the maximum value at each point.
        /// </summary>
        Maximum,

        /// <summary>
        /// Take the minimum value at each point.
        /// </summary>
        Minimum
    }

    /// <summary>
    /// Specifies the method to use for anomaly detection.
    /// </summary>
    public enum AnomalyDetectionMethod
    {
        /// <summary>
        /// Z-score based detection.
        /// </summary>
        ZScore,

        /// <summary>
        /// Modified Z-score based detection.
        /// </summary>
        ModifiedZScore,

        /// <summary>
        /// Interquartile range (IQR) based detection.
        /// </summary>
        IQR,

        /// <summary>
        /// Moving average deviation detection.
        /// </summary>
        MovingAverageDeviation,

        /// <summary>
        /// Rate of change detection.
        /// </summary>
        RateOfChange
    }

    /// <summary>
    /// Specifies the type of anomaly detected.
    /// </summary>
    public enum AnomalyType
    {
        /// <summary>
        /// Value spike (significantly higher than expected).
        /// </summary>
        Spike,

        /// <summary>
        /// Value dip (significantly lower than expected).
        /// </summary>
        Dip,

        /// <summary>
        /// Trend shift (sudden change in trend).
        /// </summary>
        TrendShift,

        /// <summary>
        /// Level shift (sudden change in level).
        /// </summary>
        LevelShift,

        /// <summary>
        /// Variance change (sudden change in variance).
        /// </summary>
        VarianceChange,

        /// <summary>
        /// Flat line (value doesn't change for unusual period).
        /// </summary>
        FlatLine
    }
}
