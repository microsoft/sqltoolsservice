//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data.Common;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Interface for a factory that generates metadata for an object to edit
    /// </summary>
    public interface IEditMetadataFactory
    {
        /// <summary>
        /// Generates a edit-ready metadata object
        /// </summary>
        /// <param name="connection">Connection to use for getting metadata</param>
        /// <param name="objectName">Name of the object to return metadata for</param>
        /// <param name="objectType">Type of the object to return metadata for</param>
        /// <returns>Metadata about the object requested</returns>
        EditTableMetadata GetObjectMetadata(DbConnection connection, string objectName, string objectType);
    }
}