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
        public string ConnectionOwnerUri { get; set; }
    }

    public abstract class TypedQueryStoreReportParams<T> : QueryStoreReportParams
    {
        public abstract T Convert();
    }

    public abstract class QueryConfigurationParams<T> : TypedQueryStoreReportParams<T> where T : QueryConfigurationBase, new()
    {
        public Metric SelectedMetric { get; set; }
        public Statistic SelectedStatistic { get; set; }
        public int TopQueriesReturned { get; set; }
        public bool ReturnAllQueries { get; set; }
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

    public class QueryStoreQueryResult : ResultStatus
    {
        public string Query { get; set; }
    }

    public interface IOrderableQueryParams
    {
        string GetOrderByColumnId();
    }
}
