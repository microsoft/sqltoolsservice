//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Defines paramaters for Get default options call
    /// </summary>
    public class SchemaCompareGetOptionsParams
    {
        /// <summary>
        /// When true, normalizes the 7 STS-overridden options back to DacFx native defaults.
        /// This should be true for Publish operations, false for Schema Compare operations.
        /// Defaults to false for backward compatibility.
        /// </summary>
        public bool NormalizeToNativeDefaults { get; set; } = false;
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
    /// TODO: Consider renaming to a more generic name (e.g., "dacfx/getDefaultDeploymentOptions")
    /// since this endpoint is now used by both Schema Compare and Publish operations.
    /// </summary>
    class SchemaCompareGetDefaultOptionsRequest
    {
        public static readonly RequestType<SchemaCompareGetOptionsParams, SchemaCompareOptionsResult> Type =
            RequestType<SchemaCompareGetOptionsParams, SchemaCompareOptionsResult>.Create("schemaCompare/getDefaultOptions");
    }


}
