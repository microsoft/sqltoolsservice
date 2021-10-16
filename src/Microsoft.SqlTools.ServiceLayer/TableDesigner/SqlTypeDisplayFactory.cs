//------------------------------------------------------------------------------
// <copyright file="SqlTypeDisplayFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;
using Microsoft.Data.Tools.Components.Diagnostics;

namespace Microsoft.Data.Relational.Design.Table
{
    internal static class SqlTypeDisplayFactory
    {
        // TODO (longch) We could consider to move those data into an auto-generated model
        public static Dictionary<SqlDataType, int> SqlTypeCanDisplayWithDefaultLengthMap = new Dictionary<SqlDataType, int>();
        public static Dictionary<SqlDataType, int> SqlTypeCanDisplayWithDefaultScaleMap = new Dictionary<SqlDataType, int>();
        public static Dictionary<SqlDataType, int> SqlTypeCanDisplayWithDefaultPrecisionMap = new Dictionary<SqlDataType, int>();

        static SqlTypeDisplayFactory()
        {
            // Add values to SqlTypeCanDisplayWithDefaultLengthMap
            SqlTypeCanDisplayWithDefaultLengthMap.Add(SqlDataType.Binary, 50);
            SqlTypeCanDisplayWithDefaultLengthMap.Add(SqlDataType.Char, 10);
            SqlTypeCanDisplayWithDefaultLengthMap.Add(SqlDataType.NChar, 10);
            SqlTypeCanDisplayWithDefaultLengthMap.Add(SqlDataType.NVarChar, 50);
            SqlTypeCanDisplayWithDefaultLengthMap.Add(SqlDataType.VarBinary, 50);
            SqlTypeCanDisplayWithDefaultLengthMap.Add(SqlDataType.VarChar, 50);

            // Add values to SqlTypeCanDisplayWithDefaultScaleMap
            SqlTypeCanDisplayWithDefaultScaleMap.Add(SqlDataType.DateTime2, 7);
            SqlTypeCanDisplayWithDefaultScaleMap.Add(SqlDataType.DateTimeOffset, 7);
            SqlTypeCanDisplayWithDefaultScaleMap.Add(SqlDataType.Time, 7);
            SqlTypeCanDisplayWithDefaultScaleMap.Add(SqlDataType.Decimal, 0);
            SqlTypeCanDisplayWithDefaultScaleMap.Add(SqlDataType.Numeric, 0);

            // Add values to SqlTypeCanDisplayWithDefaultPrecisionMap
            SqlTypeCanDisplayWithDefaultPrecisionMap.Add(SqlDataType.Decimal, 18);
            SqlTypeCanDisplayWithDefaultPrecisionMap.Add(SqlDataType.Numeric, 18);
        }

        /// <summary>
        /// Return SqlTypeDisplay for given sqlSimpleColumn
        /// </summary>
        public static SqlTypeDisplayBase GetDesignTimeSqlTypeDisplay(SqlSimpleColumn sqlSimpleColumn)
        {
            SqlExceptionUtils.ValidateNullParameter<SqlSimpleColumn>(sqlSimpleColumn, "sqlSimpleColumn", SqlTraceId.TableDesigner);

            SqlTypeSpecifierBase typeSpecifierBase = sqlSimpleColumn.TypeSpecifier;
            SqlTracer.AssertTraceEvent(typeSpecifierBase != null, TraceEventType.Error, SqlTraceId.TableDesigner, "Why typeSpecifier is null?");

            SqlTypeDisplayBase sqlTypeDisplay = null;
            if (typeSpecifierBase != null)
            {
                if (typeSpecifierBase.Type != null)
                {
                    IList<SqlTypeDisplayBase> collection = GetResolvedSqlTypeDisplays(typeSpecifierBase.Type);

                    SqlTypeSpecifier typeSpec = typeSpecifierBase as SqlTypeSpecifier;
                    sqlTypeDisplay = typeSpec != null && typeSpec.IsMax ? collection[1] : collection[0];
                }
                else
                {
                    sqlTypeDisplay = GetUnResolvedSqlTypeDisplay(typeSpecifierBase);
                }
            }

            SqlTracer.AssertTraceEvent(sqlTypeDisplay != null, TraceEventType.Error, SqlTraceId.TableDesigner, "Why sqlTypeDisplayNameFactory is null?");
            if (sqlTypeDisplay != null)
            {
                sqlTypeDisplay.UpdateWithDesignTimeValue(sqlSimpleColumn);
            }

            return sqlTypeDisplay;
        }

        /// <summary>
        /// Return list of SqlTypeDisplays for a resolved sqltype
        /// </summary>
        public static IList<SqlTypeDisplayBase> GetResolvedSqlTypeDisplays(SqlType sqlType , bool createMaxLength = true)
        {
            SqlExceptionUtils.ValidateNullParameter<SqlType>(sqlType, "sqlType", SqlTraceId.TableDesigner);

            IList<SqlTypeDisplayBase> list = new List<SqlTypeDisplayBase>();
            SqlBuiltInType sqlBuiltInType = sqlType as SqlBuiltInType;

            if (sqlBuiltInType != null)
            {
                SqlDataType sqlDataType = sqlBuiltInType.SqlDataType;

                if (SqlTypeCanDisplayWithDefaultLengthMap.ContainsKey(sqlDataType))
                {
                    list.Add(new SqlTypeDisplayWithLength(sqlBuiltInType));

                    if (createMaxLength && VMUtils.CanSqlTypeHaveMaxLength(sqlBuiltInType))
                    {
                        list.Add(new SqlTypeDisplayWithMaxLength(sqlBuiltInType));
                    }
                }
                else if (SqlTypeCanDisplayWithDefaultScaleMap.ContainsKey(sqlDataType) && SqlTypeCanDisplayWithDefaultPrecisionMap.ContainsKey(sqlDataType))
                {
                    list.Add(new SqlTypeDisplayWithScaleAndPrecision(sqlBuiltInType));
                }
                else if (SqlTypeCanDisplayWithDefaultScaleMap.ContainsKey(sqlDataType) && !SqlTypeCanDisplayWithDefaultPrecisionMap.ContainsKey(sqlDataType))
                {
                    list.Add(new SqlTypeDisplayWithScale(sqlBuiltInType));
                }
                else
                {
                    list.Add(new SqlTypeDisplayDefault(sqlType));
                }
            }
            else
            {
                list.Add(new SqlTypeDisplayDefault(sqlType));
            }

            foreach (SqlTypeDisplayBase typeDisplay in list)
            {
                typeDisplay.Initialize();
            }

            return list;
        }

        public static SqlTypeDisplayBase GetUnResolvedSqlTypeDisplay(SqlTypeSpecifierBase sqlTypeSpecifier)
        {
            SqlExceptionUtils.ValidateNullParameter<SqlTypeSpecifierBase>(sqlTypeSpecifier, "sqlTypeSpecifier", SqlTraceId.TableDesigner);
            return new SqlUnResolvedTypeDisplay(sqlTypeSpecifier);
        }
    }
}
