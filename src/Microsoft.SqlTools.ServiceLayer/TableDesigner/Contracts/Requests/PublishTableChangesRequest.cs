//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class PublishTableChangesResponse
    {
        public TableInfo NewTableInfo;
        public TableViewModel ViewModel;
        public TableDesignerView View;
        public Dictionary<string, string> PropertyBag;
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
