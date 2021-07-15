using System.Collections.Generic;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.TargetAssessment.Models;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class ServerProperties
    {
        /// <summary>
        /// Cpu cores for the server host
        /// </summary>
        public long CpuCoreCount { get; set; }
        /// <summary>
        /// Host physical memory size
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
        /// Count of databases ready for migration
        /// </summary>
        public IServerTargetReadiness SQLManagedInstanceTargetReadinesses { get; set; }
        /// <summary>
        /// Server assessment results
        /// </summary>
        public List<MigrationAssessmentInfo> Items { get; set; }
        /// <summary>
        /// Server assessment errors
        /// </summary>
        public List<ErrorModel> Errors { get; set; }
        /// <summary>
        /// List of databases that are assessed
        /// </summary>
        public List<DatabaseProperties> Databases { get; set; }
    }

    public class DatabaseProperties
    {
        /// <summary>
        /// Compatibility level of the database
        /// </summary>
        public string CompatibilityLevel { get; set; }
        /// <summary>
        /// Size of the database
        /// </summary>
        public double DatabaseSize { get; set; }
        /// <summary>
        /// Flag that indicated if the database is replicated
        /// </summary>
        public bool IsReplicationEnabled { get; set; }
        /// <summary>
        /// Time taken for assessing the database
        /// </summary>
        public double AssessmentTimeInMilliseconds { get; set; }
        /// <summary>
        /// Assessment result for database
        /// </summary>
        public List<MigrationAssessmentInfo> Items { get; set; }
        /// <summary>
        /// Database assessment errors
        /// </summary>
        public List<ErrorModel> Errors {get; set;}
        public IDatabaseTargetReadiness SQLManagedInstanceTargetReadiness { get; set; }
    }

    public class ErrorModel
    {
        public string ErrorId { get; set; }
        public string Message { get; set; }
        public string ErrorSummary { get; set; }
        public string PossibleCauses { get; set; }
        public string Guidance { get; set; }
    }
}