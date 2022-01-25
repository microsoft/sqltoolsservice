﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor;
using Microsoft.SqlServer.DataCollection.Common.Contracts.ErrorHandling;
using Microsoft.SqlServer.DataCollection.Common.Contracts.SqlQueries;
using Microsoft.SqlServer.DataCollection.Common.ErrorHandling;
using Microsoft.SqlServer.Migration.SkuRecommendation;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.Migration
{
    /// <summary>
    /// Controller to manage the collection, aggregation, and persistence of SQL performance and static data for SKU recommendation.
    /// </summary>
    public class SqlDataQueryController : IDisposable
    {
        // Timers to control performance and static data collection intervals
        private IList<System.Timers.Timer> timers = new List<System.Timers.Timer>() { };
        private int perfQueryIntervalInSec;
        private int numberOfIterations;

        // Output folder to store data in
        private string outputFolder;

        // Name of the server handled by this controller
        private string serverName;

        // Data collector and cache
        private DataPointsCollector dataCollector = null;
        private SqlPerfDataPointsCache perfDataCache = null;

        // Whether or not this controller has been disposed
        private bool disposedValue = false;

        private ISqlAssessmentLogger _logger;

        /// <summary>
        /// Create a new SqlDataQueryController.
        /// </summary>
        /// <param name="connectionString">SQL connection string</param>
        /// <param name="outputFolder">Output folder to save results to</param>
        /// <param name="perfQueryIntervalInSec">Interval, in seconds, at which perf counters are collected</param>
        /// <param name="numberOfIterations">Number of iterations of perf counter collection before aggreagtion</param>
        /// <param name="staticQueryIntervalInSec">Interval, in seconds, at which static/common counters are colltected</param>
        /// <param name="logger">Logger</param>
        public SqlDataQueryController(
                    string connectionString,
                    string outputFolder,
                    int perfQueryIntervalInSec,
                    int numberOfIterations,
                    int staticQueryIntervalInSec,
                    ISqlAssessmentLogger logger)
        {
            this.outputFolder = outputFolder;
            this.perfQueryIntervalInSec = perfQueryIntervalInSec;
            this.numberOfIterations = numberOfIterations;
            this._logger = logger;
            perfDataCache = new SqlPerfDataPointsCache(this.outputFolder, _logger);
            dataCollector = new DataPointsCollector(new string[] { connectionString }, _logger);

            // set up timers to run perf/static collection at specified intervals
            System.Timers.Timer perfDataCollectionTimer = new System.Timers.Timer();
            perfDataCollectionTimer.Elapsed += (sender, e) => PerfDataQueryEvent();
            perfDataCollectionTimer.Interval = perfQueryIntervalInSec * 1000;
            timers.Add(perfDataCollectionTimer);

            System.Timers.Timer staticDataCollectionTimer = new System.Timers.Timer();
            staticDataCollectionTimer.Elapsed += (sender, e) => StaticDataQueryAndPersistEvent();
            staticDataCollectionTimer.Interval = staticQueryIntervalInSec * 1000;
            timers.Add(staticDataCollectionTimer);
        }

        /// <summary>
        /// Start this SqlDataQueryController.
        /// </summary>
        public void Start()
        {
            foreach (var timer in timers)
            {
                timer.Start();
            }
        }

        /// <summary>
        /// Collect performance data, adding the collected points to the cache.
        /// </summary>
        private void PerfDataQueryEvent()
        {
            try
            {
                int currentIteration = perfDataCache.CurrentIteration;

                // Get raw perf data points
                var validationResult = dataCollector.CollectPerfDataPoints(CancellationToken.None, TimeSpan.FromSeconds(this.perfQueryIntervalInSec)).Result.FirstOrDefault();

                if (validationResult != null && validationResult.Status == SqlAssessmentStatus.Completed)
                {
                    IList<ISqlPerfDataPoints> result = validationResult.SqlPerfDataPoints;
                    perfDataCache.AddingPerfData(result);
                    serverName = this.perfDataCache.ServerName;

                    // perform aggregation and persistence once enough iterations have completed
                    if (currentIteration == numberOfIterations)
                    {
                        PerfDataAggregateAndPersistEvent();
                    }
                }
                else if (validationResult != null && validationResult.Status == SqlAssessmentStatus.Error)
                {
                    var error = validationResult.Errors.FirstOrDefault();

                    Logging(error);
                }
            }
            catch (Exception e)
            {
                Logging(e);
            }
        }

        /// <summary>
        /// Aggregate and persist the cached points, saving the aggregated points to disk.
        /// </summary>
        internal void PerfDataAggregateAndPersistEvent()
        {
            try
            {
                // Aggregate the records in the Cache 
                int rawDataPointsCount = this.perfDataCache.GetRawDataPointsCount();

                this.perfDataCache.AggregatingPerfData();
                int aggregatedDataPointsCount = this.perfDataCache.GetAggregatedDataPointsCount();

                // Persist into local csv.
                if (aggregatedDataPointsCount > 0)
                {
                    this.perfDataCache.PersistingCacheAsCsv();
                }
            }
            catch (Exception e)
            {
                Logging(e);
            }
        }

        /// <summary>
        /// Collect and persist static data, saving the collected points to disk.
        /// </summary>
        private void StaticDataQueryAndPersistEvent()
        {
            try
            {
                var validationResult = this.dataCollector.CollectCommonDataPoints(CancellationToken.None).Result.FirstOrDefault();
                if (validationResult != null && validationResult.Status == SqlAssessmentStatus.Completed)
                {
                    // Common data result
                    IList<ISqlCommonDataPoints> staticDataResult = new List<ISqlCommonDataPoints>();
                    staticDataResult.Add(validationResult.SqlCommonDataPoints);
                    serverName = staticDataResult.Select(p => p.ServerName).FirstOrDefault();

                    // Save to csv
                    var persistor = new DataPointsPersistor(this.outputFolder);

                    serverName = staticDataResult.Select(p => p.ServerName).FirstOrDefault();
                    persistor.SaveCommonDataPoints(staticDataResult, serverName);
                }
                else if (validationResult != null && validationResult.Status == SqlAssessmentStatus.Error)
                {
                    var error = validationResult.Errors.FirstOrDefault();

                    Logging(error);
                }
            }
            catch (Exception e)
            {
                Logging(e);
            }
        }

        /// <summary>
        /// Log exceptions to file.
        /// </summary>
        /// <param name="ex">Exception to log</param>
        private void Logging(Exception ex)
        {
            var error = new UnhandledSqlExceptionErrorModel(ex, ErrorScope.General);
            _logger.Log(error, ErrorLevel.Error, TelemetryScope.PerfCollection);
            _logger.Log(TelemetryScope.PerfCollection, ex.Message);
        }

        /// <summary>
        /// Log errors to file.
        /// </summary>
        /// <param name="error">Error to log</param>
        private void Logging(IErrorModel error)
        {
            _logger.Log(error, ErrorLevel.Error, TelemetryScope.PerfCollection);
            _logger.Log(TelemetryScope.PerfCollection, error.RawException.Message);
        }

        /// <summary>
        /// Dispose of this SqlDataQueryController.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var timer in timers)
                    {
                        timer.Stop();
                    }

                    if (perfDataCache.CurrentIteration > 2)
                    {
                        PerfDataAggregateAndPersistEvent();   // flush cache if there are enough data points
                    }

                    this.perfDataCache = null;
                }

                disposedValue = true;
            }
        }
    }

    /// <summary>
    /// Cache to store intermediate SQL performance data before it is aggregated and persisted for SKU recommendation.
    /// </summary>
    public class SqlPerfDataPointsCache
    {
        public string ServerName { get; private set; }

        public int CurrentIteration { get; private set; }

        private string outputFolder;
        private ISqlAssessmentLogger logger;

        private IList<IList<ISqlPerfDataPoints>> perfDataPoints = new List<IList<ISqlPerfDataPoints>>();
        private IList<AggregatedPerformanceCounters> perfAggregated = new List<AggregatedPerformanceCounters>();

        /// <summary>
        /// Create a new SqlPerfDataPointsCache.
        /// </summary>
        /// <param name="outputFolder">Output folder to save results to</param>
        /// <param name="logger">Logger</param>
        public SqlPerfDataPointsCache(string outputFolder, ISqlAssessmentLogger logger = null)
        {
            this.outputFolder = outputFolder;
            this.logger = logger ?? new DefaultPerfDataCollectionLogger();
            CurrentIteration = 1;
        }

        /// <summary>
        /// Add the collected data points to the cache.
        /// </summary>
        /// <param name="result">Collected data points</param>
        public void AddingPerfData(IList<ISqlPerfDataPoints> result)
        {
            ServerName = result.Select(p => p.ServerName).FirstOrDefault();
            perfDataPoints.Add(result);
            CurrentIteration++;
        }

        /// <summary>
        /// Return the number of raw data points.
        /// </summary>
        public int GetRawDataPointsCount()
        {
            // flatten list
            return perfDataPoints.SelectMany(x => x).Count();
        }

        /// <summary>
        /// Return the number of aggregated data points.
        /// </summary>
        public int GetAggregatedDataPointsCount()
        {
            return perfAggregated.Count;
        }

        /// <summary>
        /// Aggregate the cached data points.
        /// </summary>
        public void AggregatingPerfData()
        {
            try
            {
                var aggregator = new CounterAggregator(logger);
                perfAggregated = aggregator.AggregateDatapoints(perfDataPoints);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                perfDataPoints.Clear();
                // reset the iteration counter
                CurrentIteration = 1;
            }
        }

        /// <summary>
        /// Save the cached and aggregated data points to disk.
        /// </summary>
        public void PersistingCacheAsCsv()
        {
            // Save to csv
            var persistor = new DataPointsPersistor(outputFolder);
            persistor.SavePerfDataPoints(perfAggregated, machineId: ServerName, overwrite: false);
        }
    }
}
