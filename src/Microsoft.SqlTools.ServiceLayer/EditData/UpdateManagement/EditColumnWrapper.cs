using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public class EditColumnWrapper : IEditColumnWrapper
    {
        public EditColumnWrapper(int ordinal, DbColumnWrapper dbColumn, Column smoColumn)
        {
            // Store the original sources of truth
            DbColumn = dbColumn;

            // Assign the fields we need
            Ordinal = ordinal;
            EscapedName = SqlScriptFormatter.FormatIdentifier(dbColumn.ColumnName);

            // A column is trustworthy for uniqueness if it can be updated or it has an identity
            // property. If both of these are false (eg, timestamp) we can't trust it to uniquely
            // identify a row in the table
            IsTrustworthyForUniqueness = dbColumn.IsUpdatable || smoColumn.Identity;

            // A key column is determined by whether it is in the primary key and trustworthy
            IsKey = smoColumn.InPrimaryKey && IsTrustworthyForUniqueness;
        }

        public DbColumnWrapper DbColumn { get; }

        public string EscapedName { get; }

        public bool IsKey { get; }

        public bool IsTrustworthyForUniqueness { get; }

        public int Ordinal { get; }
    }
}
