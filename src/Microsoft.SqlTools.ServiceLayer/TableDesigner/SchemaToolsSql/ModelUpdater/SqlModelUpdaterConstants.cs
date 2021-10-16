//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdaterConstants.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.ScriptDom.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using CodeGenerationSupporter = Microsoft.Data.Tools.Schema.ScriptDom.Sql.CodeGenerationSupporter;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    /// <summary>
    /// Constants used by model updater, should only add entries in this class if it doesn't exist in CodeGenerationSupporter
    /// </summary>
    internal static class SqlModelUpdaterConstants
    {
        // Keywords
        public const string AS = "AS";
        public const string ASC = "ASC";
        public const string BEGIN = "BEGIN";
        public const string CHECK = "CHECK";
        public const string CLUSTERED = "CLUSTERED";
        public const string COLUMN = "COLUMN";
        public const string COLUMNSTORE = "COLUMNSTORE";
        public const string CONSTRAINT = "CONSTRAINT";
        public const string CREATE = "CREATE";
        public const string DELETE = "DELETE";
        public const string DESC = "DESC";
        public const string END = "END";
        public const string EXEC = "EXEC";
        public const string FOR = "FOR";
        public const string FOREIGN = "FOREIGN";
        public const string GO = "GO";
        public const string INDEX = "INDEX";
        public const string INSERT = "INSERT";
        public const string NONCLUSTERED = "NONCLUSTERED";
        public const string NOT = "NOT";
        public const string NULL = "NULL";
        public const string ON = "ON";
        public const string PATH = "PATH";
        public const string PRIMARY = "PRIMARY";
        public const string REFERENCES = "REFERENCES";
        public const string SELECTIVE = "SELECTIVE";
        public const string SET = "SET";
        public const string TABLE = "TABLE";
        public const string UNIQUE = "UNIQUE";
        public const string UPDATE = "UPDATE";
        public const string WITH = "WITH";

        // identifiers
        public const string SP_ADDEXTENDEDPROPERTY = "sp_addextendedproperty";
        public const string PlaceholderColumnName = "Column";
        public const string PlaceholderForeignTableName = "ToTable";
        public const string PlaceholderForeignColumnName = "ToTableColumn";
        public const string PlaceholderUniqueIndexName = "unique_index_name";
        public const string PlaceholderCatalogName = "fulltext_catalog_name";
        public const string PlaceholderXmlIndexName = "xml_index_name";
        public const string PlaceholderSelectiveXmlIndexName = "selective_xml_index_name";
        public const string PlaceholderXPathName = "xpath_name";

        // literals
        public const string PlaceholderXPathLiteral = "xpath";

        // symbols
        public const string NewLine = "\n";
        public const string Return = "\r";
        public const string Space = " ";

        // expressions
        public const string InitialCheckConstraintExpression = "(1=1)";
        public const int DefaultNCharLength = 10;

        // properties
        public const string MS_Description = "MS_Description";

        public const int Indent = 4;

        public static HashSet<SqlDataType> SqlTypesCanHaveLength;
        public static HashSet<SqlDataType> SqlTypesCanHaveMaxLength;
        public static HashSet<SqlDataType> SqlTypesCanHavePrecision;
        public static HashSet<SqlDataType> SqlTypesCanHaveScale;

        public static string[] ExtendedPropertyTypeParameterNames;
        public static string[] ExtendedPropertyNameParameterNames;
        public static Dictionary<ModelElementClass, string[]> ExtendedPropertyTypeParameterValues;
        public static Dictionary<ModelElementClass, Func<ISqlExtendedPropertyHost, string>[]> ExtendedPropertyNameParameterValues;

        static SqlModelUpdaterConstants()
        {
            SqlTypesCanHaveLength = new HashSet<SqlDataType>();
            foreach (SqlDataTypeOption item in SqlInterpretationConstants.SqlTypesCanHaveLength)
            {
                SqlTypesCanHaveLength.Add((SqlDataType)item);
            }

            SqlTypesCanHaveMaxLength = new HashSet<SqlDataType>();
            foreach (SqlDataTypeOption item in SqlInterpretationConstants.SqlTypesCanHaveMaxLength)
            {
                SqlTypesCanHaveMaxLength.Add((SqlDataType)item);
            }

            SqlTypesCanHavePrecision = new HashSet<SqlDataType>();
            foreach (SqlDataTypeOption item in SqlInterpretationConstants.SqlTypesCanHavePrecision)
            {
                SqlTypesCanHavePrecision.Add((SqlDataType)item);
            }

            SqlTypesCanHaveScale = new HashSet<SqlDataType>();
            foreach (SqlDataTypeOption item in SqlInterpretationConstants.SqlTypesCanHaveScale)
            {
                SqlTypesCanHaveScale.Add((SqlDataType)item);
            }

            ExtendedPropertyTypeParameterNames = new string[] 
            { 
                SqlInterpretationConstants.AtLevel0Type,
                SqlInterpretationConstants.AtLevel1Type,
                SqlInterpretationConstants.AtLevel2Type,
            };

            ExtendedPropertyNameParameterNames = new string[] 
            { 
                SqlInterpretationConstants.AtLevel0Name,
                SqlInterpretationConstants.AtLevel1Name,
                SqlInterpretationConstants.AtLevel2Name,
            };

            ExtendedPropertyTypeParameterValues = new Dictionary<ModelElementClass, string[]>
            { 
                { SqlTable.SqlTableClass, new [] {CodeGenerationSupporter.Schema, TABLE, null}},
                { SqlSimpleColumn.SqlSimpleColumnClass, new [] {CodeGenerationSupporter.Schema, TABLE, COLUMN}},
                { SqlComputedColumn.SqlComputedColumnClass, new [] {CodeGenerationSupporter.Schema, TABLE, COLUMN}},
                { SqlColumnSet.SqlColumnSetClass, new [] {CodeGenerationSupporter.Schema, TABLE, COLUMN }}
            };

            ExtendedPropertyNameParameterValues = new Dictionary<ModelElementClass,Func<ISqlExtendedPropertyHost,string>[]>()
            { 
                { SqlTable.SqlTableClass, new Func<ISqlExtendedPropertyHost, string> [] 
                    {
                        host => host.Name.Parts[host.Name.Parts.Count - 2], // level 0: schema
                        host => host.Name.Parts[host.Name.Parts.Count - 1], // level 1: table name
                        host => null, // no level 2
                    }
                },

                { SqlSimpleColumn.SqlSimpleColumnClass, new Func<ISqlExtendedPropertyHost, string> [] 
                    {
                        host => host.Name.Parts[host.Name.Parts.Count - 3], // level 0: schema
                        host => host.Name.Parts[host.Name.Parts.Count - 2], // level 1: table name
                        host => host.Name.Parts[host.Name.Parts.Count - 1], // level 2: column name
                    }
                },

                { SqlComputedColumn.SqlComputedColumnClass, new Func<ISqlExtendedPropertyHost, string> [] 
                    {
                        host => host.Name.Parts[host.Name.Parts.Count - 3], // level 0: schema
                        host => host.Name.Parts[host.Name.Parts.Count - 2], // level 1: table name
                        host => host.Name.Parts[host.Name.Parts.Count - 1], // level 2: column name
                    }
                },

                { SqlColumnSet.SqlColumnSetClass, new Func<ISqlExtendedPropertyHost, string> [] 
                    {
                        host => host.Name.Parts[host.Name.Parts.Count - 3], // level 0: schema
                        host => host.Name.Parts[host.Name.Parts.Count - 2], // level 1: table name
                        host => host.Name.Parts[host.Name.Parts.Count - 1], // level 2: column name
                    }
                },
            };
        }
    }
}