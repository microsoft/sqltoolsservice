//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Represents a row that should be deleted. This will generate a DELETE statement
    /// </summary>
    public sealed class RowDelete : RowEditBase
    {
        private const string DeleteStatement = "DELETE FROM {0} {1}";
        private const string DeleteHekatonStatement = "DELETE FROM {0} WITH(SNAPSHOT) {1}";

        public RowDelete(long rowId, ResultSet associatedResultSet, string associatedObject)
            : base(rowId, associatedResultSet, associatedObject)
        {
        }

        public /* override */ DbCommand GetCommitCommand()
        {
            DbColumn firstColumn = AssociatedResultSet.Columns[0];
            throw new NotImplementedException();
        }

        public override string GetScript()
        {
            // @TODO: Determine if this is a hekaton table and use the appropriate statement.
            return string.Format(DeleteStatement, AssociatedObject, GetWhereClause(false).CommandText);
        }

        /// <summary>
        /// This method should not be called on 
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public override string SetCell(int columnId, string newValue)
        {
            // @TODO: Move to constants file
            throw new InvalidOperationException("A delete is pending for this row, a cell update cannot be applied.");
        }
    }
}
