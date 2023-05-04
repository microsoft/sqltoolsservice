//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Migration.Utils
{
    internal static class MigrationSqlQueries
    {
        public static string queryDatabaseTableInfo = @"
            SELECT
                DB_NAME() as database_name,
                QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) AS table_name,
                SUM(p.Rows) AS row_count
            FROM
                sys.objects AS o
            INNER JOIN sys.partitions AS p
                ON o.object_id = p.object_id
            WHERE
                o.type = 'U'
                AND o.is_ms_shipped = 0x0
                AND index_id < 2
            GROUP BY
                o.schema_id,
                o.name
            ORDER BY table_name;";
    }
}
