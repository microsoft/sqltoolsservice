//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Defines paramaters for Get default options call
    /// No parameters required so far
    /// </summary>
    public class SchemaCompareGetOptionsParams
    {
    }

    /// <summary>
    /// Gets or sets the result of get default options call
    /// </summary>
    public class SchemaCompareOptionsResult : ResultStatus
    {
        public DeploymentOptions DefaultDeploymentOptions { get; set; }        
    }

    /// <summary>
    /// Defines the Schema Compare request type
    /// </summary>
    class SchemaCompareGetDefaultOptionsRequest
    {
        public static readonly RequestType<SchemaCompareGetOptionsParams, SchemaCompareOptionsResult> Type =
            RequestType<SchemaCompareGetOptionsParams, SchemaCompareOptionsResult>.Create("schemaCompare/getDefaultOptions");
    }


}
