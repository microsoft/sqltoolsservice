//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Common
{
    /// <summary>
    /// Describes the content type that a client supports in various result literals like Hover,
    /// ParameterInfo, or CompletionItem.
    /// 
    /// Please note that MarkupKinds must not start with a '$'. These kinds are reserved for
    /// internal usage.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MarkupKind
    {
        PlainText = 1,
        Markdown = 2
    }
}