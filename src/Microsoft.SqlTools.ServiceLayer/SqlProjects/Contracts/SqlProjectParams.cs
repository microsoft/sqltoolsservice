//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for a generic SQL Project operation
    /// </summary>
    public class SqlProjectParams : GeneralRequestDetails
    {
        /// <summary>
        /// Absolute path of the project, including .sqlproj
        /// </summary>
        public string ProjectUri { get; set; }
    }

    /// <summary>
    /// Parameters for a SQL Project operation that targets a script
    /// </summary>
    public class SqlProjectScriptParams : SqlProjectParams
    {
        /// <summary>
        /// Path of the script, including .sql, relative to the .sqlproj
        /// </summary>
        public string Path { get; set; }
    }

    /// <summary>
    /// Result from a generic SQL Project operation
    /// </summary>
    public class SqlProjectResult
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message from the exception that was thrown, if any.  Null if operation was successful.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
