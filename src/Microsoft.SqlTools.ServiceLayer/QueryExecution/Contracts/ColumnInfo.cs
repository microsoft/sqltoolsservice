using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class DbColumnWrapper
    {
        private DbColumn InternalColumn { get; set; }

        public bool IsUdt { get; private set; }
        public bool IsLongField { get; private set; }
        public bool IsChars { get; private set; }
        public bool IsBytes { get; private set; }
        public bool IsXml { get; private set; }
        public bool IsSqlVariant { get; private set; }

        private static readonly HashSet<string> allServerDataTypes = new HashSet<string>
        {
            "bigint",
            "binary",
            "bit",
            "char",
            "datatime",
            "decimal",
            "float",
            "image",
            "int",
            "money",
            "nchar",
            "ntext",
            "nvarchar",
            "real",
            "uniqueidentifier",
            "smalldatetime",
            "smallint",
            "smallmoney",
            "text",
            "timestamp",
            "tinyint",
            "varbinary",
            "varchar",
            "sql_variant",
            "xml",
            "date",
            "time",
            "datetimeoffset",
            "datetime2"
        };

        public DbColumnWrapper(DbColumn column)
        {
            InternalColumn = column;

            if (column.DataTypeName == "varchar" || column.DataTypeName == "nvarchar")
            {
                IsChars = true;

                Debug.Assert(column.ColumnSize.HasValue);
                if (column.ColumnSize.Value == int.MaxValue)
                {
                    //For Yukon, special case nvarchar(max) with column name == "Microsoft SQL Server 2005 XML Showplan" -
                    //assume it is an XML showplan.
                    //Please note this field must be in sync with a similar field defined in QESQLBatch.cs.
                    //This is not the best fix that we could do but we are trying to minimize code impact
                    //at this point. Post Yukon we should review this code again and avoid
                    //hard-coding special column name in multiple places.
                    const string YukonXmlShowPlanColumn = "Microsoft SQL Server 2005 XML Showplan";
                    if (column.ColumnName == YukonXmlShowPlanColumn)
                    {
                        // Indicate that this is xml to apply the right size limit
                        // Note we leave chars type as well to use the right retrieval mechanism.
                        IsXml = true;
                    }
                    IsLongField = true;
                }
            }
            else if (column.DataTypeName == "text" || column.DataTypeName == "ntext")
            {
                IsChars = true;
                IsLongField = true;
            }
            else if (column.DataTypeName == "xml")
            {
                IsXml = true;
                IsLongField = true;
            }
            else if (column.DataTypeName == "binary" || column.DataTypeName == "image")
            {
                IsBytes = true;
                IsLongField = true;
            }
            else if (column.DataTypeName == "varbinary" || column.DataTypeName == "rowversion")
            {
                IsBytes = true;

                Debug.Assert(column.ColumnSize.HasValue);
                if (column.ColumnSize.Value == int.MaxValue)
                {
                    IsLongField = true;
                }
            }
            else if (column.DataTypeName == "sql_variant")
            {
                IsSqlVariant = true;
            }
            else if (!allServerDataTypes.Contains(column.DataTypeName))
            {
                // treat all UDT's as long/bytes data types to prevent the CLR from attempting
                // to load the UDT assembly into our process to call ToString() on the object.

                IsUdt = true;
                IsBytes = true;
                IsLongField = true;
            }
        }
    }
}
