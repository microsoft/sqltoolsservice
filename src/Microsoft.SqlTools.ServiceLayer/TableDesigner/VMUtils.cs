//------------------------------------------------------------------------------
// <copyright file="VMUtils.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Data.Tools.Design.Core.Context;
using Microsoft.Data.Tools.Design.Core.Controls;
using Microsoft.Data.Tools.Schema;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.ScriptDom.Sql;
using Microsoft.Data.Tools.Schema.Sql;
using Microsoft.Data.Tools.Schema.Sql.DesignServices;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using CodeGenerationSupporter = Microsoft.Data.Tools.Schema.ScriptDom.Sql.CodeGenerationSupporter;

namespace Microsoft.Data.Relational.Design.Table
{
    /// <summary>
    /// Extension methods wrapper for model extension classes
    /// </summary>
    internal static class SqlModelExtension
    {
        internal static string GetName(this IModelElement sqlElem, ElementNameStyle nameStyle = ElementNameStyle.SimpleName)
        {
            if (sqlElem != null && sqlElem.Model != null)
            {
                if (sqlElem.Name == null)
                {
                    SqlFullTextIndex fullTextIndex = sqlElem as SqlFullTextIndex;
                    if (fullTextIndex != null)
                    {
                        // display SSMS-style name for fulltext index
                        return "FullText_for_" + fullTextIndex.IndexedObject.GetName();
                    }

                    return SqlCommonResource.Unnamed;
                }
                else
                {
                    DatabaseSchemaProvider dsp = ((DataSchemaModel)sqlElem.Model).DatabaseSchemaProvider;
                    UserInteractionServices services = dsp.UserInteractionServices;

                    return services.GetElementName(sqlElem, nameStyle);
                }
            }

            return null;
        }

        internal static string GetTypeDescription(this ISqlModelElement modelElement)
        {
            if (modelElement != null && modelElement.Model != null)
            {
                DatabaseSchemaProvider dsp = ((DataSchemaModel)modelElement.Model).DatabaseSchemaProvider;
                UserInteractionServices services = dsp.UserInteractionServices;

                return services.GetElementTypeDescription(modelElement.ElementClass);
            }

            return null;
        }

        internal static string GetDescription(this ISqlExtendedPropertyHost propertyHost)
        {
            SqlExtendedProperty description = VMUtils.GetDescriptionModelElementForDisplay(propertyHost);

            if (description != null && description.Value != null)
            {
                StringLiteral stringLiteral = description.Value.ScriptDom as StringLiteral;
                if (stringLiteral != null)
                {
                    // if value is a string literal, display the value without quotes
                    return stringLiteral.Value;
                }
                else
                {
                    return description.Value.Script;
                }
            }

            return null;
        }

        internal static void SetDescription(this ISqlExtendedPropertyHost propertyHost, ISqlModelUpdatingService changeService, string description)
        {
            changeService.SetDescriptionForElement(propertyHost, description);
        }

        /// <summary>
        /// Get display name for a given ModelIdentifier
        /// </summary>
        /// <param name="sqlModelIdentifier">ModelIdentifier to display</param>
        /// <returns>Display name queried from UserInteractionServices</returns>
        internal static string GetDisplayName(this ModelIdentifier sqlModelIdentifier, EscapeStyle escapeStyle = EscapeStyle.DontEscape, bool fullName = false)
        {
            if (sqlModelIdentifier != null && sqlModelIdentifier.Model != null)
            {
                DatabaseSchemaProvider dsp = ((DataSchemaModel)sqlModelIdentifier.Model).DatabaseSchemaProvider;
                UserInteractionServices services = dsp.UserInteractionServices;

                return services.GetDisplayName(sqlModelIdentifier, escapeStyle, fullName);
            }

            return null;
        }

        /// <summary>
        /// Whether Name metadata for the given model element is existed or not
        /// </summary>
        /// <param name="sqlElement">The given model element</param>
        /// <returns>True if Name metadata is set. Otherwise false</returns>
        internal static bool HasName(this IModelElement sqlElement)
        {
            return (sqlElement != null) && (sqlElement.Name != null);
        }
    }

    internal static class VMUtils
    {
        internal static class ContextViewInlineInfoGenerator
        {
            private const string openParenthesis = "(";
            private const string closeParenthesis = ")";
            private const string comma = ",";
            private const string colon = ":";
            private const string semiColon = ";";
            private const string whiteSpace = " ";

            internal static string GetInlineInfoForModelElement(ISqlModelElement element)
            {
                //We will only double-cast once and I think it is fine for the sake of clarity of the code. (yicecen)
                if (element is SqlPrimaryKeyConstraint)
                {
                    return GetInlineInfoForPrimaryKey(element as SqlPrimaryKeyConstraint);
                }
                else if(element is SqlUniqueConstraint)
                {
                    return GetInlineInfoForUniqueConstraint(element as SqlUniqueConstraint);
                }
                else if (element is SqlCheckConstraint)
                {
                    return GetInlineInfoForCheckConstraint(element as SqlCheckConstraint);
                }
                else if (element is SqlIndex)
                {
                    return GetInlineInfoForRegularIndex(element as SqlIndex);
                }
                else if (element is SqlSpatialIndex)
                {
                    return GetInlineInfoForSpatialIndex(element as SqlSpatialIndex);
                }
                else if (element is SqlColumnStoreIndex)
                {
                    return GetInlineInfoForColumnStoreIndex(element as SqlColumnStoreIndex);
                }
                else if (element is SqlXmlIndex)
                {
                    return GetInlineInfoForXMLIndex(element as SqlXmlIndex);
                }
                else if (element is SqlSelectiveXmlIndex)
                {
                    return GetInlineInfoForSelectiveXMLIndex(element as SqlSelectiveXmlIndex);
                }
                else if (element is SqlForeignKeyConstraint)
                {
                    return GetInlineInfoForForeignKey(element as SqlForeignKeyConstraint); ;
                }
                else if (element is SqlDmlTrigger)
                {
                    return GetInlineInfoForDmlTrigger(element as SqlDmlTrigger);
                }

                return string.Empty;
            }

