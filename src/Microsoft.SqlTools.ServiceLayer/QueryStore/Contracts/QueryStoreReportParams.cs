//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public abstract class QueryStoreReportParams
    {
        public string ConnectionOwnerUri;
    }

    public abstract class TypedQueryStoreReportParams<T> : QueryStoreReportParams
    {
        public abstract T Convert();
    }

    public abstract class QueryConfigurationParams<T> : TypedQueryStoreReportParams<T> where T : QueryConfigurationBase, new()
    {
        public Metric SelectedMetric;
        public Statistic SelectedStatistic;
        public int TopQueriesReturned;
        public bool ReturnAllQueries;
        public int MinNumberOfQueryPlans;

        /// <summary>
        /// Column name by which to order, if any.  Not all query generators involve ordering.
        /// </summary>
        public string OrderByColumnId;
        public bool Descending;

        public override T Convert() => new T()
        {
            SelectedMetric = SelectedMetric,
            SelectedStatistic = SelectedStatistic,
            TopQueriesReturned = TopQueriesReturned,
            ReturnAllQueries = ReturnAllQueries,
            MinNumberOfQueryPlans = MinNumberOfQueryPlans
        };
    }

    public class QueryStoreQueryResult : ResultStatus
    {
        public string Query;
    }

    public interface IOrderableQueryParams
    {
        string GetOrderByColumnId();
        bool GetDescending();
    }
}
