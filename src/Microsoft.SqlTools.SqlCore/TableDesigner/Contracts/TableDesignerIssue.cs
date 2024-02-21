//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IssueSeverity
    {
        [EnumMember(Value = "error")]
        Error,
        [EnumMember(Value = "warning")]
        Warning,
        [EnumMember(Value = "information")]
        Information,
    }

    /// <summary>
    /// Table Designer Issue
    /// </summary>
    [DataContract]
    public class TableDesignerIssue
    {
        /// <summary>
        /// The description.
        /// </summary>
        [DataMember(Name = "description")]
        public string Description { get; set; }

        /// <summary>
        /// The property path associated with the message
        /// </summary>
        [DataMember(Name = "propertyPath")]
        public object[] PropertyPath { get; set; }

        /// <summary>
        /// The severity of the message. Default value is Error.
        /// </summary>
        [DataMember(Name = "severity")]
        public IssueSeverity Severity { get; set; } = IssueSeverity.Error;

        /// <summary>
        /// Any link to docs associated with error for more information
        /// </summary>
        [DataMember(Name = "moreInfoLink")]
        public string MoreInfoLink { get; set; }
    }
}