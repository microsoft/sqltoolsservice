//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.TargetAssessment.Models;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class ServerAssessmentProperties
    {
        /// <summary>
        /// Name of the server
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Cpu cores for the server host
        /// </summary>
        public long CpuCoreCount { get; set; }
        /// <summary>
        /// Server host physical memory size
        /// </summary>
        public double PhysicalServerMemory { get; set; }
        /// <summary>
        /// Host operating system of the SQL server
        /// </summary>
        public string ServerHostPlatform { get; set; }
        /// <summary>
        /// Version of the SQL server
        /// </summary>
        public string ServerVersion { get; set; }
        /// <summary>
        /// SQL server engine edition
        /// </summary>
        public string ServerEngineEdition { get; set; }
        /// <summary>
        /// SQL server edition
        /// </summary>
        public string ServerEdition { get; set; }
        /// <summary>
        /// We use this flag to indicate if the SQL server is part of the failover cluster
        /// </summary>
        public bool IsClustered { get; set; }
        /// <summary>
        /// Returns the total number of dbs assessed
        /// </summary>
        public long NumberOfUserDatabases { get; set; }
        /// <summary>
        /// Returns the assessment status
        /// </summary>
        public int SqlAssessmentStatus { get; set; }
        /// <summary>
        /// Count of Dbs assessed
        /// </summary>
        public long AssessedDatabaseCount{get; set;}
        /// <summary>
        /// Give assessed server stats for SQL MI compatibility
        /// </summary>
        public IServerTargetReadiness SQLManagedInstanceTargetReadiness { get; set; }
        /// <summary>
        /// Server assessment results
        /// </summary>
        public MigrationAssessmentInfo[] Items { get; set; }
        /// <summary>
        /// Server assessment errors
        /// </summary>
        public ErrorModel[] Errors { get; set; }
        /// <summary>
        /// List of databases that are assessed
        /// </summary>
        public DatabaseAssessmentProperties[] Databases { get; set; }
    }

    public class DatabaseAssessmentProperties
    {
        /// <summary>
        /// Name of the database
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Compatibility level of the database
        /// </summary>
        public string CompatibilityLevel { get; set; }
        /// <summary>
        /// Size of the database
        /// </summary>
        public double DatabaseSize { get; set; }
        /// <summary>
        /// Flag that indicates if the database is replicated
        /// </summary>
        public bool IsReplicationEnabled { get; set; }
        /// <summary>
        /// Time taken for assessing the database
        /// </summary>
        public double AssessmentTimeInMilliseconds { get; set; }
        /// <summary>
        /// Database Assessment Results
        /// </summary>
        public MigrationAssessmentInfo[] Items { get; set; }
        /// <summary>
        /// Database assessment errors
        /// </summary>
        public ErrorModel[] Errors {get; set;}
        /// <summary>
        /// Flags that indicate if the database is ready for migration
        /// </summary>
        public IDatabaseTargetReadiness SQLManagedInstanceTargetReadiness { get; set; }
    }

    public class ErrorModel
    {
        /// <summary>
        /// Id of the assessment error
        /// </summary>
        public string ErrorId { get; set; }
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Summary of the Error
        /// </summary>
        public string ErrorSummary { get; set; }
        /// <summary>
        /// Possible causes for the error
        /// </summary>
        public string PossibleCauses { get; set; }
        /// <summary>
        /// Possible mitigation for the error
        /// </summary>
        public string Guidance { get; set; }
    }
}