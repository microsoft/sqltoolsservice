using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class DbColumnWrapper
    {
        private DbColumn InternalColumn { get; set; }
        private string providerSpecificDataTypeName;    // Unclear if needed
        private Type type;                              // Unclear if needed
        private int maxLength;                          // Unclear if needed

        public bool IsUdt { get; private set; }
        public bool IsLongField { get; private set; }
        public bool IsChars { get; private set; }
        public bool IsBytes { get; private set; }
        public bool IsXml { get; private set; }
        public bool IsSqlVariant { get; private set; }

        #region DbColumn Fields

        public bool? AllowDBNull { get { return InternalColumn.AllowDBNull; } }
        public string BaseCatalogName { get { return InternalColumn.BaseCatalogName; } }
        public string BaseColumnName { get { return InternalColumn.BaseColumnName; } }
        public string BaseServerName { get { return InternalColumn.BaseServerName; } }
        public string BaseTableName { get { return InternalColumn.BaseTableName; } }

        public string ColumnName
        {
            get
            {
                return string.IsNullOrEmpty(InternalColumn.ColumnName) ? "(No column name)" : InternalColumn.ColumnName;
            }
        }

        public int? ColumnOrdinal { get { return InternalColumn.ColumnOrdinal; } }
        public int? ColumnSize { get { return InternalColumn.ColumnSize; } }
        public bool? IsAliased { get { return InternalColumn.IsAliased; } }
        public bool? IsAutoIncrement { get { return InternalColumn.IsAutoIncrement; } }
        public bool? IsExpression { get { return InternalColumn.IsExpression; } }
        public bool? IsHidden { get { return InternalColumn.IsHidden; } }
        public bool? IsIdentity { get { return InternalColumn.IsIdentity; } }
        public bool? IsKey { get { return InternalColumn.IsKey; } }
        public bool? IsLong { get { return InternalColumn.IsLong; } }
        public bool? IsReadOnly { get { return InternalColumn.IsReadOnly; } }
        public bool? IsUnique { get { return InternalColumn.IsUnique; } }
        public int? NumericPrecision { get { return InternalColumn.NumericPrecision; } }
        public int? NumericScale { get { return InternalColumn.NumericScale; } }
        public string UdtAssemblyQualifiedName { get { return InternalColumn.UdtAssemblyQualifiedName; } }
        public Type DataType { get { return InternalColumn.DataType; } }
        public string DataTypeName { get { return InternalColumn.DataTypeName; } }

        #endregion

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

            switch (column.DataTypeName)
            {
                case "varchar":
                case "nvarchar":
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
                    break;
                case "text":
                case "ntext":
                    IsChars = true;
                    IsLongField = true;
                    break;
                case "xml":
                    IsXml = true;
                    IsLongField = true;
                    break;
                case "binary":
                case "image":
                    IsBytes = true;
                    IsLongField = true;
                    break;
                case "varbinary":
                case "rowversion":
                    IsBytes = true;

                    Debug.Assert(column.ColumnSize.HasValue);
                    if (column.ColumnSize.Value == int.MaxValue)
                    {
                        IsLongField = true;
                    }
                    break;
                case "sql_variant":
                    IsSqlVariant = true;
                    break;
                default:
                    if (!allServerDataTypes.Contains(column.DataTypeName))
                    {
                        // treat all UDT's as long/bytes data types to prevent the CLR from attempting
                        // to load the UDT assembly into our process to call ToString() on the object.

                        IsUdt = true;
                        IsBytes = true;
                        IsLongField = true;
                    }
                    break;
            }


            if(IsUdt)
            {
                // udtassemblyqualifiedname property is used to find if the datatype is of hierarchyid assembly type 
                // Internally hiearchyid is sqlbinary so providerspecific type and type is changed to sqlbinarytype
                object assemblyQualifiedName = InternalColumn.UdtAssemblyQualifiedName;
                const string hierarchyId = "MICROSOFT.SQLSERVER.TYPES.SQLHIERARCHYID";

                if(assemblyQualifiedName != null && string.Equals(assemblyQualifiedName.ToString(), hierarchyId, StringComparison.OrdinalIgnoreCase))
                {
                    providerSpecificDataTypeName = "System.Data.SqlTypes.SqlBinary";
                    type = typeof(SqlBinary);
                } else
                {
                    providerSpecificDataTypeName = "System.Byte[]";
                    type = typeof(byte[]);
                    maxLength = int.MaxValue;
                }

            }
        }
    }
}
