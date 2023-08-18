//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class GenerateDatabaseServerContextualizationParams
    {
        /// <summary>
        /// The URI of the connection to generate scripts for.
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Event set after a connection to a database server is completed.
    /// </summary>
    public class GenerateDatabaseServerContextualizationNotification
    {
        public static readonly EventType<GenerateDatabaseServerContextualizationParams> Type =
            EventType<GenerateDatabaseServerContextualizationParams>.Create("metadata/generateDatabaseServerContextScripts");
    }
}
