//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// The information of the table being designed.
    /// </summary>
    [DataContract]
    public class TableInfo
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "tooltip")]
        public string Tooltip { get; set; }
        [DataMember(Name = "server")]
        public string Server { get; set; }
        [DataMember(Name = "database")]
        public string Database { get; set; }
        [DataMember(Name = "schema")]
        public string Schema { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "isNewTable")]
        public bool IsNewTable { get; set; }
        [DataMember(Name = "connectionString")]
        public string ConnectionString { get; set; }
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "accessToken")]
        public string AccessToken { get; set; }
        [DataMember(Name = "tableScriptPath")]
        public string TableScriptPath { get; set; }
        [DataMember(Name = "projectFilePath")]
        public string ProjectFilePath { get; set; }
        [DataMember(Name = "allScripts")]
        public List<string> AllScripts { get; set; }
        [DataMember(Name = "targetVersion")]
        public string TargetVersion { get; set; }
    }
}