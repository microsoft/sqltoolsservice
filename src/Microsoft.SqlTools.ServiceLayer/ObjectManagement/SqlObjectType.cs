//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SqlObjectType
    {
        [EnumMember(Value = "Column")]
        Column,
        [EnumMember(Value = "Credential")]
        Credential,
        [EnumMember(Value = "ServerLevelLogin")]
        ServerLevelLogin,
        [EnumMember(Value = "Table")]
        Table,
        [EnumMember(Value = "User")]
        User,
        [EnumMember(Value = "View")]
        View,
        [EnumMember(Value = "ServerRole")]
        ServerRole,
        [EnumMember(Value = "DatabaseRole")]
        DatabaseRole,
        [EnumMember(Value = "ApplicationRole")]
        ApplicationRole
    }
}