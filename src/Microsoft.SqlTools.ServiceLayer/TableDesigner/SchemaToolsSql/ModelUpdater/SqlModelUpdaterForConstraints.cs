//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdaterForConstraints.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed partial class SqlModelUpdater
    {
        /// <summary>
        /// Deletes a Primary Key Constraint
        /// </summary>
        /// <param name="constraint">The SqlPrimaryKeyConstraint to delete</param>
        /// <returns></returns>
        public IList<SqlScriptUpdateInfo> DeletePrimaryKeyConstraint(SqlPrimaryKeyConstraint constraint)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");

            return NotifyModelUpdate(_scriptUpdater.DeleteConstraint(constraint));
        }

        #region Public Methods that Operate on SqlForeignKeyConstraint

        public IList<SqlScriptUpdateInfo> SetForeignKeyForeignTable(SqlForeignKeyConstraint constraint, SqlTable referencedTable)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");
            SqlExceptionUtils.ValidateNullParameter(referencedTable, "referencedTable");

            return NotifyModelUpdate(_scriptUpdater.SetForeignKeyForeignTable(constraint, referencedTable));
        }

        public IList<SqlScriptUpdateInfo> InsertForeignKeyForeignColumnAt(SqlForeignKeyConstraint constraint, SqlColumn column, int index)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            ValidateColumnIndex(constraint.ForeignColumns.Count, index);

            return NotifyModelUpdate(_scriptUpdater.InsertForeignKeyForeignColumnAt(constraint, column, index));
        }

        public IList<SqlScriptUpdateInfo> InsertForeignKeyColumnAt(SqlForeignKeyConstraint constraint, SqlColumn column, int index)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            ValidateColumnIndex(constraint.Columns.Count, index);

            return NotifyModelUpdate(_scriptUpdater.InsertForeignKeyColumnAt(constraint, column, index));
        }

        public IList<SqlScriptUpdateInfo> SetForeignKeyForeignColumn(SqlForeignKeyConstraint constraint, SqlColumn newColumn, int index)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");
            SqlExceptionUtils.ValidateNullParameter(newColumn, "newColumn");

            ValidateColumnIndex(constraint.ForeignColumns.Count - 1, index);
            if (constraint.ForeignColumns[index] == newColumn)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Cannot set old column to the same column");
                throw new ArgumentOutOfRangeException("newColumn", "Cannot set old column to the same column");
            }

            return NotifyModelUpdate(_scriptUpdater.SetForeignKeyForeignColumn(constraint, newColumn, index));
        }

        public IList<SqlScriptUpdateInfo> SetForeignKeyColumn(SqlForeignKeyConstraint constraint, SqlColumn newColumn, int index)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");
            SqlExceptionUtils.ValidateNullParameter(newColumn, "newColumn");

            ValidateColumnIndex(constraint.Columns.Count - 1, index);
            if (constraint.Columns[index] == newColumn)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Cannot set old column to the same column");
                throw new ArgumentException("Cannot set old column to the same column");
            }

            return NotifyModelUpdate(_scriptUpdater.SetForeignKeyColumn(constraint, newColumn, index));
        }

        /// <summary>
        /// Deletes a Foreign Key Constraint
        /// </summary>
        /// <param name="constraint">The SqlForeignKeyConstraint to delete</param>
        /// <returns></returns>
        public IList<SqlScriptUpdateInfo> DeleteForeignKeyConstraint(SqlForeignKeyConstraint constraint)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");

            return NotifyModelUpdate(_scriptUpdater.DeleteConstraint(constraint));
        }

        #endregion

        #region Public Methods that Operate on SqlDefaultConstraint
        /// <summary>
        /// Deletes a Default Constraint
        /// </summary>
        /// <param name="constraint">The SqlDefaultConstraint to delete</param>
        /// <returns></returns>
        public IList<SqlScriptUpdateInfo> DeleteDefaultConstraint(SqlDefaultConstraint constraint)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");

            return NotifyModelUpdate(_scriptUpdater.DeleteConstraint(constraint));
        }

        public IList<SqlScriptUpdateInfo> SetDefaultConstraintExpression(SqlDefaultConstraint constraint, string expression)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");
            SqlExceptionUtils.ValidateNullOrEmptyParameter(expression, "expression");

            TSqlParser parser = _model.GetParser(constraint);
            SqlModelUpdater.ValidateScalarExpression(parser, expression);

            return NotifyModelUpdate(_scriptUpdater.SetDefaultConstraintExpression(constraint, expression));
        }
        #endregion

        /// <summary>
        /// Deletes a Check Constraint
        /// </summary>
        /// <param name="constraint">The SqlCheckConstraint to delete</param>
        /// <returns></returns>
        public IList<SqlScriptUpdateInfo> DeleteCheckConstraint(SqlCheckConstraint constraint)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");

            return NotifyModelUpdate(_scriptUpdater.DeleteConstraint(constraint));
        }

        /// <summary>
        /// Deletes a Unique Constraint
        /// </summary>
        /// <param name="constraint">The SqlUniqueConstraint to delete</param>
        /// <returns></returns>
        public IList<SqlScriptUpdateInfo> DeleteUniqueConstraint(SqlUniqueConstraint constraint)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");

            return NotifyModelUpdate(_scriptUpdater.DeleteConstraint(constraint));
        }

        public IList<SqlScriptUpdateInfo> SetCheckConstraintExpression(SqlCheckConstraint constraint, string expression)
        {
            SqlExceptionUtils.ValidateNullParameter(constraint, "constraint");
            SqlExceptionUtils.ValidateNullParameter(expression, "expression");

            ValidateBooleanExpression(_model.GetParser(constraint), expression);

            return NotifyModelUpdate(SqlScriptUpdater.SetCheckConstraintExpression(constraint, expression));
        }

        public IList<SqlScriptUpdateInfo> SetPrimaryKeyConstraintIsClustered(SqlPrimaryKeyConstraint primaryKeyConstraint, bool isClustered)
        {
            SqlExceptionUtils.ValidateNullParameter(primaryKeyConstraint, "primaryKeyConstraint");

            return NotifyModelUpdate(SqlScriptUpdater.SetPrimaryKeyConstraintIsClustered(primaryKeyConstraint, isClustered));
        }

        public IList<SqlScriptUpdateInfo> SetUniqueConstraintIsClustered(SqlUniqueConstraint uniqueConstraint, bool isClustered)
        {
            SqlExceptionUtils.ValidateNullParameter(uniqueConstraint, "uniqueConstraint");

            return NotifyModelUpdate(SqlScriptUpdater.SetUniqueConstraintIsClustered(uniqueConstraint, isClustered));
        }
    }
}
