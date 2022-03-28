//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The service request to generate script for the changes.
    /// </summary>
    public class GenerateScriptRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly RequestType<TableInfo, string> Type = RequestType<TableInfo, string>.Create("tabledesigner/script");
    }
}
