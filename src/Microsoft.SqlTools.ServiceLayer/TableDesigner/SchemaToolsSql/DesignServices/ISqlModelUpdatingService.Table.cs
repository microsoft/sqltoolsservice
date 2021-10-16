//------------------------------------------------------------------------------
// <copyright file="ISqlModelUpdatingService.Table.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// Table related operations
    /// </summary>
    internal partial interface ISqlModelUpdatingService
    {
        void AddTable(string schemaName, string tableName);
        void DeleteTable(SqlTable table);
        void MergeTable(IEnumerable<SqlColumn> sourceColumns, SqlTable targetTable);
    }
}
