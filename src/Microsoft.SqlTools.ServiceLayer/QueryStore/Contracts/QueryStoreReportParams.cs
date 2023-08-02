//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class QueryStoreReportParams
    {
        public string ConnectionOwnerUri;
        public string OrderByColumnId;
        public bool Descending;
    }

    public class QueryStoreQueryResult : ResultStatus
    {
        public string Query;
    }
}
