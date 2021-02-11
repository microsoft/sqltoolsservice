//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Represents a summary of information about a result without returning any cells of the results
    /// </summary>
    public class ResultSetSummary
    {
        /// <summary>
        /// The ID of the result set within the batch results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the batch set within the query
        /// </summary>
        public int BatchId { get; set; }

        /// <summary>
        /// The number of rows that are available for the resultset thus far
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// If true it indicates that all rows have been fetched and the RowCount being sent across is final for this ResultSet
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public DbColumnWrapper[] ColumnInfo { get; set; }

        /// <summary>
        /// The special action definition of the result set 
        /// </summary>
        public SpecialAction SpecialAction { get; set; }

        /// <summary>
        /// The visualization options for the client to render charts.
        /// </summary>
        public VisualizationOptions Visualization { get; set; }

        /// <summary>
        /// Returns a string represents the current object.
        /// </summary>
        public override string ToString() => $"Result Summary Id:{Id}, Batch Id:'{BatchId}', RowCount:'{RowCount}', Complete:'{Complete}', SpecialAction:'{SpecialAction}', Visualization:'{Visualization}'";
    }

    /// <summary>
    /// Represents the configuration options for data visualization
    /// </summary>
    public class VisualizationOptions
    {
        /// <summary>
        /// Gets or sets the type of the visualization
        /// </summary>
        public VisualizationType Type { get; set; }
    }

    /// <summary>
    /// The supported visualization types
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VisualizationType
    {
        [EnumMember(Value = "bar")]
        Bar,
        [EnumMember(Value = "count")]
        Count,
        [EnumMember(Value = "doughnut")]
        Doughnut,
        [EnumMember(Value = "horizontalBar")]
        HorizontalBar,
        [EnumMember(Value = "image")]
        Image,
        [EnumMember(Value = "line")]
        Line,
        [EnumMember(Value = "pie")]
        Pie,
        [EnumMember(Value = "scatter")]
        Scatter,
        [EnumMember(Value = "table")]
        Table,
        [EnumMember(Value = "timeSeries")]
        TimeSeries
    }
}
