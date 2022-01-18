﻿using ShapeCrawler.Collections;
using ShapeCrawler.Shapes;

// ReSharper disable once CheckNamespace
namespace ShapeCrawler
{
    /// <summary>
    ///     Represents a chart.
    /// </summary>
    public interface IChart : IShape
    {
        /// <summary>
        ///     Gets chart title. Return <c>NULL</c> if chart does not have title.
        /// </summary>
        ChartType Type { get; }

        /// <summary>
        ///     Gets the chart title. Returns null if the chart has not a title.
        /// </summary>
        string Title { get; }

        /// <summary>
        ///     Gets a value indicating whether the chart has a title.
        /// </summary>
        public bool HasTitle { get; }

        /// <summary>
        ///     Gets a value indicating whether the chart has categories.
        /// </summary>
        /// <remarks>Some chart types like ScatterChart and BubbleChart does not have categories.</remarks>
        bool HasCategories { get; }

        /// <summary>
        ///     Gets collection of the chart series.
        /// </summary>
        ISeriesCollection SeriesCollection { get; }

        /// <summary>
        ///     Gets collection of chart categories.
        /// </summary>
        ICategoryCollection Categories { get; }

        /// <summary>
        ///     Gets a value indicating whether the chart has x-axis values.
        /// </summary>
        bool HasXValues { get; }

        /// <summary>
        ///     Gets collection of x-axis values.
        /// </summary>
        LibraryCollection<double> XValues { get; }

        /// <summary>
        ///     Gets workbook byte array containing chart data.
        /// </summary>
        byte[] WorkbookByteArray { get; }

        ISlide ParentSlide { get; }
    }
}