//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class MigrationAssessmentsParams 
    {
        public string OwnerUri { get; set; }
    }

    public class MigrationAssessmentsResult
    {
        /// <summary>
        /// Gets or sets a value indicating
        /// if assessment operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets an status message for the operation.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Retreive metadata for the table described in the TableMetadataParams value
    /// </summary>
    public class MigrationAssessmentsRequest
    {
        public static readonly
            RequestType<MigrationAssessmentsParams, MigrationAssessmentsResult> Type =
                RequestType<MigrationAssessmentsParams, MigrationAssessmentsResult>.Create("migration/assessments");
    }
}
