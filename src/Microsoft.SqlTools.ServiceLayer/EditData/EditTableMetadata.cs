
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public class EditTableMetadata : IEditTableMetadata
    {
        private readonly List<EditColumnWrapper> columns;
        private readonly List<EditColumnWrapper> keyColumns;

        public EditTableMetadata(IList<DbColumnWrapper> dbColumns, TableViewTableTypeBase smoObject)
        {
            // Make sure that we have equal columns on both metadata providers
            Debug.Assert(dbColumns.Count == smoObject.Columns.Count);

            // Create the columns for edit usage
            columns = new List<EditColumnWrapper>();
            for (int i = 0; i < dbColumns.Count; i++)
            {
                Column smoColumn = smoObject.Columns[i];
                DbColumnWrapper dbColumn = dbColumns[i];

                // A column is trustworthy for uniqueness if it can be updated or it has an identity
                // property. If both of these are false (eg, timestamp) we can't trust it to uniquely
                // identify a row in the table
                bool isTrustworthyForUniqueness = dbColumn.IsUpdatable || smoColumn.Identity;

                EditColumnWrapper column = new EditColumnWrapper
                {
                    DbColumn = dbColumn,
                    Ordinal = i,
                    EscapedName = SqlScriptFormatter.FormatIdentifier(dbColumn.ColumnName),
                    IsTrustworthyForUniqueness = isTrustworthyForUniqueness,

                    // A key column is determined by whether it is in the primary key and trustworthy
                    IsKey = smoColumn.InPrimaryKey && isTrustworthyForUniqueness
                };
                columns.Add(column);
            }

            // Determine what the key columns are
            keyColumns = columns.Where(c => c.IsKey).ToList();
            if (keyColumns.Count == 0)
            {
                // We didn't find any explicit key columns. Instead, we'll use all columns that are
                // trustworthy for uniqueness (usually all the columns)
                keyColumns = columns.Where(c => c.IsTrustworthyForUniqueness).ToList();
            }

            // If a table is memory optimized it is Hekaton. If it's a view, then it can't be Hekaton
            Table smoTable = smoObject as Table;
            IsHekaton = smoTable != null && smoTable.IsMemoryOptimized;

            // Escape the parts of the name
            string[] objectNameParts = {smoObject.Schema, smoObject.Name};
            EscapedMultipartName = SqlScriptFormatter.FormatMultipartIdentifier(objectNameParts);
        }

        public IEnumerable<EditColumnWrapper> Columns => columns;
        public string EscapedMultipartName { get; }
        public bool IsHekaton { get; }
        public IEnumerable<EditColumnWrapper> KeyColumns => keyColumns;
    }
}
