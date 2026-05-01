//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
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

        private readonly Lazy<List<NodeSmoProperty>> smoPropertiesLazy = new Lazy<List<NodeSmoProperty>>(() => new List<NodeSmoProperty>
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
            },
            new NodeSmoProperty
            {
                Name = "IsDroppedLedgerColumn",
                ValidFor = ValidForFlag.Sql2022|ValidForFlag.AzureV12
            }
        });

        public override IEnumerable<NodeSmoProperty> SmoProperties => smoPropertiesLazy.Value;
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
                Logger.Error($"Failed to get customized column name. error:{ex.Message}");
            }
            return string.Empty;
        }

        internal static string GetTypeSpecifierLabel(DataType dataType, UserDefinedDataTypeCollection uddts)
        {
            if (dataType == null)
            {
                return string.Empty;
            }

            string typeName = NormalizeDisplayTypeName(dataType.Name);
            string systemTypeName = GetUserDefinedSystemTypeName(dataType, uddts);

            return FormatTypeSpecifierLabel(
                typeName,
                systemTypeName,
                dataType.MaximumLength,
                dataType.NumericPrecision,
                dataType.NumericScale,
                null,
                null,
                null,
                dataType.VectorDimensions,
                GetOptionalPropertyString(dataType, "VectorBaseType"));
        }

        internal static string GetTypeSpecifierLabel(Column column, UserDefinedDataTypeCollection uddts)
        {
            if (column?.DataType == null)
            {
                return string.Empty;
            }

            string typeName = NormalizeDisplayTypeName(column.DataType.Name);
            string systemTypeName = NormalizeDisplayTypeName(GetOptionalPropertyString(column, "SystemType"));

            if (string.IsNullOrWhiteSpace(systemTypeName))
            {
                systemTypeName = GetUserDefinedSystemTypeName(column.DataType, uddts);
            }

            if (string.Equals(typeName, "vector", StringComparison.OrdinalIgnoreCase))
            {
                systemTypeName = typeName;
            }

            return FormatTypeSpecifierLabel(
                typeName,
                systemTypeName,
                column.DataType.MaximumLength,
                column.DataType.NumericPrecision,
                column.DataType.NumericScale,
                GetOptionalPropertyString(column, "XmlSchemaNamespaceSchema"),
                GetOptionalPropertyString(column, "XmlSchemaNamespace"),
                GetOptionalEnumValue<XmlDocumentConstraint>(column, "XmlDocumentConstraint"),
                column.DataType.VectorDimensions,
                GetOptionalPropertyString(column.DataType, "VectorBaseType") ?? GetOptionalPropertyString(column, "VectorBaseType"));
        }

        internal static string FormatTypeSpecifierLabel(
            string typeName,
            string systemTypeName,
            int? maximumLength,
            int? numericPrecision,
            int? numericScale,
            string xmlSchemaNamespaceSchema,
            string xmlSchemaNamespace,
            XmlDocumentConstraint? xmlDocumentConstraint,
            int? vectorDimensions,
            string vectorBaseType)
        {
            string normalizedTypeName = NormalizeDisplayTypeName(typeName);
            if (string.IsNullOrWhiteSpace(normalizedTypeName))
            {
                return string.Empty;
            }

            string formattedTypeName = FormatIntrinsicTypeLabel(
                normalizedTypeName,
                maximumLength,
                numericPrecision,
                numericScale,
                xmlSchemaNamespaceSchema,
                xmlSchemaNamespace,
                xmlDocumentConstraint,
                vectorDimensions,
                vectorBaseType);

            string normalizedSystemTypeName = NormalizeDisplayTypeName(systemTypeName);
            if (string.IsNullOrWhiteSpace(normalizedSystemTypeName)
                || string.Equals(normalizedTypeName, normalizedSystemTypeName, StringComparison.OrdinalIgnoreCase))
            {
                return formattedTypeName;
            }

            string formattedSystemTypeName = FormatIntrinsicTypeLabel(
                normalizedSystemTypeName,
                maximumLength,
                numericPrecision,
                numericScale,
                null,
                null,
                null,
                vectorDimensions,
                vectorBaseType);

            return $"{normalizedTypeName}({formattedSystemTypeName})";
        }

        private static string FormatIntrinsicTypeLabel(
            string typeName,
            int? maximumLength,
            int? numericPrecision,
            int? numericScale,
            string xmlSchemaNamespaceSchema,
            string xmlSchemaNamespace,
            XmlDocumentConstraint? xmlDocumentConstraint,
            int? vectorDimensions,
            string vectorBaseType)
        {
            switch (typeName.ToLowerInvariant())
            {
                case "char":
                case "nchar":
                case "varchar":
                case "nvarchar":
                case "binary":
                case "varbinary":
                    if (maximumLength.HasValue)
                    {
                        bool isMaxLengthType = (typeName == "varchar" || typeName == "nvarchar" || typeName == "varbinary")
                            && maximumLength.Value <= 0;
                        string lengthLabel = isMaxLengthType
                            ? "max"
                            : maximumLength.Value.ToString(CultureInfo.InvariantCulture);
                        return $"{typeName}({lengthLabel})";
                    }
                    break;
                case "numeric":
                case "decimal":
                    if (numericPrecision.HasValue && numericScale.HasValue)
                    {
                        return string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}({1},{2})",
                            typeName,
                            numericPrecision.Value,
                            numericScale.Value);
                    }
                    break;
                case "time":
                case "datetime2":
                case "datetimeoffset":
                    if (numericScale.HasValue)
                    {
                        return string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}({1})",
                            typeName,
                            numericScale.Value);
                    }
                    break;
                case "xml":
                    if (!string.IsNullOrWhiteSpace(xmlSchemaNamespace)
                        && !string.IsNullOrWhiteSpace(xmlSchemaNamespaceSchema)
                        && xmlDocumentConstraint.HasValue)
                    {
                        if (xmlDocumentConstraint.Value == XmlDocumentConstraint.Content)
                        {
                            return string.Format(
                                CultureInfo.InvariantCulture,
                                "XML({0}.{1})",
                                xmlSchemaNamespaceSchema,
                                xmlSchemaNamespace);
                        }

                        if (xmlDocumentConstraint.Value == XmlDocumentConstraint.Document)
                        {
                            return string.Format(
                                CultureInfo.InvariantCulture,
                                "XML(DOCUMENT {0}.{1})",
                                xmlSchemaNamespaceSchema,
                                xmlSchemaNamespace);
                        }
                    }

                    return "XML";
                case "vector":
                    if (vectorDimensions.HasValue)
                    {
                        if (!string.IsNullOrWhiteSpace(vectorBaseType))
                        {
                            return string.Format(
                                CultureInfo.InvariantCulture,
                                "vector({0},{1})",
                                vectorDimensions.Value,
                                vectorBaseType);
                        }

                        return string.Format(
                            CultureInfo.InvariantCulture,
                            "vector({0})",
                            vectorDimensions.Value);
                    }
                    break;
            }

            return typeName;
        }

        private static string GetUserDefinedSystemTypeName(DataType dataType, UserDefinedDataTypeCollection uddts)
        {
            if (dataType?.SqlDataType != SqlDataType.UserDefinedDataType || uddts == null)
            {
                return string.Empty;
            }

            string normalizedTypeName = NormalizeDisplayTypeName(dataType.Name);
            foreach (UserDefinedDataType item in uddts)
            {
                string normalizedItemName = NormalizeDisplayTypeName(item.Name);
                if (string.Equals(normalizedTypeName, normalizedItemName, StringComparison.OrdinalIgnoreCase)
                    || normalizedTypeName.EndsWith($".{normalizedItemName}", StringComparison.OrdinalIgnoreCase))
                {
                    return NormalizeDisplayTypeName(item.SystemType);
                }
            }

            return string.Empty;
        }

        private static string NormalizeDisplayTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return string.Empty;
            }

            if (typeName.IndexOf('[') < 0)
            {
                return typeName;
            }

            List<string> nameParts = new List<string>();
            int searchIndex = 0;
            while (searchIndex < typeName.Length)
            {
                int start = typeName.IndexOf('[', searchIndex);
                if (start < 0)
                {
                    break;
                }

                int end = typeName.IndexOf(']', start + 1);
                if (end < 0)
                {
                    break;
                }

                nameParts.Add(typeName.Substring(start + 1, end - start - 1));
                searchIndex = end + 1;
            }

            return nameParts.Count > 0
                ? string.Join(".", nameParts)
                : typeName.Replace("[", string.Empty)
                    .Replace("]", string.Empty);
        }

        private static string GetOptionalPropertyString(object target, string propertyName)
        {
            if (target == null)
            {
                return null;
            }

            var property = target.GetType().GetProperty(propertyName);
            object value = property?.GetValue(target);
            return value == null
                ? null
                : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static T? GetOptionalEnumValue<T>(object target, string propertyName) where T : struct
        {
            if (target == null)
            {
                return null;
            }

            var property = target.GetType().GetProperty(propertyName);
            object value = property?.GetValue(target);
            if (value == null)
            {
                return null;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            string stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (Enum.TryParse(stringValue, out T parsedValue))
            {
                return parsedValue;
            }

            return null;
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
            string columnType = GetTypeSpecifierLabel(column, uddts);
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
            string columnType = GetTypeSpecifierLabel(column, uddts);

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
            columnType = GetTypeSpecifierLabel(column, uddts);

            if (!string.IsNullOrWhiteSpace(columnType))
            {
                if (column.Parent is View)
                {
                    // View columns are always computed, but Object Explorer shows them as regular columns, so
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
