//------------------------------------------------------------------------------
// <copyright file="ISqlModelUpdatingService.Column.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// Column related operations
    /// </summary>
	internal partial interface ISqlModelUpdatingService
	{
        void InsertSimpleColumnAtLastIndex(SqlTable table, string columnName);
        void InsertSimpleColumnAt(SqlTable table, string columnName, int index);
        void InsertSimpleColumnBuiltInDataTypeAt(SqlTable table, string columnName, int index, string typeSpec, bool nullable);
        void InsertComputedColumnAt(SqlTable table, string columnName, int index, string expression);

        void DeleteColumn(SqlColumn column);
        void DeleteColumns(IEnumerable<SqlColumn> column);

        void SetColumnNullable(SqlSimpleColumn column, bool isNullable);

        void SetColumnIsIdentity(SqlSimpleColumn column, bool isIdentity);
        void SetColumnIdentitySeed(SqlSimpleColumn column, SqlDecimal seed);
        void SetColumnIdentityIncrement(SqlSimpleColumn column, SqlDecimal increment);

        void SetColumnDataType(SqlSimpleColumn column, SqlType newType);
        void SetColumnBuiltInDataType(SqlSimpleColumn column, string typeSpec);
        void SetColumnDataTypeLength(SqlSimpleColumn column, int length, bool isMax);
        void SetColumnDataTypePrecision(SqlSimpleColumn column, int precision);
        void SetColumnDataTypeScale(SqlSimpleColumn column, int scale);
        void SetXmlColumnStyle(SqlSimpleColumn column, bool isXmlDocument);
        void SetXmlColumnXmlSchema(SqlSimpleColumn column, SqlXmlSchemaCollection xmlSchema);

        void SetColumnDefaultValue(SqlSimpleColumn column, string expression);
        void SetSequenceToDefaultValue(SqlSimpleColumn column, SqlSequence sequence);
        void SetNewSequenceAndAssociateToDefaultConstraint(SqlTable table, SqlSimpleColumn column, string name);

        void SetComputedColumnExpression(SqlComputedColumn column, string expression);
        void SetComputedColumnIsPersisted(SqlComputedColumn column, bool isPersisted);
        void SetComputedColumnIsPersistedNullable(SqlComputedColumn column, bool isPersistedNullable);

        bool IsTypeSpecValid(SqlSimpleColumn column, string typeSpec);

	}
}
