//------------------------------------------------------------------------------
// <copyright file="InstDataTypesContainer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;

namespace Microsoft.Data.Relational.Design.Table
{
    /// <summary>
    /// InstHekatonDataTypesContainer provides function of sql data types.
    /// Only the following system types are supported in Hekaton Table.
    ///    •	Numeric Types
    ///    •	String and Binary Types
    ///    •	datetime (Transact-SQL)
    ///    •	smalldatetime (Transact-SQL)
    ///    •	uniqueidentifier (Transact-SQL)
    /// The max row size is 8060 bytes. The (n) in varchar(n), nvarchar(n) and varbinary(n) count toward this limit.
    /// </summary>
    internal sealed class InstHekatonDataTypesContainer : InstDataTypesContainer
    {

        internal InstHekatonDataTypesContainer(SqlSchemaModel sqlSchemaModel, bool withDefaultValue)
            : base(sqlSchemaModel, withDefaultValue)
        {
        }

        protected override IList<SqlTypeDisplayBase> GetResolvedSqlTypeDisplays(SqlType sqlType)
        {
            return SqlTypeDisplayFactory.GetResolvedSqlTypeDisplays(sqlType, false);
        }

        protected override void InitDataTypes(SqlSchemaModel sqlSchemaModel)
        {
            SqlExceptionUtils.ValidateNullParameter(sqlSchemaModel, "sqlSchemaModel", SqlTraceId.TableDesigner);

            // SQL built-in types
            this.AddTypesToListPicker<SqlBuiltInType>(
                VMUtils.GetColumnAllowedTypes(sqlSchemaModel),
                SqlTypePickerItem.SqlTypeCategory.SqlType);

        }
        internal override bool IsValidDataType(SqlType type)
        {
            var builtInType = type as SqlBuiltInType;
            if (builtInType == null)
                return false;

            SqlDataType dataType = builtInType.SqlDataType;
            return dataType != SqlDataType.DateTimeOffset
                   && dataType != SqlDataType.Xml
                   && dataType != SqlDataType.Text
                   && dataType != SqlDataType.Image
                   && dataType != SqlDataType.Variant;
        }
    }
}