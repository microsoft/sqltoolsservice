//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    [DataContract]
    public class PublishTableChangesResponse
    {
        [DataMember(Name = "newTableInfo")]
        public TableInfo NewTableInfo;
        [DataMember(Name = "viewModel")]
        public TableViewModel ViewModel;
        [DataMember(Name = "view")]
        public TableDesignerView View;
        [DataMember(Name = "metadata")]
        public Dictionary<string, string> Metadata;
    }

    /// <summary>
    /// The service request to publish the changes.
    /// </summary>
    public class PublishTableChangesRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<TableInfo, PublishTableChangesResponse> Type = RequestType<TableInfo, PublishTableChangesResponse>.Create("tabledesigner/publish");
    }
}
