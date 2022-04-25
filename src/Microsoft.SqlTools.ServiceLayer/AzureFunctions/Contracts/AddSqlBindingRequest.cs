//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts
{
    /// <summary>
    /// Binding types for sql bindings for Azure Functions
    /// </summary>
    public enum BindingType
    {
        input,
        output
    }

    /// <summary>
    /// Parameters for adding a sql binding
    /// </summary>
    public class AddSqlBindingParams
    {
        /// <summary>
        /// Gets or sets the filePath
        /// </summary>
        public string filePath { get; set; }

        /// <summary>
        /// Gets or sets the binding type
        /// </summary>
        public BindingType bindingType { get; set; }

        /// <summary>
        /// Gets or sets the function name
        /// </summary>
        public string functionName { get; set; }

        /// <summary>
        /// Gets or sets the object name
        /// </summary>
        public string objectName { get; set; }

        /// <summary>
        /// Gets or sets the connection string setting
        /// </summary>
        public string connectionStringSetting { get; set; }
    }

    /// <summary>
    /// Defines the Add Sql Binding request
    /// </summary>
    class AddSqlBindingRequest
    {
        public static readonly RequestType<AddSqlBindingParams, ResultStatus> Type =
            RequestType<AddSqlBindingParams, ResultStatus>.Create("azureFunctions/sqlBinding");
    }
}
