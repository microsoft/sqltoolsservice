//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    /// <summary>
    /// Base class for a Query Store report parameters
    /// </summary>
    public abstract class QueryStoreReportParams
    {
        /// <summary>
        /// Connection URI for the database
        /// </summary>
        public string ConnectionOwnerUri { get; set; }
    }

    /// <summary>
    /// Base class for Query Store report parameters that can be converted to a configuration object for use in QSM query generators
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TypedQueryStoreReportParams<T> : QueryStoreReportParams
    {
        /// <summary>
        /// Converts this SQL Tools Service parameter object to the QSM configuration object
        /// </summary>
        /// <returns></returns>
        public abstract T Convert();
    }

    /// <summary>
    /// Base class for parameters for a report type that uses QueryConfigurationBase for its configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class QueryConfigurationParams<T> : TypedQueryStoreReportParams<T> where T : QueryConfigurationBase, new()
    {
        /// <summary>
        /// Metric to summarize
        /// </summary>
        public Metric SelectedMetric { get; set; }

        /// <summary>
        /// Statistic to calculate on SelecticMetric
        /// </summary>
        public Statistic SelectedStatistic { get; set; }

        /// <summary>
        /// Number of queries to return if ReturnAllQueries is not set
        /// </summary>
        public int TopQueriesReturned { get; set; }

        /// <summary>
        /// True to include all queries in the report; false to only include the top queries, up to the value specified by TopQueriesReturned
        /// </summary>
        public bool ReturnAllQueries { get; set; }

        /// <summary>
        /// Minimum number of query plans for a query to included in the report
        /// </summary>
        public int MinNumberOfQueryPlans { get; set; }

        public override T Convert() => new T()
        {
            SelectedMetric = SelectedMetric,
            SelectedStatistic = SelectedStatistic,
            TopQueriesReturned = TopQueriesReturned,
            ReturnAllQueries = ReturnAllQueries,
            MinNumberOfQueryPlans = MinNumberOfQueryPlans
        };
    }

    /// <summary>
    /// Base class for parameters for a report that can be ordered by a specified column
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class OrderableQueryConfigurationParams<T> : QueryConfigurationParams<T>, IOrderableQueryParams where T : QueryConfigurationBase, new()
    {
        /// <summary>
        /// Name of the column to order results by
        /// </summary>
        public string OrderByColumnId { get; set; }

        /// <summary>
        /// Direction of the result ordering
        /// </summary>
        public bool Descending { get; set; }

        /// <summary>
        /// Gets the name of the column to order the report results by
        /// </summary>
        /// <returns></returns>
        public string GetOrderByColumnId() => OrderByColumnId;
    }

    /// <summary>
    /// Result containing a finalized query for a report
    /// </summary>
    public class QueryStoreQueryResult : ResultStatus
    {
        /// <summary>
        /// Finalized query for a report
        /// </summary>
        public string Query { get; set; }
    }

    /// <summary>
    /// Interface for parameters for a report that can be ordered by a specific column
    /// </summary>
    public interface IOrderableQueryParams
    {
        /// <summary>
        /// Gets the name of the column to order the report results by
        /// </summary>
        /// <returns></returns>
        string GetOrderByColumnId();
    }
}
