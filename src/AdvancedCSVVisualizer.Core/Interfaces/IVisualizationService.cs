using AdvancedCSVVisualizer.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedCSVVisualizer.Core.Interfaces
{
    /// <summary>
    /// Provides functionality for creating and managing visualizations.
    /// </summary>
    public interface IVisualizationService
    {
        /// <summary>
        /// Creates a visualization configuration based on the provided data.
        /// </summary>
        /// <param name="data">The time series data to visualize.</param>
        /// <param name="visualizationType">The type of visualization to create.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="VisualizationSettings"/> object with appropriate configuration for the data.</returns>
        Task<VisualizationSettings> CreateVisualizationSettingsAsync(TimeSeriesData data, VisualizationType visualizationType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a collection of color palettes available for visualizations.
        /// </summary>
        /// <returns>A dictionary mapping palette names to color collections.</returns>
        Task<Dictionary<string, IList<string>>> GetAvailableColorPalettesAsync();

        /// <summary>
        /// Exports a visualization to an image file.
        /// </summary>
        /// <param name="data">The time series data to visualize.</param>
        /// <param name="settings">The visualization settings to apply.</param>
        /// <param name="filePath">The file path to save the image to.</param>
        /// <param name="imageFormat">The format of the image.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous export operation.</returns>
        Task ExportToImageAsync(TimeSeriesData data, VisualizationSettings settings, string filePath, 
            ImageFormat imageFormat, int width, int height, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports a visualization to a PDF file.
        /// </summary>
        /// <param name="data">The time series data to visualize.</param>
        /// <param name="settings">The visualization settings to apply.</param>
        /// <param name="filePath">The file path to save the PDF to.</param>
        /// <param name="width">The width of the PDF in points.</param>
        /// <param name="height">The height of the PDF in points.</param>
        /// <param name="includeStatistics">Whether to include statistics in the PDF.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous export operation.</returns>
        Task ExportToPdfAsync(TimeSeriesData data, VisualizationSettings settings, string filePath,
            float width, float height, bool includeStatistics, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a line chart visualization model from the provided data and settings.
        /// </summary>
        /// <param name="data">The time series data to visualize.</param>
        /// <param name="settings">The visualization settings to apply.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A visualization model that can be used with the UI components.</returns>
        Task<object> CreateLineChartModelAsync(TimeSeriesData data, VisualizationSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a bar chart visualization model from the provided data and settings.
        /// </summary>
        /// <param name="data">The time series data to visualize.</param>
        /// <param name="settings">The visualization settings to apply.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A visualization model that can be used with the UI components.</returns>
        Task<object> CreateBarChartModelAsync(TimeSeriesData data, VisualizationSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a pie chart visualization model from the provided data and settings.
        /// </summary>
        /// <param name="data">The time series data to visualize.</param>
        /// <param name="settings">The visualization settings to apply.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A visualization model that can be used with the UI components.</returns>
        Task<object> CreatePieChartModelAsync(TimeSeriesData data, VisualizationSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a diverging bar chart visualization model from the provided data and settings.
        /// </summary>
        /// <param name="data">The time series data to visualize.</param>
        /// <param name="settings">The visualization settings to apply.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A visualization model that can be used with the UI components.</returns>
        Task<object> CreateDivergingBarChartModelAsync(TimeSeriesData data, VisualizationSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a visualization model with new data or settings.
        /// </summary>
        /// <param name="model">The visualization model to update.</param>
        /// <param name="data">The new time series data.</param>
        /// <param name="settings">The new visualization settings.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The updated visualization model.</returns>
        Task<object> UpdateVisualizationModelAsync(object model, TimeSeriesData data, VisualizationSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a stream containing the visualization as an image.
        /// </summary>
        /// <param name="model">The visualization model to render.</param>
        /// <param name="imageFormat">The format of the image.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A stream containing the visualization image.</returns>
        Task<Stream> GetVisualizationStreamAsync(object model, ImageFormat imageFormat, int width, int height, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recommended visualization settings for the provided data.
        /// </summary>
        /// <param name="data">The time series data to analyze.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="VisualizationSettings"/> object with recommended settings.</returns>
        Task<VisualizationSettings> GetRecommendedSettingsAsync(TimeSeriesData data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a dictionary of data point information for tooltips.
        /// </summary>
        /// <param name="data">The time series data.</param>
        /// <param name="seriesName">The name of the series.</param>
        /// <param name="pointIndex">The index of the data point.</param>
        /// <returns>A dictionary of tooltip information.</returns>
        Dictionary<string, string> GetTooltipInfo(TimeSeriesData data, string seriesName, int pointIndex);
    }

    /// <summary>
    /// Specifies the format of exported images.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// PNG image format.
        /// </summary>
        PNG,

        /// <summary>
        /// JPEG image format.
        /// </summary>
        JPEG,

        /// <summary>
        /// BMP image format.
        /// </summary>
        BMP,

        /// <summary>
        /// TIFF image format.
        /// </summary>
        TIFF,

        /// <summary>
        /// SVG image format.
        /// </summary>
        SVG
    }
}
