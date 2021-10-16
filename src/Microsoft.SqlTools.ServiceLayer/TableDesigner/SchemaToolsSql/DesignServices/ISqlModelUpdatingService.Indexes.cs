//------------------------------------------------------------------------------
// <copyright file="ISqlModelUpdatingService.Indexes.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// Index related operations
    /// </summary>
    internal partial interface ISqlModelUpdatingService
    {
        void CreateIndex(SqlTable table, string name);
        void DeleteIndex(SqlIndex sqlIndex);
        void SetIndexIsClustered(SqlIndex sqlIndex, bool isClustered);
        void SetIndexIsUnique(SqlIndex sqlIndex, bool isUnique);

        void CreateXmlIndex(SqlTable table, string name, bool isPrimary);
        void DeleteXmlIndex(SqlXmlIndex sqlXmlIndex);

        void CreateSelectiveXmlIndex(SqlTable table, string name, bool isPrimary);
        void DeleteSelectiveXmlIndex(SqlSelectiveXmlIndex sqlSelectiveXmlIndex);

        void CreateSpatialIndex(SqlTable table, string name);
        void DeleteSpatialIndex(SqlSpatialIndex sqlSpatial);

        void CreateFullTextIndex(SqlTable table);
        void DeleteFullTextIndex(SqlFullTextIndex sqlFullTextIndex);

        void CreateColumnStoreIndex(SqlTable table, string name);
        void DeleteColumnStoreIndex(SqlColumnStoreIndex columnStoreIndex);

        void UpdateIndexColumns(SqlIndex sqlIndex, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder);

        void UpdateColumnStoreIndexColumns(SqlColumnStoreIndex columnStoreIndex, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder);
    }
}
