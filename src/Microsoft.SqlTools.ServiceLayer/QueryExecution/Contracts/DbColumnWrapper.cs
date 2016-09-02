//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Wrapper around a DbColumn, which provides extra functionality, but can be used as a
    /// regular DbColumn
    /// </summary>
    public class DbColumnWrapper : DbColumn
    {
        #region Properties

        private DbColumn InternalColumn { get; set; }
        private Type type;                              // Unclear if needed

        /// <summary>
        /// Whether or not the column is a user-defined type
        /// </summary>
        public bool IsUdt { get; private set; }
        /// <summary>
        /// Whether or not the column is a long type (eg, varchar(MAX))
        /// </summary>
        public new bool IsLong { get; private set; }
        /// <summary>
        /// Whether or not the column is a character type
        /// </summary>
        public bool IsChars { get; private set; }
        /// <summary>
        /// Whether or not the column is bytes
        /// </summary>
        public bool IsBytes { get; private set; }
        /// <summary>
        /// Whether or not the column is XML
        /// </summary>
        public bool IsXml { get; private set; }
        /// <summary>
        /// Whether or not the column is a SqlVariant type
        /// </summary>
        public bool IsSqlVariant { get; private set; }

        #endregion

        #region DbColumn Fields

        /// <summary>
        /// Override for column name, if null or empty, we default to a "no column name" value
        /// </summary>
        public new string ColumnName
        {
            get
            {
                // TODO: Localize
                return string.IsNullOrEmpty(InternalColumn.ColumnName) ? "(No column name)" : InternalColumn.ColumnName;
            }
        }

        public new bool? AllowDBNull { get { return InternalColumn.AllowDBNull; } }
        public new string BaseCatalogName { get { return InternalColumn.BaseCatalogName; } }
        public new string BaseColumnName { get { return InternalColumn.BaseColumnName; } }
        public new string BaseServerName { get { return InternalColumn.BaseServerName; } }
        public new string BaseTableName { get { return InternalColumn.BaseTableName; } }
        public new int? ColumnOrdinal { get { return InternalColumn.ColumnOrdinal; } }
        public new int? ColumnSize { get { return InternalColumn.ColumnSize; } }
        public new bool? IsAliased { get { return InternalColumn.IsAliased; } }
        public new bool? IsAutoIncrement { get { return InternalColumn.IsAutoIncrement; } }
        public new bool? IsExpression { get { return InternalColumn.IsExpression; } }
        public new bool? IsHidden { get { return InternalColumn.IsHidden; } }
        public new bool? IsIdentity { get { return InternalColumn.IsIdentity; } }
        public new bool? IsKey { get { return InternalColumn.IsKey; } }
        public new bool? IsReadOnly { get { return InternalColumn.IsReadOnly; } }
        public new bool? IsUnique { get { return InternalColumn.IsUnique; } }
        public new int? NumericPrecision { get { return InternalColumn.NumericPrecision; } }
        public new int? NumericScale { get { return InternalColumn.NumericScale; } }
        public new string UdtAssemblyQualifiedName { get { return InternalColumn.UdtAssemblyQualifiedName; } }
        public new Type DataType { get { return InternalColumn.DataType; } }
        public new string DataTypeName { get { return InternalColumn.DataTypeName; } }

        #endregion

        /// <summary>
        /// All types supported by the server, stored as a hash set to provide O(1) lookup
        /// </summary>
        private static readonly HashSet<string> AllServerDataTypes = new HashSet<string>
        {
            "bigint",
            "binary",
            "bit",
            "char",
            "datetime",
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

        /// <summary>
        /// Constructor for a DbColumnWrapper
        /// </summary>
        /// <remarks>Most of this logic is taken from SSMS ColumnInfo class</remarks>
        /// <param name="column">The column we're wrapping around</param>
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
                        IsLong = true;
                    }
                    break;
                case "text":
                case "ntext":
                    IsChars = true;
                    IsLong = true;
                    break;
                case "xml":
                    IsXml = true;
                    IsLong = true;
                    break;
                case "binary":
                case "image":
                    IsBytes = true;
                    IsLong = true;
                    break;
                case "varbinary":
                case "rowversion":
                    IsBytes = true;

                    Debug.Assert(column.ColumnSize.HasValue);
                    if (column.ColumnSize.Value == int.MaxValue)
                    {
                        IsLong = true;
                    }
                    break;
                case "sql_variant":
                    IsSqlVariant = true;
                    break;
                default:
                    if (!AllServerDataTypes.Contains(column.DataTypeName))
                    {
                        // treat all UDT's as long/bytes data types to prevent the CLR from attempting
                        // to load the UDT assembly into our process to call ToString() on the object.

                        IsUdt = true;
                        IsBytes = true;
                        IsLong = true;
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
                    //providerSpecificDataTypeName = "System.Data.SqlTypes.SqlBinary";
                    type = typeof(SqlBinary);
                } else
                {
                    //providerSpecificDataTypeName = "System.Byte[]";
                    //maxLength = int.MaxValue;
                    type = typeof(byte[]);
                }

            }
        }
    }
}
