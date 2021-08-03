﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
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
    /// Parameters for inserting a sql binding
    /// </summary>
    public class InsertSqlBindingParams
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
    /// Defines the Insert Sql Binding request
    /// </summary>
    class InsertSqlBindingRequest
    {
        public static readonly RequestType<InsertSqlBindingParams, ResultStatus> Type =
            RequestType<InsertSqlBindingParams, ResultStatus>.Create("azureFunctions/sqlBinding");

    }
}
