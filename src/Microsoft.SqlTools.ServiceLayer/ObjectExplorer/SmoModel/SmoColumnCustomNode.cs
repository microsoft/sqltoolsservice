//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Custom name for Columns
    /// </summary>
    internal partial class ColumnsChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return SmoColumnCustomNodeHelper.CalculateCustomLabel(smoObject, smoContext);
        }

        public override IEnumerable<NodeSmoProperty> SmoProperties
        {
            get
            {
                return new List<NodeSmoProperty>
                {
                    new NodeSmoProperty
                    {
                        Name = "Computed",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "IsColumnSet",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "Nullable",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "DataType",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "InPrimaryKey",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "IsForeignKey",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "SystemType",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "Length",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "NumericPrecision",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "NumericScale",
                        ValidFor = ValidForFlag.All
                    },
                    new NodeSmoProperty
                    {
                        Name = "XmlSchemaNamespaceSchema",
                        ValidFor = ValidForFlag.NotSqlDw
                    },
                    new NodeSmoProperty
                    {
                        Name = "XmlSchemaNamespace",
                        ValidFor = ValidForFlag.NotSqlDw
                    },
                    new NodeSmoProperty
                    {
                        Name = "XmlDocumentConstraint",
                        ValidFor = ValidForFlag.NotSqlDw
                    }
                };
            }
        }
    }

    /// <summary>
    /// Custom name for UserDefinedTableTypeColumn
    /// </summary>
    internal partial class UserDefinedTableTypeColumnsChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return SmoColumnCustomNodeHelper.CalculateCustomLabel(smoObject, smoContext);
        }
    }

    static class SmoColumnCustomNodeHelper
    {
        private const string SimpleColumnLabelWithType = "{0} ({1}{2}, {3})";
        private const string SimpleColumnLabelWithoutType = "{0} ({1})";
        private const string SimpleColumnLabelWithTypeAndKeyString = "{0} ({1}, {2}, {3})";

        internal static string CalculateCustomLabel(object context, SmoQueryContext smoContext)
        {
            UserDefinedDataTypeCollection uddts = null;
            if (smoContext != null)
            {
                uddts = smoContext.Database.UserDefinedDataTypes;
            }
            Column column = context as Column;
            if(column != null)
            {
                return GetCustomizedLabel(column, uddts);
            }

            return string.Empty;
        }

        private static string GetCustomizedLabel(Column column, UserDefinedDataTypeCollection uddts)
        {
            try
            {
                if (column.Computed)
                {
                    return GetComputedColumnLabel(column, uddts);
                }
                else if (column.IsColumnSet)
                {
                    return GetColumnSetLabel(column, uddts);
                }
                else
                {
                    return GetSimpleColumnLabel(column, uddts);
                }
            }
            catch(Exception ex)
            {
                Logger.Write(TraceEventType.Error, $"Failed to get customized column name. error:{ex.Message}");
            }
            return string.Empty;
        }

        private static string GetTypeSpecifierLabel(DataType dataType, UserDefinedDataTypeCollection uddts)
        {
            string typeName = string.Empty;
            if (dataType != null)
            {
                // typeSpecifier might still be in a resolve candidate status.  If so then the
                // name might be null.  Don't ask for the type specifier name in this case.
                typeName = dataType.Name;

                // This may return [dbo].[MyType], but for the purposes of display we only want MyType
                if (!string.IsNullOrWhiteSpace(typeName) &&
                    typeName.EndsWith("]", StringComparison.Ordinal))
                {
                    int nameStart = typeName.LastIndexOf('[');
                    typeName = typeName.Substring(nameStart + 1, typeName.Length - nameStart - 2);

                }

                if(dataType.SqlDataType == SqlDataType.UserDefinedDataType && uddts != null)
                {
                    foreach (UserDefinedDataType item in uddts)
                    {
                        if(item.Name == dataType.Name)
                        {
                            typeName += $"({item.SystemType})";
                            break;
                        }
                    }
                }

                // These types supports detailed information
                switch (dataType.SqlDataType)
                {
                    case SqlDataType.Char:
                    case SqlDataType.NChar:
                    case SqlDataType.Binary:
                    case SqlDataType.VarChar:
                    case SqlDataType.NVarChar:
                    case SqlDataType.VarBinary:
                        typeName += $"({dataType.MaximumLength})";
                        break;
                    case SqlDataType.Numeric:
                    case SqlDataType.Decimal:
                        typeName += $"({dataType.NumericPrecision},{dataType.NumericScale})";
                        break;
                    case SqlDataType.DateTime2:
                    case SqlDataType.Time:
                    case SqlDataType.DateTimeOffset:
                        typeName += $"({dataType.NumericScale})";
                        break;
                    case SqlDataType.VarBinaryMax:
                    case SqlDataType.NVarCharMax:
                    case SqlDataType.VarCharMax:
                        typeName += "(max)";
                        break;
                }
            }
            return typeName;
        }

        private static string GetKeyString(Column column)
        {
            // Get if it's a PK or FK (or both)
            // Here's how it could be both...notice t2c1 is both a primary and foreign key
            //
            // Create table t1 (t1c1 int, t1c2 int not null primary key)
            // Create table t2 (t2c1 int primary key, t2c2 int not null)
            // Alter table t2 add FOREIGN KEY(t2c1) references t1(t1c2)
            //
            string keyString = null;
            if (column.InPrimaryKey)
                keyString = "PK";
            if (column.IsForeignKey)
            {
                keyString = (keyString == null) ? "FK" :
                                                  "PK, FK";
            }

            return keyString;
        }

        private static string GetColumnSetLabel(Column column, UserDefinedDataTypeCollection uddts)
        {
            // This is the simple name
            string label = column.Name;

            // Get the column type
            string columnType = GetTypeSpecifierLabel(column.DataType, uddts);
            string keyString = GetKeyString(column);

            if (keyString != null && !string.IsNullOrWhiteSpace(columnType))
            {
                return string.Format(CultureInfo.InvariantCulture,
                                             SR.SchemaHierarchy_ColumnSetLabelWithTypeAndKeyString,
                                             label,
                                             keyString,
                                             columnType,
                                             SR.SchemaHierarchy_NullColumn_Label);
            }

            if (!string.IsNullOrWhiteSpace(columnType))
            {
                return string.Format(CultureInfo.InvariantCulture,
                                             SR.SchemaHierarchy_ColumnSetLabelWithType,
                                             label,
                                             keyString,
                                             columnType,
                                             SR.SchemaHierarchy_NullColumn_Label);
            }

            return string.Format(CultureInfo.InvariantCulture,
                                         SR.SchemaHierarchy_ColumnSetLabelWithoutType,
                                         label,
                                         SR.SchemaHierarchy_NullColumn_Label);
        }

        private static string GetSimpleColumnLabel(Column column, UserDefinedDataTypeCollection uddts)
        {
            // This is the simple name
            string label = column.Name;

            // Get the nullability
            string isNullable = column.Nullable ? SR.SchemaHierarchy_NullColumn_Label : SR.SchemaHierarchy_NotNullColumn_Label;

            // Get the column type
            string columnType = GetTypeSpecifierLabel(column.DataType, uddts);

            string keyString = GetKeyString(column);

            if (keyString != null && !string.IsNullOrWhiteSpace(columnType))
            {
                return string.Format(CultureInfo.InvariantCulture,
                                             SimpleColumnLabelWithTypeAndKeyString,
                                             label,
                                             keyString,
                                             columnType,
                                             isNullable);
            }

            if (!string.IsNullOrWhiteSpace(columnType))
            {
                return string.Format(CultureInfo.InvariantCulture,
                                             SimpleColumnLabelWithType,
                                             label,
                                             keyString,
                                             columnType,
                                             isNullable);
            }

            return string.Format(CultureInfo.InvariantCulture,
                                         SimpleColumnLabelWithoutType,
                                         label,
                                         isNullable);
        }

        private static string GetComputedColumnLabel(Column column, UserDefinedDataTypeCollection uddts)
        {
            string columnType = null;

            // Display the type name as fully qualified
            string label = column.Name;

            // Get the nullability
            string isNullable = column.Nullable ? SR.SchemaHierarchy_NullColumn_Label : SR.SchemaHierarchy_NotNullColumn_Label;

            string keyString = GetKeyString(column);

            // Get the column type
            columnType = GetTypeSpecifierLabel(column.DataType, uddts);

            if (!string.IsNullOrWhiteSpace(columnType))
            {
                if (column.Parent is View)
                {
                    // View columns are always computed, but SSMS shows then as never computed, so
                    // treat them as simple columns
                    return string.Format(CultureInfo.InvariantCulture,
                                            SimpleColumnLabelWithType,
                                            label,
                                            keyString,
                                            columnType,
                                            isNullable);
                }
                return string.Format(CultureInfo.InvariantCulture,
                                        SR.SchemaHierarchy_ComputedColumnLabelWithType,
                                        label,
                                        keyString,
                                        columnType,
                                        isNullable);
            }

            if (column.Parent is View)
            {
                return string.Format(CultureInfo.InvariantCulture,
                                    SimpleColumnLabelWithoutType,
                                    label,
                                    keyString);
            }
            return string.Format(CultureInfo.InvariantCulture,
                                SR.SchemaHierarchy_ComputedColumnLabelWithoutType,
                                label,
                                keyString);

        }
    }
}
