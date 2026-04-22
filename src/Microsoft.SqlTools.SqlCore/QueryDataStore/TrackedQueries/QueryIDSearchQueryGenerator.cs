//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Globalization;

namespace Microsoft.SqlServer.Management.QueryStoreModel.TrackedQueries
{
    public static class QueryIDSearchQueryGenerator
    {
        public const string QuerySearchTextParameter = "@QuerySearchText";

        public static string GetQuery()
        {
            // TODO: VSTS 4442842 remove the 500 results limit
            return string.Format(
                CultureInfo.InvariantCulture,
@"SELECT TOP 500 q.query_id, q.query_text_id, qt.query_sql_text 
FROM sys.query_store_query_text qt JOIN sys.query_store_query q ON q.query_text_id = qt.query_text_id 
WHERE qt.query_sql_text LIKE ('%' + {0} + '%')",
                QuerySearchTextParameter);
        }
    }
}
