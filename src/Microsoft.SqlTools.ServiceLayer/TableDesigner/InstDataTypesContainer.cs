//------------------------------------------------------------------------------
// <copyright file="InstDataTypesContainer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Data.Tools.Design.Core.Collections;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;

namespace Microsoft.Data.Relational.Design.Table
{
    /// <summary>
    /// InstDataTypesContainer provides function of sql data types
    /// </summary>
    internal class InstDataTypesContainer
    {
        private SortedObservableCollection<SqlTypePickerItem> _instDataTypes = new SortedObservableCollection<SqlTypePickerItem>(new SqlTypesComparer());
        private bool _isModelCollationCaseSensitive;
        private bool _isPopulateWithDefaultValue;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal InstDataTypesContainer(SqlSchemaModel sqlSchemaModel, bool withDefaultValue)
        {
            _isModelCollationCaseSensitive = sqlSchemaModel.Collation.CaseSensitive;
            _isPopulateWithDefaultValue = withDefaultValue;
            this.InitDataTypes(sqlSchemaModel);
        }

        internal ObservableCollection<SqlTypePickerItem> InstDataTypes
        {
            get
            {
                return this._instDataTypes;
            }
        }

        internal virtual bool IsValidDataType(SqlType type)
        {
            if (type == null)
            {
                return false;
            }

            if (type is SqlTableType)
            {
                return false;
            }

            if (type.IsBuiltIn())
            {
                return true;
            }

            if (!type.IsExternal() ||
                DataSchemaModel.IsElementFromCompositeReference(type))
            {
                return true;
            }

            return false;
        }

        internal SqlType GetRepresentedDataType(string dataTypeDisplayName)
        {
            StringComparison compType = _isModelCollationCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (SqlTypePickerItem sqlTypePickerItem in _instDataTypes)
            {
                if (sqlTypePickerItem.SqlTypeDisplay.RepresentName.Equals(dataTypeDisplayName, compType))
                {
                    return sqlTypePickerItem.SqlType;
                }
            }

            return null;
        }

        internal int IndexOfTypeRepresentName(string dataTypeSimpleDisplayName)
        {
            StringComparison compType = _isModelCollationCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (SqlTypePickerItem sqlTypePickerItem in _instDataTypes)
            {
                if (sqlTypePickerItem.SqlTypeDisplay.RepresentName.Equals(dataTypeSimpleDisplayName, compType))
                {
                    return _instDataTypes.IndexOf(sqlTypePickerItem);
                }
            }

            return -1;
        }

        internal void DeleteInstType(SqlType sqlType)
        {
            SqlExceptionUtils.ValidateNullParameter(sqlType, "sqlType", SqlTraceId.TableDesigner);

            foreach (SqlTypePickerItem sqlTypePickerItem in _instDataTypes.ToList())
            {
                if (sqlTypePickerItem.SqlType == sqlType)
                {
                    _instDataTypes.Remove(sqlTypePickerItem);
                }
            }
        }

        internal void AddInstType(SqlType type)
        {
            SqlExceptionUtils.ValidateNullParameter(type, "type", SqlTraceId.TableDesigner);

            if (this.ContainsSqlType(type) == false)
            {
                this.AddNewPickerItemType(
                (type is SqlUserDefinedType) ?
                SqlTypePickerItem.SqlTypeCategory.ClrType : SqlTypePickerItem.SqlTypeCategory.UddtType,
                type);
            }
        }

        protected virtual void InitDataTypes(SqlSchemaModel sqlSchemaModel)
        {
            SqlExceptionUtils.ValidateNullParameter(sqlSchemaModel, "sqlSchemaModel", SqlTraceId.TableDesigner);

            // SQL built-in types
            this.AddTypesToListPicker<SqlBuiltInType>(
                VMUtils.GetColumnAllowedTypes(sqlSchemaModel),
                SqlTypePickerItem.SqlTypeCategory.SqlType);

            // CLR types
            this.AddTypesToListPicker<SqlUserDefinedType>(
                sqlSchemaModel.GetElements<SqlUserDefinedType>(ModelElementQueryFilter.All),
                SqlTypePickerItem.SqlTypeCategory.ClrType);

            // UDT types
            this.AddTypesToListPicker<SqlUserDefinedDataType>(
                sqlSchemaModel.GetElements<SqlUserDefinedDataType>(ModelElementQueryFilter.All),
                SqlTypePickerItem.SqlTypeCategory.UddtType);
        }

        protected void AddTypesToListPicker<T>(IEnumerable<T> typesList, SqlTypePickerItem.SqlTypeCategory cat) where T : SqlType
        {
            if (typesList != null && typesList.FirstOrDefault() != null)
            {
                foreach (T type in typesList)
                {
                    if (IsValidDataType(type))
                    {
                        this.AddNewPickerItemType(cat, type);
                    }
                }
            }
        }

        private void AddNewPickerItemType(SqlTypePickerItem.SqlTypeCategory cat, SqlType sqlType)
        {
            if (_isPopulateWithDefaultValue)
            {
                foreach (SqlTypeDisplayBase sqlTypeDisplayFactory in GetResolvedSqlTypeDisplays(sqlType))
                {
                    SqlTypePickerItem sqlTypePickerItem = SqlTypePickerItem.Create(sqlType, cat, sqlTypeDisplayFactory);
                    _instDataTypes.Add(sqlTypePickerItem);
                }
            }
            else
            {
                SqlTypePickerItem sqlTypePickerItem = SqlTypePickerItem.Create(sqlType, cat, new SqlTypeDisplayDefault(sqlType));
                _instDataTypes.Add(sqlTypePickerItem);
            }
        }

        protected virtual IList<SqlTypeDisplayBase> GetResolvedSqlTypeDisplays(SqlType sqlType)
        {
            return SqlTypeDisplayFactory.GetResolvedSqlTypeDisplays(sqlType);
        }

        private bool ContainsSqlType(SqlType sqlType)
        {
            if (sqlType == null)
            {
                return false;
            }

            foreach (SqlTypePickerItem sqlTypePickerItem in _instDataTypes)
            {
                if (sqlTypePickerItem.SqlType == sqlType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
