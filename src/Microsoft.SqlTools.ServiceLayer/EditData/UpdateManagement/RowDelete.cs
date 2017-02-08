//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Globalization;
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

        public RowDelete(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
        }

        public /* override */ DbCommand GetCommitCommand()
        {
            throw new NotImplementedException();
        }

        public override string GetScript()
        {
            string formatString = AssociatedObjectMetadata.IsHekaton ? DeleteHekatonStatement : DeleteStatement;
            return string.Format(CultureInfo.InvariantCulture, formatString,
                AssociatedObjectMetadata.EscapedMultipartName, GetWhereClause(false).CommandText);
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