            private static string GetInlineInfoForDmlTrigger(SqlDmlTrigger sqlDmlTrigger)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                bool isThereTokenBefore = false;

                if (sqlDmlTrigger.SqlTriggerType == SqlTriggerType.InsteadOf)
                {
                    sb.Append(SqlTriggerType.InsteadOf.ToString());
                    isThereTokenBefore = true;
                }

                if (sqlDmlTrigger.IsInsertTrigger)
                {
                    if (isThereTokenBefore)
                    {
                        sb.Append(comma);
                        sb.Append(whiteSpace);
                    }

                    sb.Append(TSqlTokenType.Insert.ToString());
                    isThereTokenBefore = true;
                }

                if (sqlDmlTrigger.IsUpdateTrigger)
                {
                    if (isThereTokenBefore)
                    {
                        sb.Append(comma);
                        sb.Append(whiteSpace);
                    }

                    sb.Append(TSqlTokenType.Update.ToString());
                    isThereTokenBefore = true;
                }

                if (sqlDmlTrigger.IsDeleteTrigger)
                {
                    if (isThereTokenBefore)
                    {
                        sb.Append(comma);
                        sb.Append(whiteSpace);
                    }

                    sb.Append(TSqlTokenType.Delete.ToString());
                }

                sb.Append(closeParenthesis);

                return sb.ToString();
            }


            private static string GetInlineInfoForPrimaryKey(SqlPrimaryKeyConstraint primaryKey)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                sb.Append(primaryKey.GetTypeDescription());
                if (primaryKey.IsClustered)
                {
                    sb.Append(comma);
                    sb.Append(whiteSpace);
                    sb.Append(TSqlTokenType.Clustered.ToString());
                }

                sb.Append(colon);
                sb.Append(whiteSpace);
                
                AppendInlineColumnsListForIndexOrKey(sb, primaryKey);

                sb.Append(closeParenthesis);

                return sb.ToString();               
            }

            private static string GetInlineInfoForUniqueConstraint(SqlUniqueConstraint uniqueConstraint)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                if (uniqueConstraint.IsClustered)
                {
                    sb.Append(TSqlTokenType.Clustered.ToString());
                    sb.Append(colon);
                    sb.Append(whiteSpace);
                }

                AppendInlineColumnsListForIndexOrKey(sb, uniqueConstraint);

                sb.Append(closeParenthesis);

