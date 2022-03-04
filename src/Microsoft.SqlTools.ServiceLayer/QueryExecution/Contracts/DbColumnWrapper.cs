﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Wrapper around a DbColumn, which provides extra functionality, but can be used as a
    /// regular DbColumn
    /// </summary>
    public class DbColumnWrapper : DbColumn
    {
        #region Constants

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

        private const string SqlXmlDataTypeName = "xml";
        private const string DbTypeXmlDataTypeName = "DBTYPE_XML";
        private const string UnknownTypeName = "unknown";

        #endregion

        /// <summary>
        /// Constructor for a DbColumnWrapper
        /// </summary>
        /// <remarks>Most of this logic is taken from SSMS ColumnInfo class</remarks>
        /// <param name="column">The column we're wrapping around</param>
        public DbColumnWrapper(DbColumn column)
        {
            // Set all the fields for the base
            AllowDBNull = column.AllowDBNull;
            BaseCatalogName = column.BaseCatalogName;
            BaseColumnName = column.BaseColumnName;
            BaseSchemaName = column.BaseSchemaName;
            BaseServerName = column.BaseServerName;
            BaseTableName = column.BaseTableName;
            ColumnOrdinal = column.ColumnOrdinal;
            ColumnSize = column.ColumnSize;
            IsAliased = column.IsAliased;
            IsAutoIncrement = column.IsAutoIncrement;
            IsExpression = column.IsExpression;
            IsHidden = column.IsHidden;
            IsIdentity = column.IsIdentity;
            IsKey = column.IsKey;
            IsLong = column.IsLong;
            IsReadOnly = column.IsReadOnly;
            IsUnique = column.IsUnique;
            NumericPrecision = column.NumericPrecision;
            NumericScale = column.NumericScale;
            UdtAssemblyQualifiedName = column.UdtAssemblyQualifiedName;
            DataType = column.DataType;
            DataTypeName = column.DataTypeName.ToLowerInvariant();

            DetermineSqlDbType();
            AddNameAndDataFields(column.ColumnName);

            if (IsUdt)
            {
                // udtassemblyqualifiedname property is used to find if the datatype is of hierarchyid assembly type 
                // Internally hiearchyid is sqlbinary so providerspecific type and type is changed to sqlbinarytype
                object assemblyQualifiedName = column.UdtAssemblyQualifiedName;
                const string hierarchyId = "MICROSOFT.SQLSERVER.TYPES.SQLHIERARCHYID";

                if (assemblyQualifiedName != null
                && assemblyQualifiedName.ToString().StartsWith(hierarchyId, StringComparison.OrdinalIgnoreCase))
                {
                    DataType = typeof(SqlBinary);
                }
                else
                {
                    DataType = typeof(byte[]);
                }
            }
            else
            {
                DataType = column.DataType;
            }
        }

        public DbColumnWrapper(ColumnInfo columnInfo)
        {
            DataTypeName = columnInfo.DataTypeName.ToLowerInvariant();
            DetermineSqlDbType();
            DataType = TypeConvertor.ToNetType(this.SqlDbType);
            if (DataType == typeof(String))
            {
                this.ColumnSize = int.MaxValue;
            }
            AddNameAndDataFields(columnInfo.Name);
        }


        /// <summary>
        /// Default constructor, used for deserializing JSON RPC only
        /// </summary>
        public DbColumnWrapper()
        {
        }

        #region Properties

        /// <summary>
        /// Whether or not the column is bytes
        /// </summary>
        public bool IsBytes { get; private set; }

        /// <summary>
        /// Whether or not the column is a character type
        /// </summary>
        public bool IsChars { get; private set; }

        /// <summary>
        /// Whether or not the column is a SqlVariant type
        /// </summary>
        public bool IsSqlVariant { get; private set; }

        /// <summary>
        /// Whether or not the column is a user-defined type
        /// </summary>
        public bool IsUdt { get; private set; }

        /// <summary>
        /// Whether or not the column is XML
        /// </summary>
        public bool IsXml { get; set; }

        /// <summary>
        /// Whether or not the column is JSON
        /// </summary>
        public bool IsJson { get; set; }

        /// <summary>
        /// The SqlDbType of the column, for use in a SqlParameter
        /// </summary>
        public SqlDbType SqlDbType { get; private set; }

        /// <summary>
        /// Whther this is a HierarchyId column
        /// </summary>
        public bool IsHierarchyId { get; set; }

        /// <summary>
        /// Whether or not the column is an XML Reader type.
        /// </summary>
        /// <remarks>
        /// Logic taken from SSDT determination of whether a column is a SQL XML type. It may not
        /// be possible to have XML readers from .NET Core SqlClient.
        /// </remarks>
        public bool IsSqlXmlType => DataTypeName.Equals(SqlXmlDataTypeName, StringComparison.OrdinalIgnoreCase) ||
                                    DataTypeName.Equals(DbTypeXmlDataTypeName, StringComparison.OrdinalIgnoreCase) ||
                                    DataType == typeof(System.Xml.XmlReader);

        /// <summary>
        /// Whether or not the column is an unknown type
        /// </summary>
        /// <remarks>
        /// Logic taken from SSDT determination of unknown columns. It may not even be possible to
        /// have "unknown" column types with the .NET Core SqlClient.
        /// </remarks>
        public bool IsUnknownType => DataType == typeof(object) &&
                                     DataTypeName.Equals(UnknownTypeName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Whether or not the column can be updated, based on whether it's an auto increment
        /// column, is an XML reader column, and if it's read only.
        /// </summary>
        /// <remarks>
        /// Logic taken from SSDT determination of updatable columns
        /// Special treatment for HierarchyId since we are using an Expression for HierarchyId column and expression column is readonly.
        /// </remarks>
        public bool IsUpdatable => (!IsAutoIncrement.HasTrue() &&
                                   !IsReadOnly.HasTrue() &&
                                   !IsSqlXmlType) || IsHierarchyId;

        #endregion


        private void DetermineSqlDbType()
        {
            // Determine the SqlDbType
            SqlDbType type;
            if (Enum.TryParse(DataTypeName, true, out type))
            {
                SqlDbType = type;
            }
            else
            {
                switch (DataTypeName)
                {
                    case "numeric":
                        SqlDbType = SqlDbType.Decimal;
                        break;
                    case "sql_variant":
                        SqlDbType = SqlDbType.Variant;
                        break;
                    case "timestamp":
                        SqlDbType = SqlDbType.VarBinary;
                        break;
                    case "sysname":
                        SqlDbType = SqlDbType.NVarChar;
                        break;
                    default:
                        SqlDbType = DataTypeName.EndsWith(".sys.hierarchyid") ? SqlDbType.NVarChar : SqlDbType.Udt;
                        break;
                }
            }
        }

        private void AddNameAndDataFields(string columnName)
        {
            // We want the display name for the column to always exist
            ColumnName = string.IsNullOrEmpty(columnName)
                ? SR.QueryServiceColumnNull
                : columnName;

            switch (DataTypeName)
            {
                case "varchar":
                case "nvarchar":
                    IsChars = true;

                    Debug.Assert(ColumnSize.HasValue);
                    if (ColumnSize.Value == int.MaxValue)
                    {
                        //For Yukon, special case nvarchar(max) with column name == "Microsoft SQL Server 2005 XML Showplan" -
                        //assume it is an XML showplan.
                        //Please note this field must be in sync with a similar field defined in QESQLBatch.cs.
                        //This is not the best fix that we could do but we are trying to minimize code impact
                        //at this point. Post Yukon we should review this code again and avoid
                        //hard-coding special column name in multiple places.
                        const string yukonXmlShowPlanColumn = "Microsoft SQL Server 2005 XML Showplan";
                        if (columnName == yukonXmlShowPlanColumn)
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

                    Debug.Assert(ColumnSize.HasValue);
                    if (ColumnSize.Value == int.MaxValue)
                    {
                        IsLong = true;
                    }
                    break;
                case "sql_variant":
                    IsSqlVariant = true;
                    break;
                default:
                    if (!AllServerDataTypes.Contains(DataTypeName))
                    {
                        // treat all UDT's as long/bytes data types to prevent the CLR from attempting
                        // to load the UDT assembly into our process to call ToString() on the object.

                        IsUdt = true;
                        IsBytes = true;
                        IsLong = true;
                    }
                    break;
            }
        }
    }



    /// <summary>
    /// Convert a base data type to another base data type
    /// </summary>
    public sealed class TypeConvertor
    {
        private static Dictionary<SqlDbType,Type> _typeMap = new Dictionary<SqlDbType,Type>();

        static TypeConvertor()
        {
            _typeMap[SqlDbType.BigInt] = typeof(Int64);
            _typeMap[SqlDbType.Binary] = typeof(Byte);
            _typeMap[SqlDbType.Bit] = typeof(Boolean);
            _typeMap[SqlDbType.Char] = typeof(String);
            _typeMap[SqlDbType.DateTime] = typeof(DateTime);
            _typeMap[SqlDbType.Decimal] = typeof(Decimal);
            _typeMap[SqlDbType.Float] = typeof(Double);
            _typeMap[SqlDbType.Image] = typeof(Byte[]);
            _typeMap[SqlDbType.Int] = typeof(Int32);
            _typeMap[SqlDbType.Money] = typeof(Decimal);
            _typeMap[SqlDbType.NChar] = typeof(String);
            _typeMap[SqlDbType.NChar] = typeof(String);
            _typeMap[SqlDbType.NChar] = typeof(String);
            _typeMap[SqlDbType.NText] = typeof(String);
            _typeMap[SqlDbType.NVarChar] = typeof(String);
            _typeMap[SqlDbType.Real] = typeof(Single);
            _typeMap[SqlDbType.UniqueIdentifier] = typeof(Guid);
            _typeMap[SqlDbType.SmallDateTime] = typeof(DateTime);
            _typeMap[SqlDbType.SmallInt] = typeof(Int16);
            _typeMap[SqlDbType.SmallMoney] = typeof(Decimal);
            _typeMap[SqlDbType.Text] = typeof(String);
            _typeMap[SqlDbType.Timestamp] = typeof(Byte[]);
            _typeMap[SqlDbType.TinyInt] = typeof(Byte);
            _typeMap[SqlDbType.VarBinary] = typeof(Byte[]);
            _typeMap[SqlDbType.VarChar] = typeof(String);
            _typeMap[SqlDbType.Variant] = typeof(Object);
            // Note: treating as string
            _typeMap[SqlDbType.Xml] = typeof(String);
            _typeMap[SqlDbType.TinyInt] = typeof(Byte);
            _typeMap[SqlDbType.TinyInt] = typeof(Byte);
            _typeMap[SqlDbType.TinyInt] = typeof(Byte);
            _typeMap[SqlDbType.TinyInt] = typeof(Byte);
        }

        private TypeConvertor()
        {

        }


        /// <summary>
        /// Convert TSQL type to .Net data type
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static Type ToNetType(SqlDbType sqlDbType)
        {
            Type netType;
            if (!_typeMap.TryGetValue(sqlDbType, out netType))
            {
                netType = typeof(String);
            }
            return netType;
        }
    }
}
