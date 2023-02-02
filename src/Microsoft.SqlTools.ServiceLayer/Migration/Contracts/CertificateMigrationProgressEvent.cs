//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    /// <summary>
    /// Parameters for the certificate migration progress event
    /// </summary>
    public class CertificateMigrationProgressParams
    {
        /// <summary>
        /// Database name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Message related to the success status. true should describe a positive outcome. false should have an error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Result of migration
        /// </summary>
        public bool Success { get; set; }
    }


    /// <summary>
    /// Create a certificate migration request. This should be register at the client.
    /// </summary>
    public class CertificateMigrationProgressEvent
    {
        /// <summary>
        /// Name and parameters for the event definition.
        /// </summary>
        public static readonly
            EventType<CertificateMigrationProgressParams> Type =
                EventType<CertificateMigrationProgressParams>.Create("migration/tdemigrationprogress");
    }
}
