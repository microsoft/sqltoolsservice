//------------------------------------------------------------------------------
// <copyright file="ISqlModelUpdatingService.Constraints.cs" company="Microsoft">
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
    /// Constraint related operations
    /// </summary>
    internal partial interface ISqlModelUpdatingService
    {
        void AddCheckConstraint(SqlTable table, string constraintName);
        void DeleteCheckConstraint(SqlCheckConstraint sqlChk);
        void SetCheckConstraintExpression(SqlCheckConstraint constraint, string expression);

        void AddPrimaryKeyConstraint(SqlTable table, IEnumerable<SqlColumn> columns = null);
        void AddPrimaryKeyConstraint(SqlTable table, string constraintName, IEnumerable<SqlColumn> columns = null);
        void DeletePrimaryKeyConstraint(SqlPrimaryKeyConstraint primaryKeyConstraint);
        void SetPrimaryKey(SqlTable table, string constraintName, IEnumerable<SqlColumn> columns);
        void RemovePrimaryKey(SqlTable table);
        void SetPrimaryKeyIsClustered(SqlPrimaryKeyConstraint primaryKey, bool isClustered);
        void UpdatePrimaryKeyColumns(SqlPrimaryKeyConstraint primaryKeyConstraint, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder);

        void AddUniqueConstraint(SqlTable table, string constraintName);
        void DeleteUniqueKeyConstraint(SqlUniqueConstraint uniqueConstraint);
        void SetUniqueConstraintIsClustered(SqlUniqueConstraint uniqueConstraint, bool isClustered);
        void UpdateUniqueConstraintColumns(SqlUniqueConstraint uniqueConstraint, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder);
        
        void AddForeignKeyConstraint(SqlTable table, string constraintName, IEnumerable<SqlColumn> referencingColumns = null, SqlTableBase referencedTable = null, IEnumerable<SqlColumn> referencedColumns = null);
        void DeleteForeignKeyConstraint(SqlForeignKeyConstraint foreignKeyConstraint);
        void SetForeignKeyForeignTable(SqlForeignKeyConstraint constraint, SqlTable referencedTable);
        void InsertForeignKeyForeignColumnAt(SqlForeignKeyConstraint constraint, SqlColumn column, int index);
        void InsertForeignKeyColumnAt(SqlForeignKeyConstraint constraint, SqlColumn column, int index);
        void SetForeignKeyForeignColumn(SqlForeignKeyConstraint constraint, SqlColumn newColumn, int index);
        void SetForeignKeyColumn(SqlForeignKeyConstraint constraint, SqlColumn newColumn, int index);
        
        void AddDefaultConstraint(SqlSimpleColumn column, string expression);
        void DeleteDefaultConstraint(SqlDefaultConstraint defaultConstraint);
        void SetDefaultConstraintExpression(SqlDefaultConstraint constraint, string expression);
    }
}
