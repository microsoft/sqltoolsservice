using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
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