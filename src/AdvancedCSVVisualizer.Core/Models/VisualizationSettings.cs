using MemoryPack;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace AdvancedCSVVisualizer.Core.Models
{
    /// <summary>
    /// Represents settings for data visualization.
    /// </summary>
    [MemoryPackable]
    public partial class VisualizationSettings
    {
        /// <summary>
        /// Gets or sets the type of visualization.
        /// </summary>
        public VisualizationType VisualizationType { get; set; } = VisualizationType.LineChart;

        /// <summary>
        /// Gets or sets the title of the visualization.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subtitle of the visualization.
        /// </summary>
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to show the title.
        /// </summary>
        public bool ShowTitle { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the subtitle.
        /// </summary>
        public bool ShowSubtitle { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the legend.
        /// </summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>
        /// Gets or sets the position of the legend.
        /// </summary>
        public LegendPosition LegendPosition { get; set; } = LegendPosition.Right;

        /// <summary>
        /// Gets or sets a value indicating whether to show data point indicators.
        /// </summary>
        public bool ShowDataPoints { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show grid lines.
        /// </summary>
        public bool ShowGridLines { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable zooming.
        /// </summary>
        public bool EnableZoom { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show tooltips.
        /// </summary>
        public bool ShowTooltips { get; set; } = true;

        /// <summary>
        /// Gets or sets the animation speed in milliseconds.
        /// </summary>
        public int AnimationSpeed { get; set; } = 300;

        /// <summary>
        /// Gets or sets the format string for the X-axis labels.
        /// </summary>
        public string XAxisFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>
        /// Gets or sets the title of the X-axis.
        /// </summary>
        public string XAxisTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to show the X-axis title.
        /// </summary>
        public bool ShowXAxisTitle { get; set; } = false;

        /// <summary>
        /// Gets or sets the format string for the Y-axis labels.
        /// </summary>
        public string YAxisFormat { get; set; } = "0.##";

        /// <summary>
        /// Gets or sets the title of the Y-axis.
        /// </summary>
        public string YAxisTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to show the Y-axis title.
        /// </summary>
        public bool ShowYAxisTitle { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum limit for the Y-axis.
        /// </summary>
        public double? YAxisMinLimit { get; set; }

        /// <summary>
        /// Gets or sets the maximum limit for the Y-axis.
        /// </summary>
        public double? YAxisMaxLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to auto-scale the Y-axis.
        /// </summary>
        public bool AutoScaleYAxis { get; set; } = true;

        /// <summary>
        /// Gets or sets the time granularity for the X-axis.
        /// </summary>
        public TimeGranularity TimeGranularity { get; set; } = TimeGranularity.Day;

        /// <summary>
        /// Gets or sets the thickness of the lines in line charts.
        /// </summary>
        public int LineThickness { get; set; } = 2;

        /// <summary>
        /// Gets or sets the size of the data points.
        /// </summary>
        public int DataPointSize { get; set; } = 6;

        /// <summary>
        /// Gets or sets the theme for the visualization.
        /// </summary>
        public VisualizationTheme Theme { get; set; } = VisualizationTheme.Dark;

        /// <summary>
        /// Gets or sets the color palette for the visualization.
        /// </summary>
        public ColorPalette ColorPalette { get; set; } = ColorPalette.Default;

        /// <summary>
        /// Gets or sets the custom colors for the visualization.
        /// </summary>
        [MemoryPackIgnore]
        public List<Color> CustomColors { get; set; } = new List<Color>();

        /// <summary>
        /// Gets or sets the hex color codes for serialization purposes.
        /// </summary>
        public List<string> HexColors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether to use custom colors.
        /// </summary>
        public bool UseCustomColors { get; set; } = false;

        /// <summary>
        /// Gets or sets additional settings specific to the visualization type.
        /// </summary>
        public Dictionary<string, object> TypeSpecificSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the chart aspect ratio (width / height).
        /// </summary>
        public double AspectRatio { get; set; } = 1.6;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationSettings"/> class.
        /// </summary>
        public VisualizationSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationSettings"/> class with the specified type.
        /// </summary>
        /// <param name="visualizationType">The type of visualization.</param>
        public VisualizationSettings(VisualizationType visualizationType)
        {
            VisualizationType = visualizationType;

            // Set default settings based on visualization type
            switch (visualizationType)
            {
                case VisualizationType.LineChart:
                    ShowDataPoints = true;
                    LineThickness = 2;
                    break;

                case VisualizationType.BarChart:
                    ShowDataPoints = false;
                    // Set bar chart specific defaults
                    TypeSpecificSettings["BarSpacing"] = 0.2;
                    TypeSpecificSettings["GroupSpacing"] = 0.5;
                    TypeSpecificSettings["IsStacked"] = false;
                    break;

                case VisualizationType.PieChart:
                    ShowDataPoints = false;
                    ShowGridLines = false;
                    // Set pie chart specific defaults
                    TypeSpecificSettings["ShowPercentages"] = true;
                    TypeSpecificSettings["ShowLabels"] = true;
                    TypeSpecificSettings["IsDonut"] = false;
                    TypeSpecificSettings["InnerRadius"] = 0.0;
                    break;

                case VisualizationType.DivergingBar:
                    ShowDataPoints = false;
                    // Set diverging bar chart specific defaults
                    TypeSpecificSettings["ZeroLineThickness"] = 2;
                    TypeSpecificSettings["PositiveColor"] = "#4CAF50"; // Green
                    TypeSpecificSettings["NegativeColor"] = "#F44336"; // Red
                    TypeSpecificSettings["SortByMagnitude"] = false;
                    break;
            }
        }

        /// <summary>
        /// Creates a copy of the visualization settings.
        /// </summary>
        /// <returns>A new instance of <see cref="VisualizationSettings"/> with the same values.</returns>
        public VisualizationSettings Clone()
        {
            var clone = new VisualizationSettings
            {
                VisualizationType = VisualizationType,
                Title = Title,
                Subtitle = Subtitle,
                ShowTitle = ShowTitle,
                ShowSubtitle = ShowSubtitle,
                ShowLegend = ShowLegend,
                LegendPosition = LegendPosition,
                ShowDataPoints = ShowDataPoints,
                ShowGridLines = ShowGridLines,
                EnableZoom = EnableZoom,
                ShowTooltips = ShowTooltips,
                AnimationSpeed = AnimationSpeed,
                XAxisFormat = XAxisFormat,
                XAxisTitle = XAxisTitle,
                ShowXAxisTitle = ShowXAxisTitle,
                YAxisFormat = YAxisFormat,
                YAxisTitle = YAxisTitle,
                ShowYAxisTitle = ShowYAxisTitle,
                YAxisMinLimit = YAxisMinLimit,
                YAxisMaxLimit = YAxisMaxLimit,
                AutoScaleYAxis = AutoScaleYAxis,
                TimeGranularity = TimeGranularity,
                LineThickness = LineThickness,
                DataPointSize = DataPointSize,
                Theme = Theme,
                ColorPalette = ColorPalette,
                UseCustomColors = UseCustomColors,
                AspectRatio = AspectRatio,
                HexColors = new List<string>(HexColors)
            };

            // Clone custom colors
            clone.CustomColors = new List<Color>(CustomColors);

            // Clone type specific settings
            foreach (var kvp in TypeSpecificSettings)
            {
                clone.TypeSpecificSettings[kvp.Key] = kvp.Value;
            }

            return clone;
        }
    }

    /// <summary>
    /// Specifies the type of visualization.
    /// </summary>
    public enum VisualizationType
    {
        /// <summary>
        /// Line chart visualization.
        /// </summary>
        LineChart,

        /// <summary>
        /// Bar chart visualization.
        /// </summary>
        BarChart,

        /// <summary>
        /// Pie chart visualization.
        /// </summary>
        PieChart,

        /// <summary>
        /// Diverging bar chart visualization.
        /// </summary>
        DivergingBar
    }

    /// <summary>
    /// Specifies the position of the legend.
    /// </summary>
    public enum LegendPosition
    {
        /// <summary>
        /// Legend at the top of the chart.
        /// </summary>
        Top,

        /// <summary>
        /// Legend at the right of the chart.
        /// </summary>
        Right,

        /// <summary>
        /// Legend at the bottom of the chart.
        /// </summary>
        Bottom,

        /// <summary>
        /// Legend at the left of the chart.
        /// </summary>
        Left
    }

    /// <summary>
    /// Specifies the granularity of time data.
    /// </summary>
    public enum TimeGranularity
    {
        /// <summary>
        /// Minute granularity.
        /// </summary>
        Minute,

        /// <summary>
        /// Hour granularity.
        /// </summary>
        Hour,

        /// <summary>
        /// Day granularity.
        /// </summary>
        Day,

        /// <summary>
        /// Week granularity.
        /// </summary>
        Week,

        /// <summary>
        /// Month granularity.
        /// </summary>
        Month,

        /// <summary>
        /// Quarter granularity.
        /// </summary>
        Quarter,

        /// <summary>
        /// Year granularity.
        /// </summary>
        Year
    }

    /// <summary>
    /// Specifies the theme for the visualization.
    /// </summary>
    public enum VisualizationTheme
    {
        /// <summary>
        /// Light theme (white background).
        /// </summary>
        Light,

        /// <summary>
        /// Dark theme (dark background).
        /// </summary>
        Dark,

        /// <summary>
        /// Custom theme defined by user settings.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Specifies the color palette for the visualization.
    /// </summary>
    public enum ColorPalette
    {
        /// <summary>
        /// Default application color palette.
        /// </summary>
        Default,

        /// <summary>
        /// Pastel colors palette.
        /// </summary>
        Pastel,

        /// <summary>
        /// Vibrant colors palette.
        /// </summary>
        Vibrant,

        /// <summary>
        /// Monochromatic colors palette.
        /// </summary>
        Monochromatic,

        /// <summary>
        /// Custom colors defined by user.
        /// </summary>
        Custom
    }
}
