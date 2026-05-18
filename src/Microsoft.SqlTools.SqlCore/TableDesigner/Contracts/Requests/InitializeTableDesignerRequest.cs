//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    /// <summary>
    /// Parameters for initializing a table designer session.
    /// </summary>
    [DataContract]
    public class InitializeTableDesignerRequestParams
    {
        /// <summary>
        /// Unique identifier for the table designer session.
        /// </summary>
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }

        /// <summary>
        /// The table information used to initialize the designer.
        /// </summary>
        [DataMember(Name = "tableInfo")]
        public TableInfo TableInfo { get; set; }
    }

    /// <summary>
    /// The service request to initialize a table designer.
    /// </summary>
    public class InitializeTableDesignerRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<InitializeTableDesignerRequestParams, TableDesignerInfo> Type = RequestType<InitializeTableDesignerRequestParams, TableDesignerInfo>.Create("tabledesigner/initialize");
    }
}
