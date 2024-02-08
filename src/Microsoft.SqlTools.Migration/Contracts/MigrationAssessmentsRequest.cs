//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public class MigrationAssessmentsParams 
    {
        /// <summary>
        /// Owner URI
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// List of databases to assess
        /// </summary>
        public string[] Databases { get; set; }

        /// <summary>
        /// Folder path to XEvents files to be assessed, if applicable. Empty string to disable XEvents assessment.
        /// </summary>
        public string XEventsFilesFolderPath { get; set; }

        /// <summary>
        /// Bool to enable ad-hoc query collection and extraction through QDS. False if not set.
        /// </summary>
        public bool collectAdhocQueries { get; set; } = false;
    }

    public class MigrationAssessmentResult
    {
        /// <summary>
        /// Errors that happen while running the assessment
        /// </summary>
        public ErrorModel[] Errors { get; set; }
        /// <summary>
        /// Result of the assessment
        /// </summary>
        public ServerAssessmentProperties AssessmentResult { get; set; }
        /// <summary>
        /// Start time of the assessment
        /// </summary>
        public string StartTime { get; set; }
        /// <summary>
        /// End time of the assessment
        /// </summary>
        public string EndedTime { get; set; }
        /// <summary>
        /// Contains the raw assessment response 
        /// </summary>
        public ISqlMigrationAssessmentModel RawAssessmentResult { get; set; }
        /// <summary>
        /// File path where the assessment report was saved
        /// </summary>
        public string AssessmentReportPath { get; set; }
    }

    /// <summary>
    /// Retreive metadata for the table described in the TableMetadataParams value
    /// </summary>
    public class MigrationAssessmentsRequest
    {
        public static readonly
            RequestType<MigrationAssessmentsParams, MigrationAssessmentResult> Type =
                RequestType<MigrationAssessmentsParams, MigrationAssessmentResult>.Create("migration/getassessments");
    }
}
