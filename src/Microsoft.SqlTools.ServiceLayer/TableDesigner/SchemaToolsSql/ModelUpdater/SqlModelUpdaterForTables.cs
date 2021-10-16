//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdaterForTables.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed partial class SqlModelUpdater
    {

        #region Public Methods that operate on SqlTable

        public IList<SqlScriptUpdateInfo> AddInlineIndex(SqlTable table, string indexName)
        {
            return AddInlineAttribute(table, indexName, _scriptUpdater.AddInlineIndex);
        }

        public IList<SqlScriptUpdateInfo> AddCheckConstraint(SqlTable table, string constraintName)
        {
            return AddInlineAttribute(table, constraintName, _scriptUpdater.AddCheckConstraint);
        }

        public IList<SqlScriptUpdateInfo> AddInlineAttribute(SqlTable table, 
            string name,
            Func<SqlTable,string, IList<SqlScriptUpdateInfo>> scriptUpdateInfoGenerator)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, name, "name");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                updates = scriptUpdateInfoGenerator(table, name);
                NotifyModelUpdate(updates);

                // we inserted it after the last table item (column or table constraint),
                // and we don't want the length of the last item to be affected by this change
                CreateTableStatement tableAst = SqlModelUpdaterUtils.GetPrimaryAst<CreateTableStatement>(table);
                TSqlFragment lastTableItem = GetLastTableItem(tableAst); // get last column or last table constraint
                Tuple<int, int> excludingRange = new Tuple<int, int>(lastTableItem.StartOffset, lastTableItem.StartOffset + lastTableItem.FragmentLength);

                // We don't want to update ScriptCache now as it will be updated later, otherwise the model will not be updated 
                // with the script change as well as IntelliSense error check will be skipped.
                // Instead of manually create a constraint/index with relationships in schema model and bind statement position here, 
                // we get model updated based on script change and context view window will pick up the changes later.
                UpdatePositions(updates, excludingRange);
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }
       
        public IList<SqlScriptUpdateInfo> AddPrimaryKeyConstraint(SqlTable table, string constraintName)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, constraintName, "constraintName");

            return NotifyModelUpdate(_scriptUpdater.AddPrimaryKeyConstraint(table, constraintName));
        }

        public IList<SqlScriptUpdateInfo> AddPrimaryKeyConstraint(SqlTable table, IEnumerable<SqlColumn> columns)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            SqlExceptionUtils.ValidateNullParameter(columns, "columns");

            return NotifyModelUpdate(_scriptUpdater.AddPrimaryKeyConstraint(table, columns));
        }

        public IList<SqlScriptUpdateInfo> AddPrimaryKeyConstraint(SqlTable table, string constraintName, IEnumerable<SqlColumn> columns)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            SqlExceptionUtils.ValidateNullParameter(columns, "columns");
            ValidateIdentifier(table, constraintName, "constraintName");

            return NotifyModelUpdate(_scriptUpdater.AddPrimaryKeyConstraint(table, constraintName, columns));
        }

        public IList<SqlScriptUpdateInfo> UpdatePrimaryKeyColumns(SqlPrimaryKeyConstraint primaryKeyConstraint, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            SqlExceptionUtils.ValidateNullParameter(primaryKeyConstraint, "primaryKeyConstraint");
            SqlExceptionUtils.ValidateNullParameter(columns, "columns");
            SqlExceptionUtils.ValidateNullParameter(sortOrder, "sortOrder");

            if (!(primaryKeyConstraint.DefiningTable is SqlTable))
            {
                throw new ArgumentException("primaryKeyConstraint.DefiningTable should be SqlTable", "primaryKeyConstraint");
            }

            if (!columns.GetEnumerator().MoveNext())
            {
                SqlModelUpdaterUtils.TraceAndThrow("primaryKeyConstraint should have at least 1 referencing column");
            }

            return NotifyModelUpdate(SqlScriptUpdater.UpdatePrimaryKeyColumns(primaryKeyConstraint, columns, sortOrder));
        }

        public IList<SqlScriptUpdateInfo> AddUniqueConstraint(SqlTable table, string constraintName)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, constraintName, "constraintName");

            return NotifyModelUpdate(_scriptUpdater.AddUniqueConstraint(table, constraintName));
        }

        public IList<SqlScriptUpdateInfo> UpdateUniqueConstraintColumns(SqlUniqueConstraint uniqueConstraint, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            SqlExceptionUtils.ValidateNullParameter(uniqueConstraint, "uniqueConstraint");
            SqlExceptionUtils.ValidateNullParameter(columns, "columns");
            SqlExceptionUtils.ValidateNullParameter(sortOrder, "sortOrder");

            if (!(uniqueConstraint.DefiningTable is SqlTable))
            {
                throw new ArgumentException("uniqueConstraint.DefiningTable should be SqlTable", "uniqueConstraint");
            }

            if (!columns.GetEnumerator().MoveNext())
            {
                SqlModelUpdaterUtils.TraceAndThrow("uniqueConstraint should have at least 1 referencing column");
            }

            return NotifyModelUpdate(SqlScriptUpdater.UpdateUniqueConstraintColumns(uniqueConstraint, columns, sortOrder));
        }

        public IList<SqlScriptUpdateInfo> AddForeignKeyConstraint(SqlTable table, string constraintName)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, constraintName, "constraintName");

            return NotifyModelUpdate(_scriptUpdater.AddForeignKeyConstraint(table, constraintName));
        }

        public IList<SqlScriptUpdateInfo> AddForeignKeyConstraint(SqlTable table, string constraintName, IEnumerable<SqlColumn> referencingColumns, SqlTableBase referencedTable, IEnumerable<SqlColumn> referencedColumns)  
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            SqlExceptionUtils.ValidateNullParameter(referencingColumns, "referencingColumns");
            SqlExceptionUtils.ValidateNullParameter(referencedTable, "referencedTable");
            SqlExceptionUtils.ValidateNullParameter(referencedColumns, "referencedColumns");
            ValidateIdentifier(table, constraintName, "constraintName");

            IList<SqlScriptUpdateInfo> updates = _scriptUpdater.AddForeignKeyConstraint(table, constraintName, referencingColumns, referencedTable, referencedColumns);
            NotifyModelUpdate(updates);
            return updates;
        }

        public IList<SqlScriptUpdateInfo> AddDefaultConstraint(SqlSimpleColumn column, string expression)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");
            SqlExceptionUtils.ValidateNullParameter(expression, "expression");

            if (column.Defaults.Count > 0)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't have more than one Default constraint defined for the column");
            }

            TSqlParser parser = _model.GetParser(column);
            ValidateScalarExpression(parser, expression);

            return NotifyModelUpdate(_scriptUpdater.AddDefaultConstraint(column, expression));
        }
        
        /// <summary>
        /// Inserts a column set defition at the specified position in the specified table.
        /// </summary>
        /// <param name="table">The table where the column should be inserted. Cannot be null.</param>
        /// <param name="columnName">The unescaped name of the column to be inserted.</param>
        /// <param name="index">The position where the column should be inserted.</param>
        /// <returns>
        /// List of script updates that represent the insertion of the column.
        /// </returns>
        public IList<SqlScriptUpdateInfo> InsertColumnSetAt(SqlTable table, string columnName, int index)
        {
            // Validate parameters
            SqlExceptionUtils.ValidateNullParameter(table, "table");

            // Validate column name
            ValidateIdentifier(table, columnName, "columnName");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                // Validate column index
                ValidateColumnIndex(table.Columns.Count, index);

                updates = _scriptUpdater.InsertColumnSetAt(table, columnName, index);
                NotifyModelUpdate(updates);
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        /// <summary>
        /// Inserts a computed column defition at the specified position in the specified table.
        /// </summary>
        /// <param name="table">The table where the column should be inserted. Cannot be null.</param>
        /// <param name="columnName">The unescaped name of the column to be inserted.</param>
        /// <param name="index">The position where the column should be inserted.</param>
        /// <param name="expression">The expression that represents the computed column definition.</param>
        /// <returns>
        /// List of script updates that represent the insertion of the column.
        /// </returns>
        public IList<SqlScriptUpdateInfo> InsertComputedColumnAt(SqlTable table, string columnName, int index, string expression)
        {
            // Validate parameters
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            SqlExceptionUtils.ValidateNullOrEmptyParameter(expression, "expression");

            TSqlParser parser = _model.GetParser(table);

            ValidateScalarExpression(parser, expression);

            // Validate column name
            ValidateIdentifier(parser, columnName, "columnName");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                // Validate column index
                ValidateColumnIndex(table.Columns.Count, index);

                updates = _scriptUpdater.InsertComputedColumnAt(table, columnName, index, expression);
                NotifyModelUpdate(updates);
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }


        /// <summary>
        /// Inserts a computed column defition at the specified position in the specified table. The column will have a type of INT
        /// </summary>
        /// <param name="table">The table where the column should be inserted. Cannot be null.</param>
        /// <param name="columnName">The unescaped name of the column to be inserted.</param>
        /// <param name="index">The position where the column should be inserted.</param>
        /// <returns>
        /// List of script updates that represent the insertion of the column.
        /// </returns>
        public IList<SqlScriptUpdateInfo> InsertSimpleColumnAt(SqlTable table, string columnName, int index)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");

            // Validate column name
            ValidateIdentifier(table, columnName, "columnName");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                TableDefinition tableDef = SqlModelUpdaterUtils.GetPrimaryAst<CreateTableStatement>(table).Definition;

                // Validate column index
                ValidateColumnIndex(table.Columns.Count, index);

                updates = _scriptUpdater.InsertSimpleColumnAt(table, columnName, index);
                NotifyModelUpdate(updates);

                TSqlFragment[] collatedDefinitions = SqlScriptUpdater.GetCollatedDefinitionsForTable(tableDef);
                int collatedIndex = SqlScriptUpdater.GetCollatedIndexForDefinition<ColumnDefinition>(index, collatedDefinitions);

                Tuple<int, int> excludingRange;
                if (collatedDefinitions.Length == 0)
                {
                    // no existing columns
                    excludingRange = null;
                }
                else if (collatedIndex < collatedDefinitions.Length)
                {
                    // avoid changing length for the column/constraint before which the new column was inserted
                    excludingRange = CreateExcludingRange(collatedDefinitions[collatedIndex]);
                }
                else
                {
                    // avoid changing length for the last column/constraint after which the new column was inserted
                    excludingRange = CreateExcludingRange(collatedDefinitions[collatedIndex - 1]);
                }

                UpdatePositionsAndScriptCache(updates, excludingRange);

                string filename = table.PrimarySource.SourceName;

                ColumnDefinition columnAst = GetColumnAst(table, index);

                SourceCodePosition columnPosition = new SourceCodePosition(
                    isPrimary: true,
                    startOffset: columnAst.StartOffset,
                    length: columnAst.FragmentLength,
                    startLine: columnAst.StartLine,
                    startColumn: columnAst.StartColumn,
                    sourceName: filename);

                SourceCodePosition dataTypePosition = new SourceCodePosition(
                    isPrimary: true,
                    startOffset: columnAst.DataType.StartOffset,
                    length: columnAst.DataType.FragmentLength,
                    startLine: columnAst.DataType.StartLine,
                    startColumn: columnAst.DataType.StartColumn,
                    sourceName: filename);

                // let's create a simple column
                List<string> columnNameParts = new List<string>(table.Name.Parts);
                columnNameParts.Add(columnName);
                ModelIdentifier columnId = _model.CreateIdentifier(columnNameParts);
                SqlSimpleColumn column = _model.CreateNamedElement<SqlSimpleColumn>(columnId);
                column.IsNullable = true;
                column.SourceCodePositions.Add(columnPosition);

                // insert the column to the table: table -> column
                
                IModelMultiRelationship<ISqlColumnSource, SqlColumn> tableToColumnsRelationship = table.GetColumnsRelationship();
                IModelRelationshipEntry entry = tableToColumnsRelationship.InsertElement(index, column);
                entry.SourceCodePositions.Add(columnPosition);

                // create a type specifier for the column: column -> type specifier
                SqlTypeSpecifier typeSpec = _model.CreateElement<SqlTypeSpecifier>();
                typeSpec.Length = SqlModelUpdaterConstants.DefaultNCharLength;
                typeSpec.SourceCodePositions.Add(dataTypePosition);
                IModelSingleRelationship<SqlColumn, SqlTypeSpecifierBase> columnToTypeSpecRelationship = column.GetTypeSpecifierRelationship();
                entry = columnToTypeSpecRelationship.RecreateRelationshipEntry(typeSpec);
                entry.SourceCodePositions.Add(dataTypePosition);

                // create an entry for type specifier to type: type specifier -> type
                IModelSingleRelationship<SqlTypeSpecifierBase, SqlType> typeSpecToType = typeSpec.GetTypeRelationship();
                entry = typeSpecToType.RecreateRelationshipEntry();

                // attach a resolvable annotation for data type (int)
                ModelIdentifier annoId = _model.CreateIdentifier(SqlModelUpdaterUtils.GetBuiltInTypeName(SqlDataType.NChar));
                SqlModelBuilderResolvableAnnotation anno = _model.CreateNamedAnnotation<SqlModelBuilderResolvableAnnotation>(annoId);
                anno.TargetType = typeof(SqlType);
                entry.AddAnnotation(anno);
                entry.SourceCodePositions.Add(dataTypePosition);

                typeSpec.ResolutionStatus = ModelElementResolutionStatus.ResolveCandidate;
                typeSpec.ValidationStatus = ModelElementValidationStatus.Initial;

                // the inserted column could affect its containing table's validation (example: row size)
                ResetValidationStatus(table);

                // handle the sitatuions where we have columns with the same name
                SqlModelBuilderSchemaAnalyzer.HandleDuplicates(column, column.Name);

                // reset wildcards that depend on the table
                SqlModelResolver.ResetWildcardOnElement(table, table.Name);
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        #endregion
    }
}
