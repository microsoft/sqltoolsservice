//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    /// <summary>
    /// Class contains logic to map runtime/wait metrics names that are recorded by QDS to equivalent enumerations.
    /// This class reads the available metric once for each unique database context and stores it under ContextToMetaDataMapping to reuse later.
    /// </summary>
    public static class QdsMetadataMapper
    {
        #region Private fields

        /// <summary>
        /// This stores the available metric list for each unique database context
        /// </summary>
        private readonly static Dictionary<string, IList<Metric>> ContextToMetaDataMapping = new Dictionary<string, IList<Metric>>();

        private static readonly object Locker = new object();

        #endregion

        /// <summary>
        /// This method fetches the list of available metrics for the database context passed in. 
        /// If the list is already in the cache the same list is returned else the list is fetched 
        /// from database and stored under the cache.
        /// </summary>
        /// <returns></returns>
        public static IList<Metric> GetAvailableMetrics(SqlConnection sqlConnection)
        {
            IList<Metric> availableMetrics;
            lock (Locker)
            {
                if (ContextToMetaDataMapping.TryGetValue(GetCacheKey(sqlConnection), out availableMetrics))
                {
                    return availableMetrics;
                }

                availableMetrics = MapDbNamesToAvailableMetrics(GetAvailableMetricNamesFromDb(sqlConnection));
                ContextToMetaDataMapping.Add(GetCacheKey(sqlConnection), availableMetrics);
            }

            return availableMetrics;
        }

        /// <summary>
        /// This method fetches the list of available replicas for the database context passed in. 
        /// A SQL connection is opened and the replica name and id are queried and added to a 
        /// list of ReplicaGroupItem objects.
        /// </summary>
        /// <returns></returns>
        public static List<ReplicaGroupItem> GetAvailableReplicas(SqlConnection sqlConnection)
        {
            Debug.Assert(sqlConnection != null, "GetAvailableReplicas failed with: sqlConnection is null");

            var replicas = new List<ReplicaGroupItem>();

            try
            {
                // Open the connection if it is not already open
                //
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }

                using (var sqlCommand = new SqlCommand())
                {
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandType = CommandType.Text;

                    // These queries ensure that only replicas with data in the runtime stats table are included in the results.
                    //
                    sqlCommand.CommandText = @"
                        SELECT COUNT(*) AS ReplicaCount FROM sys.query_store_replicas;
                        SELECT r.replica_name, r.replica_group_id  
                        FROM sys.query_store_replicas r
                        WHERE EXISTS (
                            SELECT 1
                            FROM sys.query_store_runtime_stats s
                            WHERE s.replica_group_id = r.replica_group_id);";

                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        // Fetch the result of the first query (replica count)
                        //
                        while (reader.Read())
                        {
                            var replicaCount = reader.GetInt32(0);
                            if (replicaCount == 0)
                            {
                                // Case 1: Replica table is empty, show all four options
                                //
                                replicas.Add(new ReplicaGroupItem(ReplicaGroup.Primary.ToLong(), nameof(ReplicaGroup.Primary)));
                                replicas.Add(new ReplicaGroupItem(ReplicaGroup.Secondary.ToLong(), nameof(ReplicaGroup.Secondary)));
                                replicas.Add(new ReplicaGroupItem(ReplicaGroup.GeoSecondary.ToLong(), nameof(ReplicaGroup.GeoSecondary)));
                                replicas.Add(new ReplicaGroupItem(ReplicaGroup.GeoHASecondary.ToLong(), nameof(ReplicaGroup.GeoHASecondary)));
                                break;
                            }
                        }
                        if (reader.NextResult())
                        {
                            // Case 2: Replica table is not empty, show the replicas that have data in the runtime stats table
                            //
                            while (reader.Read())
                            {
                                var replicaName = reader.GetString(0);
                                var replicaGroupId = reader.GetInt64(1);
                                replicas.Add(new ReplicaGroupItem(replicaGroupId, replicaName));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Swallow the (unlikely) exception
                //
                Debug.WriteLine($"Error in GetAvailableReplicas: {ex.Message}");
            }

            // If for some reason there is no primary replica in the list, add it
            //
            var primaryId = ReplicaGroup.Primary.ToLong();
            if (!replicas.Any(replica => replica.ReplicaGroupId == primaryId))
            {
                replicas.Insert(0, new ReplicaGroupItem(ReplicaGroup.Primary.ToLong(), nameof(ReplicaGroup.Primary)));
            }

            return replicas;
        }

        #region Helper Method

        private static string GetCacheKey(SqlConnection sqlConnection) => $"{sqlConnection.DataSource}/{sqlConnection.Database}";

        /// <summary>
        /// With iterative changes going in SQL Server Engine, definition of sys.query_store_runtime_stats 
        /// and sys.query_store_wait_stats may change over time. For Ex - adding new columns in sys.query_store_runtime_stats, 
        /// Adding new DMV like sys.query_store_wait_stats which is added with SQL2017.
        /// This method fetches available metrics for current database engine client is connected to.
        /// Metric Names are the combination of column names from sys.query_store_runtime_stats and 
        /// wait_stats_id column from sys.query_store_wait_stats.
        /// These column names are later mapped to Metrics supported under QueryStoreUI so that UI can 
        /// dynamically adapt to database engine running.
        /// </summary>
        /// <returns></returns>
        public static IList<string> GetAvailableMetricNamesFromDb(SqlConnection sqlConnection)
        {
            IList<string> metrics;

            sqlConnection.Open();

            // read column list from sys.query_store_runtime_stats
            using (var sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.CommandText = "select top(1) * from sys.query_store_runtime_stats;" +
                    "IF OBJECT_ID ('sys.query_store_wait_stats') IS NOT NULL SELECT CAST(1 as bit) AS result ELSE SELECT CAST(0 as bit) AS result;";

                using (var dataReader = sqlCommand.ExecuteReader())
                {
                    metrics = Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToList();
                    while (dataReader.NextResult() && dataReader.Read())
                    {
                        if (!dataReader.GetBoolean(0))
                        {
                            continue;
                        }
                        metrics.Add("wait_stats_id");
                    }
                }

            }
            return metrics;
        }

        /// <summary>
        /// Helper method that maps the raw metric names from database table columns to enumeration
        /// </summary>
        /// <param name="metricRawNamesList"></param>
        /// <returns></returns>
        public static IList<Metric> MapDbNamesToAvailableMetrics(IList<string> metricRawNamesList)
        {
            if (metricRawNamesList == null || metricRawNamesList.Count == 0)
            {
                throw new InvalidOperationException(SR.MetricsNotAvailableInTargetDb);
            }

            var dbNamesToServerSupportedMetricMapping = MetricUtils.DbNamesToServerSupportedMetricMapping();

            var availableMetrics = new HashSet<Metric>();

            foreach (var metricName in metricRawNamesList)
            {
                if (dbNamesToServerSupportedMetricMapping.ContainsKey(metricName))
                {
                    availableMetrics.Add(dbNamesToServerSupportedMetricMapping[metricName]);
                }
            }

            return availableMetrics.ToList();
        }

        #endregion
    }
}
