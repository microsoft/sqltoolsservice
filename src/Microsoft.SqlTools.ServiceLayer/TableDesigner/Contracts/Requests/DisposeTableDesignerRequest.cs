//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class DisposeTableDesignerResponse
    {
    }

    /// <summary>
    /// The request to dispose the table designer.
    /// </summary>
    public class DisposeTableDesignerRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<TableInfo, DisposeTableDesignerResponse> Type = RequestType<TableInfo, DisposeTableDesignerResponse>.Create("tabledesigner/dispose");
    }
}
