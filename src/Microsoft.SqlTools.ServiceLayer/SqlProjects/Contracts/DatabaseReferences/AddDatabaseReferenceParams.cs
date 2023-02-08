//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Base class for add database reference request paramaters
    /// </summary>
    public abstract class AddDatabaseReferenceParams : SqlProjectParams
    {
        /// <summary>
        /// Whether to suppress missing dependencies
        /// </summary>
        public bool SuppressMissingDependencies { get; set; }

        /// <summary>
        /// SQLCMD variable name for specifying the other database this reference is to, if different from that of the current project
        /// </summary>
        public string? DatabaseVariable { get; set; }
    }
}