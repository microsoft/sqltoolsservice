//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Runtime.Serialization;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.LanguageService.Formatter.Contracts
{
    /// <summary>
    /// Notification sent when a formatting request cannot produce edits.
    /// </summary>
    public class FormattingFailedNotification
    {
        public static readonly EventType<FormattingFailedParams> Type =
            EventType<FormattingFailedParams>.Create("textDocument/formattingFailed");
    }

    /// <summary>
    /// Parameters describing a formatting failure.
    /// </summary>
    public class FormattingFailedParams
    {
        /// <summary>
        /// URI of the document that could not be formatted.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Whether document or range formatting was requested.
        /// </summary>
        public FormattingRequestType FormatType { get; set; }

        /// <summary>
        /// Reason formatting could not be completed.
        /// </summary>
        public FormattingFailureReason Reason { get; set; }

        /// <summary>
        /// Number of parse errors found in the formatted text.
        /// </summary>
        public int ParseErrorCount { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FormattingFailureReason
    {
        [EnumMember(Value = "ParseError")]
        ParseError
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FormattingRequestType
    {
        [EnumMember(Value = "Document")]
        Document,

        [EnumMember(Value = "Range")]
        Range
    }
}