                return sb.ToString();
            }

            private static string GetInlineInfoForRegularIndex(SqlIndex sqlRegularIndex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                bool uniqueOrClustered = false;
                if (sqlRegularIndex.IsUnique)
                {
                    sb.Append(TSqlTokenType.Unique.ToString());
                    if (sqlRegularIndex.IsClustered)
                    {
                        sb.Append(comma);
                        sb.Append(whiteSpace);
                    }
                    uniqueOrClustered = true;
                }
                if (sqlRegularIndex.IsClustered)
                {
                    sb.Append(TSqlTokenType.Clustered.ToString());
                    uniqueOrClustered = true;
                }

                if (uniqueOrClustered)
                {
                    sb.Append(colon);
                    sb.Append(whiteSpace);
                }

                AppendInlineColumnsListForIndexOrKey(sb, sqlRegularIndex);

                sb.Append(closeParenthesis);

                return sb.ToString();
            }

            private static string GetInlineInfoForSpatialIndex(SqlSpatialIndex spatialIndex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                AppendSingleColumnName(sb, spatialIndex, SqlSpatialIndex.ColumnClass);

                sb.Append(closeParenthesis);

                return sb.ToString();
            }

            private static string GetInlineInfoForColumnStoreIndex(SqlColumnStoreIndex columnStoreIndex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                AppendCommaSeparatedColumnNameList(sb, columnStoreIndex.ColumnSpecifications);

                sb.Append(closeParenthesis);

                return sb.ToString();
            }

            private static string GetInlineInfoForXMLIndex(SqlXmlIndex xmlIndex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                if (xmlIndex.IsPrimary)
                {
                    sb.Append(ViewModelResources.PrimaryXMLIndexToken);
                    sb.Append(whiteSpace);
                    sb.Append(xmlIndex.GetTypeDescription());
                }
                else
                {
                    sb.Append(ViewModelResources.SecondaryXMLIndexToken);
                    sb.Append(whiteSpace);
                    sb.Append(xmlIndex.GetTypeDescription());

                    if (xmlIndex.PrimaryXmlIndexUsage != SqlPrimaryXmlIndexUsage.Unknown)
                    {
                        sb.Append(comma);
                        sb.Append(whiteSpace);
                        sb.Append(xmlIndex.PrimaryXmlIndexUsage.ToString());
                    }
                }

                sb.Append(colon);
                sb.Append(whiteSpace);

                AppendSingleColumnName(sb, xmlIndex, SqlXmlIndex.ColumnClass);

                sb.Append(closeParenthesis);

                return sb.ToString();
            }

            private static string GetInlineInfoForSelectiveXMLIndex(SqlSelectiveXmlIndex index)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                if (index.IsPrimary)
                {
                    sb.Append(ViewModelResources.PrimaryXMLIndexToken);
                    sb.Append(whiteSpace);
                    sb.Append(index.GetTypeDescription());
                }
                else
                {
                    sb.Append(ViewModelResources.SecondaryXMLIndexToken);
                    sb.Append(whiteSpace);
                    sb.Append(index.GetTypeDescription());

                    if (index.ForPrimaryPromotedPath != null)
                    {
                        sb.Append(comma);
                        sb.Append(whiteSpace);
                        sb.Append(index.ForPrimaryPromotedPath.Name.GetDisplayName());
                    }
                }

                sb.Append(colon);
                sb.Append(whiteSpace);

                AppendSingleColumnName(sb, index, SqlSelectiveXmlIndex.ColumnClass);

                sb.Append(closeParenthesis);

                return sb.ToString();
            }

            private static string GetInlineInfoForForeignKey(SqlForeignKeyConstraint foreignKey)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                if (foreignKey.Name == null)
                {
                    sb.Append(VMUtils.GetReferencedElementIdentifierDisplayNameForSingleRelationship(foreignKey, SqlForeignKeyConstraint.ForeignTableClass));
                    sb.Append(colon);
                    sb.Append(whiteSpace);
                }

                sb.Append(VMUtils.GetReferencedElementIdentifiersStringForMultipleRelationships(foreignKey, SqlForeignKeyConstraint.ForeignColumnsClass));
                sb.Append(closeParenthesis);

                return sb.ToString();

            }

            private static string GetInlineInfoForCheckConstraint(SqlCheckConstraint constraint)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(openParenthesis);

                AppendInlineColumnsListForCheckConstraint(sb, constraint);

                sb.Append(closeParenthesis);

                return sb.ToString();
            }

            private static void AppendSingleColumnName(StringBuilder sb, ISqlModelElement modelElement, ModelRelationshipClass modelRelationShipClass)
            {
                sb.Append(VMUtils.GetReferencedElementIdentifierDisplayNameForSingleRelationship(modelElement, modelRelationShipClass));
            }

            private static void AppendInlineColumnsListForIndexOrKey(StringBuilder sb, ISqlSpecifiesIndex indexOrKey)
            {
                AppendCommaSeparatedColumnNameList(sb, indexOrKey.ColumnSpecifications);
            }

            private static void AppendInlineColumnsListForCheckConstraint(StringBuilder sb, SqlCheckConstraint checkConstraint)
            {
                if (checkConstraint.CheckExpressionDependencies == null ||
                    checkConstraint.CheckExpressionDependencies.Count == 0)
                {
                    return;
                }

                IEnumerable<ISqlModelElement> columnsInCheckConstraint = from element in checkConstraint.CheckExpressionDependencies where element is SqlColumn
                                                                  select element;
                if (columnsInCheckConstraint.Count() == 0)
                {
                    return;
                }

                bool first = true;
                foreach (ISqlModelElement element in columnsInCheckConstraint)
                {
                    if (!first)
                    {
                        sb.Append(comma);
                        sb.Append(whiteSpace);
                    }
                    first = false;
                    sb.Append(element.GetName());
                }
            }

            private static void AppendCommaSeparatedColumnNameList(StringBuilder sb, IList<SqlIndexedColumnSpecification> columnSpecifications)
            {
                if (columnSpecifications != null &&
                    columnSpecifications.Count != 0)
                {
                    bool firstElement = true;

                    foreach (SqlIndexedColumnSpecification columnSpecification in columnSpecifications)
                    {
                        if (!firstElement)
                        {
                            sb.Append(", ");
                        }
                        firstElement = false;

                        sb.Append(VMUtils.GetColNameFromColSpec(columnSpecification));
                    }
                }
            }
        }

        internal static string GetTableSchemaDisplayName(SqlTable sqlTable)
        {
            ModelIdentifier schemaIdentifier = sqlTable.Model.CreateIdentifier(sqlTable.Name.Parts[0]);
            return schemaIdentifier.GetDisplayName();
        }

        internal static bool GetIsColumnPrimary(SqlColumn sqlColumn)
        {
            bool isPk = false;

            if (sqlColumn != null)
            {
                SqlTable sqlTable = sqlColumn.Parent as SqlTable;
                if (sqlTable != null)
                {
                    foreach (SqlPrimaryKeyConstraint pkConstraint in sqlTable.Constraints.OfType<SqlPrimaryKeyConstraint>())
                    {
                        var firstColSpec =
                            (from colSpec in pkConstraint.ColumnSpecifications
                             where colSpec.Column == sqlColumn
                             select colSpec).FirstOrDefault();
                        if (firstColSpec != null)
                        {
                            isPk = true;
                            break;
                        }
                    }
                }
            }

            return isPk;
        }

        internal static bool IsElementSupported(SqlPlatforms elementSupportedPlatforms, EditingContext editingContext)
        {
            SqlExceptionUtils.ValidateNullParameter<EditingContext>(editingContext, "editingContext", SqlTraceId.TableDesigner);

            SqlTable sqlTable = editingContext.Items.GetValue<ContextItem<SqlTable>>().Object;
            if (sqlTable != null)
            {
                SqlTracer.AssertTraceEvent(sqlTable.Model != null, TraceEventType.Error, SqlTraceId.TableDesigner, "Why sqlTable.Model is null?");
                if (sqlTable.Model != null)
                {
                    SqlPlatforms currentPlatform = sqlTable.Model.Platform;
                    return (elementSupportedPlatforms & currentPlatform) == currentPlatform;
                }
            }
            
            return false;
        }

        internal static string GetDefaultValue(SqlSimpleColumn sqlColumn)
        {
            SqlDefaultConstraint defaultConstraint = GetDefaultConstraintForDisplay(sqlColumn);
            if (defaultConstraint != null)
            {
                return defaultConstraint.DefaultExpressionScript.Script;
            }

            return null;
        }

        internal static SqlExtendedProperty GetDescriptionModelElementForDisplay(ISqlExtendedPropertyHost propertyHost)
        {
            if (propertyHost != null && propertyHost.IsDeleted() == false)
            {
                IList<SqlExtendedProperty> descriptions = SqlModelUpdaterUtils.GetExtendedPropertyList(propertyHost, SqlModelUpdaterConstants.MS_Description);
                SqlTracer.AssertTraceEvent(descriptions != null, TraceEventType.Error, SqlTraceId.TableDesigner, "Why descriptions is null?");

                if (descriptions != null)
                {
                    if (descriptions.Count == 1)
                    {
                        return descriptions[0];
                    }
                }
            }

            return null;
        }

        internal static SqlDefaultConstraint GetDefaultConstraintForDisplay(SqlSimpleColumn sqlColumn)
        {
            if (sqlColumn != null && sqlColumn.IsDeleted() == false)
            {
                IEnumerable<SqlDefaultConstraint> defaultConstraints = sqlColumn.Defaults;

                // column-inline constraint may not be part of column.Defaults
                // when column has duplicates
                defaultConstraints = defaultConstraints.Union(
                    ((SqlTable)sqlColumn.Parent).Constraints.OfType<SqlDefaultConstraint>().Where(
                            dc => SqlModelUpdaterUtils.IsInlineColumnConstraint(sqlColumn, dc)));

                // only return default constraint if there is exactly one default constraint
                // associated to the column
                if (defaultConstraints.Count() == 1)
                {
                    return defaultConstraints.First();
                }
            }

            return null;
        }

        internal static SqlSequence GetSequenceInDefaultConstraintForDisplay(SqlSimpleColumn sqlColumn)
        {
            SqlDefaultConstraint defaultConstraint = GetDefaultConstraintForDisplay(sqlColumn);

            return GetSequenceInDefaultConstraintForDisplay(defaultConstraint);
        }

        internal static SqlSequence GetSequenceInDefaultConstraintForDisplay(SqlDefaultConstraint defaultConstraint)
        {
            bool foundSequenceAlready = false;
            SqlSequence sequence = null;

            if (defaultConstraint != null)
            {
                foreach (ISqlModelElement modelElement in defaultConstraint.ExpressionDependencies)
                {
                    sequence = modelElement as SqlSequence;

                    // If we have more than two sequences tied in the default constraint, treat it as an expression and do not return either of them
                    if (sequence != null && foundSequenceAlready)
                    {
                        return null;
                    }
                    else
                    {
                        foundSequenceAlready = true;
                    }
                }
            }

            return sequence;
        }

        internal static string GetSequenceDataTypeForDisplay(SqlSequence sequence)
        {
            if (sequence.TypeSpecifier == null)
            {
                return CodeGenerationSupporter.Int;
            }
            else if (sequence.TypeSpecifier.Type == null)
            {
                SqlUnResolvedTypeDisplay unresolvedType = new SqlUnResolvedTypeDisplay(sequence.TypeSpecifier);
                return unresolvedType.DisplayName;
            }
            else
            {
                return SqlModelExtension.GetDisplayName(sequence.TypeSpecifier.Type.Name);
            }
        }

        internal static string GetDataTypeDisplayName(SqlType type)
        {
            if (type != null)
            {
                return GetDataTypeDisplayName(type.Name);
            }

            return string.Empty;
        }

        internal static string GetDataTypeDisplayName(ModelIdentifier typeIdentifier)
        {
            return typeIdentifier.GetDisplayName(EscapeStyle.EscapeIfNecessary, true);
        }

        internal static string GetDataTypeDisplayName(SqlSimpleColumn sqlCol)
        {
            if (sqlCol != null)
            {
                SqlTypeSpecifierBase typeSpecifier = sqlCol.TypeSpecifier;
                if (typeSpecifier != null)
                {
                    if (typeSpecifier.Type != null)
                    {
                        return GetDataTypeDisplayName(typeSpecifier.Type);
                    }
                    else
                    {
                        // type specifier has not been resolved yet
                        IModelSingleRelationship<SqlTypeSpecifierBase, SqlType> relationToType = typeSpecifier.GetTypeRelationship();
                        if (relationToType != null)
                        {
                            SqlModelBuilderResolvableAnnotation anno =
                                relationToType.GetRelationshipEntry().GetAnnotations<SqlModelBuilderResolvableAnnotation>().FirstOrDefault();
                            if (anno != null)
                            {
                                return GetDataTypeDisplayName(anno.Name);
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        internal static SqlBuiltInType GetBuiltInType(SqlSimpleColumn col)
        {
            if (col != null && col.TypeSpecifier != null)
            {
                return col.TypeSpecifier.Type as SqlBuiltInType;
            }
            return null;
        }

        internal static void SetDataType(ISqlModelUpdatingService svc, SqlSimpleColumn sqlCol, SqlType type, string typeSpec)
        {
            if (type != null)
            {
                // user selected a type that is available in list of available types
                svc.SetColumnDataType(sqlCol, type);
            }
            else
            {
                // user entered a type specification that may include length/precision/scale
                svc.SetColumnBuiltInDataType(sqlCol, typeSpec);
            }
        }

        internal static bool CanDataTypeHaveMaxLength(SqlSimpleColumn col)
        {
            return CanSqlTypeHaveMaxLength(GetBuiltInType(col));
        }

        internal static bool CanSqlTypeHaveMaxLength(SqlBuiltInType type)
        {
            return type != null ? SqlModelUpdaterConstants.SqlTypesCanHaveMaxLength.Contains(type.SqlDataType) : false;
        }

        internal static bool CanDataTypeHaveLength(SqlSimpleColumn col)
        {
            return CanSqlTypeHaveLength(GetBuiltInType(col));
        }

        internal static bool CanSqlTypeHaveLength(SqlBuiltInType type)
        {
            return type != null ? SqlModelUpdaterConstants.SqlTypesCanHaveLength.Contains(type.SqlDataType) : false;
        }

        internal static string GetDataTypeLength(SqlSimpleColumn col)
        {
            if (CanDataTypeHaveLength(col))
            {
                SqlTypeSpecifier typeSpec = col.TypeSpecifier as SqlTypeSpecifier;
                if (typeSpec != null)
                {
                    return typeSpec.IsMax ? CodeGenerationSupporter.Max : typeSpec.Length.ToString(CultureInfo.CurrentCulture);
                }
            }
            return null;
        }

        internal static void SetDataTypeLength(ISqlModelUpdatingService svc, SqlSimpleColumn col, string length)
        {
            length = length.Trim();
            if (string.Compare(length, CodeGenerationSupporter.Max, System.StringComparison.OrdinalIgnoreCase) == 0)
            {
                svc.SetColumnDataTypeLength(col, -1, true);
            }
            else
            {
                // let int.Parse throw if cannot convert
                int len = int.Parse(length, CultureInfo.CurrentCulture);
                svc.SetColumnDataTypeLength(col, len, false);
            }
        }

        internal static bool CanDataTypeHavePrecision(SqlSimpleColumn col)
        {
            return CanSqlTypeHavePrecision(GetBuiltInType(col));
        }

        internal static bool CanSqlTypeHavePrecision(SqlBuiltInType type)
        {
            return type != null ? SqlModelUpdaterConstants.SqlTypesCanHavePrecision.Contains(type.SqlDataType) : false;
        }

        internal static int? GetDataTypePrecision(SqlSimpleColumn col)
        {
            if (CanDataTypeHavePrecision(col))
            {
                SqlTypeSpecifier typeSpec = col.TypeSpecifier as SqlTypeSpecifier;
                if (typeSpec != null)
                {
                    return typeSpec.Precision;
                }
            }
            return null;
        }

        internal static void SetDataTypePrecision(ISqlModelUpdatingService svc, SqlSimpleColumn col, int? precision)
        {
            if (precision.HasValue)
            {
                svc.SetColumnDataTypePrecision(col, precision.Value);
            }
        }

        internal static bool CanDataTypeHaveScale(SqlSimpleColumn col)
        {
            return CanSqlTypeHaveScale(GetBuiltInType(col));
        }

        internal static bool CanSqlTypeHaveScale(SqlBuiltInType type)
        {
            return type != null ? SqlModelUpdaterConstants.SqlTypesCanHaveScale.Contains(type.SqlDataType) : false;
        }

        internal static int? GetDataTypeScale(SqlSimpleColumn col)
        {
            if (CanDataTypeHaveScale(col))
            {
                SqlTypeSpecifier typeSpec = col.TypeSpecifier as SqlTypeSpecifier;
                if (typeSpec != null)
                {
                    return typeSpec.Scale;
                }
            }
            return null;
        }

        internal static void SetDataTypeScale(ISqlModelUpdatingService svc, SqlSimpleColumn col, int? scale)
        {
            if (scale.HasValue)
            {
                svc.SetColumnDataTypeScale(col, scale.Value);
            }
        }

        internal static decimal? GetIdentitySeed(SqlSimpleColumn col)
        {
            if (col != null && col.IsIdentity)
            {
                return col.IdentitySeed.Value;
            }
            return null;
        }

        internal static void SetIdentitySeed(ISqlModelUpdatingService svc, SqlSimpleColumn col, decimal? seed)
        {
            if (col != null && seed.HasValue)
            {
                svc.SetColumnIdentitySeed(col, SqlDecimal.Parse(seed.Value.ToString(CultureInfo.InvariantCulture)));
            }
        }

        internal static decimal? GetIdentityIncrement(SqlSimpleColumn col)
        {
            if (col != null && col.IsIdentity)
            {
                return col.IdentityIncrement.Value;
            }
            return null;
        }

        internal static void SetIdentityIncrement(ISqlModelUpdatingService svc, SqlSimpleColumn col, decimal? increment)
        {
            if (col != null && increment.HasValue)
            {
                svc.SetColumnIdentityIncrement(col, SqlDecimal.Parse(increment.Value.ToString(CultureInfo.InvariantCulture)));
            }
        }

        internal static bool CanDataTypeBeID(SqlColumn col)
        {
            if (col != null && col.TypeSpecifier != null)
            {
                SqlBuiltInType sqlBuiltIn = col.TypeSpecifier.Type as SqlBuiltInType;
                if (sqlBuiltIn != null)
                {
                    switch (sqlBuiltIn.SqlDataType)
                    {
                        case SqlDataType.BigInt:
                        case SqlDataType.Decimal:
                        case SqlDataType.Float:
                        case SqlDataType.Int:
                        case SqlDataType.Money:
                        case SqlDataType.Numeric:
                        case SqlDataType.Real:
                        case SqlDataType.SmallInt:
                        case SqlDataType.SmallMoney:
                        case SqlDataType.TinyInt:
                            return true;

                        default:
                            return false;
                    }
                }
            }

            return false;
        }

        internal static IEnumerable<SqlBuiltInType> GetColumnAllowedTypes(SqlSchemaModel model)
        {
            // exclude CURSOR and TABLE from allowed types for a column
            IEnumerable<SqlBuiltInType> types =
                        from sqlType in model.GetElements<SqlBuiltInType>(ModelElementQueryFilter.External)
                        where (sqlType.SqlDataType != SqlDataType.Cursor) && (sqlType.SqlDataType != SqlDataType.Table)
                        select sqlType;
            return types;
        }

        internal static string GetComposingElementIdentifiersNOrder(IEnumerable<ISqlModelElement> sqlModelElementEnumerator, ModelRelationshipClass modelRelationshipClass)
        {
            StringBuilder builder = new StringBuilder();
            if (sqlModelElementEnumerator != null)
            {
                int index = 0;
                foreach (ISqlModelElement sqlModelElement in sqlModelElementEnumerator)
                {
                    IList<ModelIdentifier> modelIdentifiers;
                    IList<bool> exists;

                    VMUtils.GetReferencedElementIdentifiers(
                        sqlModelElement,
                        modelRelationshipClass,
                        out modelIdentifiers,
                        out exists);

                    SqlTracer.AssertTraceEvent(modelIdentifiers != null, TraceEventType.Error, SqlTraceId.TableDesigner, "modelIdentifiers should not be null!");
                    SqlTracer.AssertTraceEvent(modelIdentifiers.Count == 1, TraceEventType.Error, SqlTraceId.TableDesigner, "Count of model identifier should be 1!");

                    if (index > 0)
                    {
                        builder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    }

                    builder.Append(modelIdentifiers[0].GetDisplayName());
                    index++;
                }
            }

            return builder.ToString();
        }

        internal static string GetColNameFromColSpec(SqlIndexedColumnSpecification colSpec)
        {
            IList<ModelIdentifier> modelIdentifiers;
            IList<bool> exists;

            VMUtils.GetReferencedElementIdentifiers(colSpec, 
               SqlIndexedColumnSpecification.ColumnClass, out modelIdentifiers, out exists);

            SqlTracer.AssertTraceEvent(modelIdentifiers != null, TraceEventType.Error, SqlTraceId.TableDesigner, "modelIdentifiers is null");
            SqlTracer.AssertTraceEvent(modelIdentifiers.Count == 1, TraceEventType.Error, SqlTraceId.TableDesigner, "modelIdentifiers.Count != 1");

            return modelIdentifiers[0].GetDisplayName();
        }

        /// <summary>
        /// Try to get the model identifiers for the element referenced by a given model element in a particular relationship
        /// </summary>
        /// <param name="referencingElement">Element at the left hand side of the relationship</param>
        /// <param name="relationshipClass">Indicates the relationship of interest</param>
        /// <param name="referencedElementIdentifierList">The model identifier of the referenced elements</param>
        /// <param name="existList">Indicates whether or not the referenced elements exist in the model</param>
        internal static void GetReferencedElementIdentifiers(
            ISqlModelElement referencingElement,
            ModelRelationshipClass relationshipClass,
            out IList<ModelIdentifier> referencedElementIdentifierList,
            out IList<bool> existList)
        {
            SqlExceptionUtils.ValidateNullParameter(referencingElement, "referencingElement", SqlTraceId.TableDesigner);
            SqlExceptionUtils.ValidateNullParameter(relationshipClass, "relationshipClass", SqlTraceId.TableDesigner);

            IModelRelationship relationship = referencingElement.GetRelationship(relationshipClass);
            IList<IModelRelationshipEntry> entries = relationship.GetReferencedRelationshipEntries();
            SqlTracer.AssertTraceEvent(entries != null, TraceEventType.Error, SqlTraceId.TableDesigner, "could not find relationship entries");

            referencedElementIdentifierList = new List<ModelIdentifier>();
            existList = new List<bool>();

            foreach (IModelRelationshipEntry entry in entries)
            {
                if (entry.Element != null)
                {
                    existList.Add(true);
                    referencedElementIdentifierList.Add(entry.Element.Name);
                }
                else
                {
                    existList.Add(false);
                    IList<SqlModelBuilderResolvableAnnotation> annos = entry.GetAnnotations<SqlModelBuilderResolvableAnnotation>();
                    SqlTracer.AssertTraceEvent(annos != null && annos.Count > 0, TraceEventType.Error, SqlTraceId.TableDesigner, "could not find resolvable annotation");
                    referencedElementIdentifierList.Add(annos[0].Name);
                }
            }
        }

        /// <summary>
        /// Create sql script generator for the given model element
        /// </summary>
        internal static SqlScriptGenerator CreateSqlScriptGenerator(IModelElement modelElement)
        {
            if (modelElement != null && modelElement.Model != null)
            {
                SqlDatabaseSchemaProvider databaseSchemaProvider = ((DataSchemaModel)modelElement.Model).DatabaseSchemaProvider as SqlDatabaseSchemaProvider;
                SqlScriptGeneratorConstructor scriptGeneratorConstructor = databaseSchemaProvider.GetServiceConstructor<SqlScriptGeneratorConstructor>();

                // Generate T-Script for the type to get its condensed data type
                return scriptGeneratorConstructor.ConstructService();
            }

            return null;
        }

        /// <summary>
        /// Try to get full text index for the given column. If the column has full text index, 
        /// both sqlFullTextIndex and sqlFullTextIndexColumnSpecifier are not null.
        /// </summary>
        /// <param name="sqlColumn">The given column</param>
        /// <param name="sqlFullTextIndex">[out] Full text index retrieved</param>
        /// <param name="sqlFullTextIndexColumnSpecifier">[out] Full text index column specifier retrieved</param>
        internal static void GetFullTextIndex(
            SqlColumn sqlColumn,
            out SqlFullTextIndex sqlFullTextIndex,
            out SqlFullTextIndexColumnSpecifier sqlFullTextIndexColumnSpecifier)
        {
            SqlExceptionUtils.ValidateNullParameter(sqlColumn, "sqlColumn", SqlTraceId.TableDesigner);
            sqlFullTextIndex = null;
            sqlFullTextIndexColumnSpecifier = null;
            SqlTable sqlTable = sqlColumn.Parent as SqlTable;

            // Traverse list of full text indexes for its parent and try to find a full text index which refers to the given column
            if (sqlTable != null && sqlTable.FullTextIndex != null)
            {
                foreach (SqlFullTextIndex sqlfti in sqlTable.FullTextIndex)
                {
                    foreach (SqlFullTextIndexColumnSpecifier ftiColumnSpecifier in sqlfti.Columns)
                    {
                        if (ftiColumnSpecifier.Column == sqlColumn)
                        {
                            sqlFullTextIndex = sqlfti;
                            sqlFullTextIndexColumnSpecifier = ftiColumnSpecifier;
                            break;
                        }
                    }
                }
            }
        }

        internal static string GetReferencedElementIdentifierDisplayNameForSingleRelationship(ISqlModelElement referencingElement, ModelRelationshipClass relationshipClass, bool fullName = false)
        {
            ModelIdentifier modelIdentifier = GetReferencedElementIdentifierForSingleRelationship(referencingElement, relationshipClass);
            return modelIdentifier != null ? modelIdentifier.GetDisplayName(EscapeStyle.DontEscape, fullName) : null;
        }

        internal static ModelIdentifier GetReferencedElementIdentifierForSingleRelationship(ISqlModelElement referencingElement, ModelRelationshipClass relationshipClass)
        {
            IList<ModelIdentifier> modelIdentifiers;
            IList<bool> exists;

            GetReferencedElementIdentifiers(referencingElement, relationshipClass, out modelIdentifiers, out exists);

            if (modelIdentifiers != null && modelIdentifiers.Count == 1)
            {
                return modelIdentifiers[0];
            }

            return null;
        }

        internal static string GetReferencedElementIdentifiersStringForMultipleRelationships(ISqlModelElement referencingElement, ModelRelationshipClass relationshipClass)
        {
            IList<ModelIdentifier> modelIdentifiers;
            IList<bool> exists;

            GetReferencedElementIdentifiers(referencingElement, relationshipClass, out modelIdentifiers, out exists);

            if (modelIdentifiers != null && modelIdentifiers.Count > 0)
            {
                int index = 0;
                StringBuilder stringBuilder = new StringBuilder();

                foreach (ModelIdentifier modelIdentifier in modelIdentifiers)
                {
                    if (index != 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.Append(modelIdentifier.GetDisplayName());
                    index++;
                }

                return stringBuilder.ToString();
            }

            return null;
        }

        internal static IEnumerable<TSelectedObj> GetObjectsFromSelection<TSelectedObj>(object selection) where TSelectedObj : class
        {
            // single selection
            TSelectedObj selectedObj = selection as TSelectedObj;
            if (selectedObj != null)
            {
                yield return selectedObj;
            }
            else
            {
                // multi selection
                object[] selectedItems = selection as object[];
                SqlTracer.AssertTraceEvent(selectedItems != null, TraceEventType.Warning, SqlTraceId.TableDesigner, "could not get selection entries");
                if (selectedItems != null)
                {
                    foreach (object item in selectedItems)
                    {
                        selectedObj = item as TSelectedObj;
                        SqlTracer.AssertTraceEvent(selectedObj != null, TraceEventType.Warning, SqlTraceId.TableDesigner, "could not get selected object");
                        if (selectedObj != null)
                        {
                            yield return selectedObj;
                        }
                    }
                }
            }
        }

        internal delegate void ModelUpdateOperation(ISqlModelUpdatingService svc);

        internal static PerformEditResult PerformEdit(EditingContext ctx, ModelUpdateOperation operation, bool refreshState = true)
        {
            SqlExceptionUtils.ValidateNullParameter<EditingContext>(ctx, "editingContext", SqlTraceId.TableDesigner);
            SqlExceptionUtils.ValidateNullParameter<ModelUpdateOperation>(operation, "operation", SqlTraceId.TableDesigner);

            // IDesignerHostService hostSvc = ctx.Services.GetService<IDesignerHostService>();

            ISqlModelUpdatingService svc = ctx.Items.GetValue<ContextItem<ISqlModelUpdatingService>>().Object;
            SqlTracer.AssertTraceEvent(svc != null, TraceEventType.Error, SqlTraceId.TableDesigner, "could not get ISqlModelUpdatingService");

            if (svc != null)
            {
                // the updating service will call back on us to notify all the script documents 
                // that are being changed by the current edit operation
                EventHandler<BeforeModelUpdateEventArgs> beforeUpdatingScripts = (sender, args) =>
                {
                    // if (hostSvc != null)
                    // {
                    //     // get list of scripts that will be changed as part of the current edit operation
                    //     foreach (string scriptMoniker in args.Updates.Select(u => u.ScriptCacheIdentifier).Distinct())
                    //     {
                    //         // make sure that all scripts that need to be changed have
                    //         // backing buffers, and that those scripts are tracked for navigation
                    //         // in the script pane
                    //         hostSvc.AddEntryToScriptWindowPanelCache(ctx, scriptMoniker);
                    //     }

                    //     bool isChangingAnyScript =
                    //         args.Updates.Select(u => u.Updates.FirstOrDefault()).Where(item => item != null).Any();
                    //     if (isChangingAnyScript)
                    //     {
                    //         // make sure that even edits that occur in secondary scrips 
                    //         // also show up in the undo stack of the main document
                    //         // that is opened in the designer
                    //         hostSvc.OnDesignerEdit(ctx);
                    //     }
                    // }
                };
                svc.BeforeUpdatingScripts += beforeUpdatingScripts;

                // the updating service will call back to request the list
                // of scripts that are associated with the table being edited in the designer
                EventHandler<BeforeResolveChangesEventArgs> beforeResolveChanges = (sender, args) =>
                {
                    // if (hostSvc != null)
                    // {
                    //     args.AdditionalFilesToProcess = hostSvc.GetTableRelatedDocuments(ctx);
                    // }
                };
                svc.BeforeResolveChanges += beforeResolveChanges;

                try
                {
                    operation(svc);

                    // if (refreshState && hostSvc != null)
                    // {
                    //     // needed for operations performed via script updater only (i.e.
                    //     // operations that don't have the full model updater implementation)
                    //     hostSvc.RefreshTableDesignerState(ctx);
                    // }

                    return PerformEditResult.Success;
                }
                catch (CannotEditFilesException)
                {
                    return PerformEditResult.FailAbort;
                }
                catch (Exception ex)
                {
                    if (SqlExceptionUtils.IsIrrecoverableException(ex))
                    {
                        throw;
                    }
                    SqlTracer.TraceException(SqlTraceId.TableDesigner, ex);
                }
                finally
                {
                    svc.BeforeUpdatingScripts -= beforeUpdatingScripts;
                    svc.BeforeResolveChanges -= beforeResolveChanges;
                }
            }

            return PerformEditResult.FailRetry;
        }

        
        internal static string GetDefaultFileGroupDisplayName(ModelStore model)
        {
            SqlTracer.AssertTraceEvent(model != null, TraceEventType.Critical, SqlTraceId.TableDesigner, "model is null");

            if (model != null)
            {
                SqlDatabaseOptions sqlDatabaseOptions = GetDefaultDatabaseOptions(model);

                if (sqlDatabaseOptions != null && sqlDatabaseOptions.DefaultFilegroup != null)
                {
                    return sqlDatabaseOptions.DefaultFilegroup.GetName();
                }
            }

            return null;
        }

        internal static string GetDefaultFileStreamFileGroupDisplayName(ModelStore model)
        {
            SqlTracer.AssertTraceEvent(model != null, TraceEventType.Critical, SqlTraceId.TableDesigner, "model is null");

            if (model != null)
            {
                SqlDatabaseOptions sqlDatabaseOptions = GetDefaultDatabaseOptions(model);

                if (sqlDatabaseOptions != null && sqlDatabaseOptions.DefaultFileStreamFilegroup != null)
                {
                    return sqlDatabaseOptions.DefaultFileStreamFilegroup.GetName();
                }
            }

            return null;
        }

        private static SqlDatabaseOptions GetDefaultDatabaseOptions(ModelStore model)
        {
            IList<SqlDatabaseOptions> options = model.GetElements<SqlDatabaseOptions>(ModelElementQueryFilter.Internal);
            
            // Only 1 database settings
            if (options != null && options.Count == 1)
            {
                return options[0];
            }

            return null;
        }

        internal static string GetDefaultNameForNewSequence(VM.Column column)
        {
            IEnumerable<SqlSequence> sequences = from element in column.Table.SqlSchemaModel.GetElements<SqlSequence>(ModelElementQueryFilter.Internal)
                                                select element;

            string newSequenceName = GetNewItemName<SqlSequence>("{0}_{1}_{2}", column.Table.SqlTable, sequences, column.Name, "Sequence");

            return newSequenceName;
        }

        internal static string GetNewItemName<T>(string nameFormat, SqlTable table, IEnumerable<T> collection, params string[] additionalInfo)
            where T : ISqlModelElement
        {
            SqlSchemaModel model = (SqlSchemaModel)table.Model;

            List<object> nameParameters = new List<object>();
            nameParameters.Add(table.GetName());
            nameParameters.AddRange(additionalInfo);

            string baseName = string.Format(CultureInfo.InvariantCulture, nameFormat, nameParameters.ToArray());

            return GetNonConflictingNameForModelItem<T>(collection, model, baseName);
        }

        internal static string GetNonConflictingNameForModelItem<T>(IEnumerable<T> collection, SqlSchemaModel model, string baseName) where T : ISqlModelElement
        {
            StringBuilder sb = new StringBuilder();

            HashSet<string> existingNames = new HashSet<string>(
                from element in collection select element.GetName(),
                model.Comparer);

            for (int i = 0; i < int.MaxValue; i++)
            {
                sb.Clear();
                sb.Append(baseName);
                if (i != 0)
                {
                    sb.Append('_');
                    sb.Append(i);
                }

                string newName = sb.ToString();

                if (!existingNames.Contains(newName))
                {
                    return newName;
                }
            }

            return string.Empty;
        }

        internal static bool IsGeneratedSqlClrElement(ISqlClrClassDefined clrClassDefined)
        {
            bool generated = false;
            SqlAssembly assembly = clrClassDefined.Assembly;
            if (assembly != null)
            {
                IList<GeneratedSqlFileAnnotation> annotations = assembly.GetAnnotations<GeneratedSqlFileAnnotation>();
                SqlTracer.AssertTraceEvent(annotations.Count < 2, TraceEventType.Error, SqlTraceId.TableDesigner, "there should be no or only one GeneratedSqlFileAnnotation.");
                generated =
                    (annotations.Count > 0) &&
                    (assembly.PrimarySource != null) &&
                    (assembly.PrimarySource.SourceName != null) &&
                    (clrClassDefined.PrimarySource != null) &&
                    (assembly.PrimarySource.SourceName == clrClassDefined.PrimarySource.SourceName);
            }
            return generated;
        }
    }
}
