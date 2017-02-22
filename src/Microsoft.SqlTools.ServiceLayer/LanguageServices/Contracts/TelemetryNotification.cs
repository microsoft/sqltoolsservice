//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    public class TelemetryProperties
    {
        public string EventName { get; set; }

        /// <summary>
        /// Telemetry properties
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Telemetry measures
        /// </summary>
        public Dictionary<string, double> Measures { get; set; }
    }

    /// <summary>
    /// Parameters sent back with an IntelliSense ready event
    /// </summary>
    public class TelemetryParams
    {
        public TelemetryProperties Params { get; set; }
    }

    /// <summary>
    /// Event sent when the language service needs to add a telemetry event
    /// </summary>
    public class TelemetryNotification
    {
        public static readonly
            EventType<TelemetryParams> Type =
            EventType<TelemetryParams>.Create("telemetry/sqlevent");
    }

    /// <summary>
    /// List of telemetry events
    /// </summary>
    public static class TelemetryEventNames
    {
        /// <summary>
        /// telemetry event name for auto complete response time
        /// </summary>
        public const string IntellisenseQuantile = "IntellisenseQuantile";

        /// <summary>
        /// telemetry event name for when definition is requested
        /// </summary>
        public const string PeekDefinitionRequested = "PeekDefinitionRequested";

        /// <summary>
        /// telemetry event name for when definition is requested
        /// </summary>
        public const string FormatCode = "FormatCode";
    }

    /// <summary>
    ///  List of properties used in telemetry events
    /// </summary>
    public static class TelemetryPropertyNames
    {
        /// <summary>
        /// Is a connection to an Azure database or not
        /// </summary>
        public const string IsAzure = "IsAzure";

        /// <summary>
        /// Did an event succeed or not
        /// </summary>
        public const string Succeeded = "Succeeded";

        /// <summary>
        /// Was the action against a connected file or similar resource, or not
        /// </summary>
        public const string Connected = "Connected";

        /// <summary>
        /// Format type property - should be one of <see cref="DocumentFormatType"/> or <see cref="RangeFormatType"/>
        /// </summary>
        public const string FormatType = "FormatType";

        /// <summary>
        /// A full document format
        /// </summary>
        public const string DocumentFormatType = "Document";

        /// <summary>
        /// A document range format
        /// </summary>
        public const string RangeFormatType = "Range";
    }
}
