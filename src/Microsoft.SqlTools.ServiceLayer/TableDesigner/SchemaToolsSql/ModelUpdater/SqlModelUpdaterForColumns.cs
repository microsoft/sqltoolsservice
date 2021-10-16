//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdaterForColumns.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.DesignServices;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed partial class SqlModelUpdater
    {

        // Public Methods that Operate on SqlColumn

        public IList<SqlScriptUpdateInfo> DeleteColumn(SqlColumn column)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                // the code works with table only
                SqlTable table = (SqlTable)column.Parent;
                if (table.Columns.Count == 1)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Can't delete the last column");
                }
                updates = _scriptUpdater.DeleteColumn(column);
                NotifyModelUpdate(updates);
            
                // we don't want to change the column or table constraint before the column to be deleted
                Tuple<int, int> excludingRange = CreateExcludingRangeForDeleteColumn(table, updates[0].Updates.First().StartOffset);

                IList<ScriptHandle> handles = GetUpdatedHandles(updates);

                // delete inline constraints associated with the column
                DeleteConstraints(column);

                // let's delete the column first to avoid changing offset and length of the column to be deleted
                DeleteElement(column);

                UpdatePositionsAndScriptCache(updates, handles, excludingRange);

                // reset wildcards that depend on the table
                SqlModelResolver.ResetWildcardOnElement(table, table.Name);
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        // Public Methods that Operate on SqlSimpleColumn

        public IList<SqlScriptUpdateInfo> SetColumnDataType(SqlSimpleColumn column, SqlType newType)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");
            SqlExceptionUtils.ValidateNullParameter(newType, "newType");

            SqlBuiltInType builtin = newType as SqlBuiltInType;
            if (builtin != null && 
                (builtin.SqlDataType == SqlDataType.Cursor || builtin.SqlDataType == SqlDataType.Table))
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't change a column's data type to CURSOR or TABLE");
            }

            if (column.IsFileStream)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't change data type for a FILESTREAM column");
            }

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                SqlType oldType = column.TypeSpecifier.Type;
                if (column.GraphType == SqlColumnGraphType.None && newType != oldType)
                {
                    bool isNamelessTimestamp;
                    updates = _scriptUpdater.SetColumnDataType(column, newType, out isNamelessTimestamp);
                    NotifyModelUpdate(updates);

                    IList<ScriptHandle> handles = GetUpdatedHandles(updates);

                    AdjustNullability(column, newType);
                    ResetValidationStatus(column.TypeSpecifier);
                    UpdatePositionsAndScriptCache(updates, handles);

                    SqlTypeSpecifierBase typeSpecifierBase = column.TypeSpecifier;

                    SqlScriptUpdateItem updateItem = updates[0].Updates.First(); // we should have exact one update; if we have nothing, let it throw
                    int spaceCount = 0;
                    while (char.IsWhiteSpace(updateItem.NewText, spaceCount))
                    {
                        spaceCount++;
                    }

                    string sourceName = typeSpecifierBase.PrimarySource.SourceName;
                    SourceCodePosition newPosition = new SourceCodePosition(
                        isPrimary: true,
                        startOffset: updateItem.StartOffset + spaceCount,
                        length: updateItem.NewText.Length - spaceCount,
                        startLine: updateItem.StartLine,
                        startColumn: updateItem.StartColumn + spaceCount,
                        sourceName: sourceName);

                    Sql90ResolveActions.ClrRelatedElementReset(typeSpecifierBase);

                    if (IsXmlType(oldType) || IsXmlType(newType) || isNamelessTimestamp) 
                    // oldType and newType are different, so we change to or from an XML type
                    // oldType and newType can't be both XML: the previous if statement ensures this
                    {
                        // we have only two types of type specifier: SqlTypeSpecifier and SqlXmlTypeSpecifier
                        // we need to change the type specifier if either new or old type is XML
                        DeleteElement(column.TypeSpecifier);

                        if (IsXmlType(newType))
                        {
                            typeSpecifierBase = _model.CreateElement<SqlXmlTypeSpecifier>();
                        }
                        else
                        {
                            typeSpecifierBase = _model.CreateElement<SqlTypeSpecifier>();
                        }
                        column.TypeSpecifier = typeSpecifierBase;

                        IModelSingleRelationship<SqlColumn, SqlTypeSpecifierBase> relationToTypeSpec = column.GetTypeSpecifierRelationship();
                        IModelRelationshipEntry typeSpecEntry = relationToTypeSpec.GetRelationshipEntry();
                        typeSpecEntry.SourceCodePositions.Add(newPosition);
                    }
                    else
                    {
                        SourceCodePosition primary = FindPrimaryPosition(typeSpecifierBase.SourceCodePositions);
                        if (primary != null)
                        {
                            typeSpecifierBase.SourceCodePositions.Remove(primary);
                        }
                    }

                    typeSpecifierBase.SourceCodePositions.Add(newPosition);

                    // we don't have to worry about XML type specifier: if typeSpecifier is an XML type specifier,
                    // it must have been newly created, and its properties have their default values.
                    // but for non-XML type specifier, we may have changed between a UDDT and a builtin, or
                    // switched between two builtins, either case, we would like to reset the property values 
                    // according to their current type
                    SqlTypeSpecifier typeSpecifier = typeSpecifierBase as SqlTypeSpecifier;
                    if (typeSpecifier != null)
                    {
                        typeSpecifier.IsMax = false;
                        typeSpecifier.Length = 0;
                        typeSpecifier.Precision = 0;
                        typeSpecifier.Scale = 0;

                        if (builtin != null)
                        {
                            SetDefaultTypeSpecifierProperties(typeSpecifier, builtin.SqlDataType);
                        }
                    }

                    // we don't hook up the type with the type specifier, since we may have more than one
                    // UDDT with the same name. we can check, but we duplicate resolver's work, and we
                    // may miss resolve actions
                    // builtins don't have this issue, but if we don't hook up non-bultins, the updater's
                    // client has to call model.Resolve(), so hooking up builtins seems to make little
                    // sense
                    typeSpecifierBase.ResolutionStatus = ModelElementResolutionStatus.ResolveCandidate;

                    SqlModelBuilderResolvableAnnotation anno = _model.CreateNamedAnnotation<SqlModelBuilderResolvableAnnotation>(newType.Name);

                    if (newType is SqlUserDefinedType && newType.IsBuiltIn())
                    {
                        // fix the TargetType to match model builder pipeline
                        anno.TargetType = typeof(SqlUserDefinedType);
                    }
                    else
                    {
                        anno.TargetType = typeof(SqlType);
                    }

                    IModelSingleRelationship<SqlTypeSpecifierBase, SqlType> relationToType = typeSpecifierBase.GetTypeRelationship();
                    IModelRelationshipEntry entry = relationToType.RecreateRelationshipEntry();
                    entry.AddAnnotation(anno);
                    entry.SourceCodePositions.Add(newPosition);

                    // resolve the new type specifier
                    SqlSchemaModel model = (SqlSchemaModel)column.Model;
                    model.Resolve(typeSpecifierBase);

                    SqlModelResolver.ResetWildcardOnElement(column.Parent, column.Parent.Name);
                    SqlModelResolver.ResetPotentialRelationshipEntries(column.Name, typeof(SqlSimpleColumn));
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        private static void AdjustNullability(SqlSimpleColumn column, SqlType newType)
        {
            ColumnDefinition columnAst = SqlModelUpdaterUtils.GetPrimaryAst<ColumnDefinition>(column);
            bool hasNullConstraint = false;
            foreach (ConstraintDefinition constraint in columnAst.Constraints)
            {
                if (constraint is NullableConstraintDefinition)
                {
                    hasNullConstraint = true;
                    break;
                }
            }

            if (hasNullConstraint == false && // if there exists an explicit nullability constraint, no adjustment
                columnAst.IdentityOptions == null && // for an identity column, no adjustment
                SqlModelUpdaterUtils.IsInlinePrimaryKeyColumn(column) == false) // if the column is part of inline primary key, no adjustment
            {
                bool isNull = true;
                SqlBuiltInType builtin = newType as SqlBuiltInType;
                if (builtin != null)
                {
                    isNull = builtin.SqlDataType != SqlDataType.Timestamp && builtin.SqlDataType != SqlDataType.Rowversion;

                    // remove possible existing ImplicitNullabilityAnnotation
                    IList<ImplicitNullabilityAnnotation> annos = column.GetAnnotations<ImplicitNullabilityAnnotation>();
                    Debug.Assert(annos.Count == 0 || annos.Count == 1, "There should exist zero or one ImplicitNullabilityAnnotation");
                    foreach (ImplicitNullabilityAnnotation anno in annos)
                    {
                        anno.Delete();
                    }
                }
                else
                {
                    SqlUserDefinedDataType uddt = newType as SqlUserDefinedDataType;
                    SqlUserDefinedType udt = newType as SqlUserDefinedType;
                    if (uddt != null || udt != null)
                    {
                        IList<ImplicitNullabilityAnnotation> annos = column.GetAnnotations<ImplicitNullabilityAnnotation>();
                        Debug.Assert(annos.Count == 0 || annos.Count == 1, "There should exist zero or one ImplicitNullabilityAnnotation");
                        if (annos.Count == 0)
                        {
                            ImplicitNullabilityAnnotation anno = column.Model.CreateAnnotation<ImplicitNullabilityAnnotation>();
                            column.AddAnnotation(anno);
                        }
                        if (uddt != null)
                        {
                            isNull = uddt.IsNullable;
                        }
                    }
                }

                column.IsNullable = isNull;
            }
        }

        public IList<SqlScriptUpdateInfo> SetColumnDataTypeLength(SqlSimpleColumn column, int length, bool isMax = false)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            if (column.IsFileStream)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't change length for a FILESTREAM column");
            }

            if (isMax == false && length < 1)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: length");
                throw new ArgumentOutOfRangeException("length");
            }

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                SqlBuiltInType columnType = column.TypeSpecifier.Type as SqlBuiltInType;
                if (columnType == null ||
                    SqlModelUpdaterConstants.SqlTypesCanHaveLength.Contains(columnType.SqlDataType) == false)
                {
                    // length is only applicable for xxxChar or xxxBinary
                    SqlModelUpdaterUtils.TraceAndThrow("Length is only applicable for char and binary families");
                }

                if (isMax &&
                    SqlModelUpdaterConstants.SqlTypesCanHaveMaxLength.Contains(columnType.SqlDataType) == false)
                {
                    // length is only applicable for xxxChar or xxxBinary
                    SqlModelUpdaterUtils.TraceAndThrow("MAX is only applicable for VarXXXX families");
                }

                if (column.GraphType != SqlColumnGraphType.None)
                {
                    // Can't edit graph columns.
                    SqlModelUpdaterUtils.TraceAndThrow("Cannot edit data type of graph columns");
                }

                SqlTypeSpecifier typeSpecifier = (SqlTypeSpecifier)column.TypeSpecifier;
                if (isMax != typeSpecifier.IsMax ||
                    (isMax == false && typeSpecifier.Length != length))
                {
                    updates = SqlScriptUpdater.SetColumnDataTypeLength(column, length, isMax);
                    NotifyModelUpdate(updates);
                    UpdatePositionsAndScriptCache(updates);

                    typeSpecifier.IsMax = isMax;
                    if (isMax)
                    {
                        typeSpecifier.Length = 0;
                    }
                    else
                    {
                        typeSpecifier.Length = length;
                    }
                    ResetValidationStatus(column);
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> SetColumnDataTypePrecision(SqlSimpleColumn column, int precision)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            if (precision < 0)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: precision");
                throw new ArgumentOutOfRangeException("precision");
            }

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                SqlBuiltInType columnType = column.TypeSpecifier.Type as SqlBuiltInType;
                if (columnType == null ||
                    SqlModelUpdaterConstants.SqlTypesCanHavePrecision.Contains(columnType.SqlDataType) == false)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Precision is not applicable");
                }

                SqlTypeSpecifier typeSpecifier = (SqlTypeSpecifier)column.TypeSpecifier;
                if (typeSpecifier.Precision != precision)
                {
                    updates = SqlScriptUpdater.SetColumnDataTypePrecision(column, precision);
                    NotifyModelUpdate(updates);
                    UpdatePositionsAndScriptCache(updates);

                    typeSpecifier.Precision = precision;
                    ResetValidationStatus(column);
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> SetColumnDataTypeScale(SqlSimpleColumn column, int scale)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            if (scale < 0)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: scale");
                throw new ArgumentOutOfRangeException("scale");
            }

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                SqlBuiltInType columnType = column.TypeSpecifier.Type as SqlBuiltInType;
                if (columnType == null ||
                    SqlModelUpdaterConstants.SqlTypesCanHaveScale.Contains(columnType.SqlDataType) == false)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Scale is not applicable");
                }

                SqlTypeSpecifier typeSpecifier = (SqlTypeSpecifier)column.TypeSpecifier;
                if (typeSpecifier.Scale != scale)
                {
                    updates = SqlScriptUpdater.SetColumnDataTypeScale(column, scale);
                    NotifyModelUpdate(updates);
                    UpdatePositionsAndScriptCache(updates);

                    typeSpecifier.Scale = scale;
                    ResetValidationStatus(column);
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        /// <summary>
        /// Partially resolves the model to find the column references, and
        /// returns the locations of the given column.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public IList<SqlScriptUpdateInfo> RefactorRenameColumnInTableScope(SqlColumn column, string newName)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");
            SqlExceptionUtils.ValidateNullParameter(newName, "newName");

            IList<SqlScriptUpdateInfo> update = null;

            _modelController.AcquireWriteAccess();
            try
            {
                update = SingleFileSymbolLocator.ComputeUpdateInformation(column, newName);
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return NotifyModelUpdate(update);
        }

        // TODO, yangg: think about the scenario where the underlying column definition doesn't have an
        // explicit definition, but has a default setting for nullability
        public IList<SqlScriptUpdateInfo> SetColumnNullable(SqlSimpleColumn column, bool isNullable)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                if (isNullable != column.IsNullable)
                {
                    updates = SqlScriptUpdater.SetColumnNullable(column, isNullable);
                    NotifyModelUpdate(updates);

                    // exclude type specification/constraints that may be defined before
                    // the nullability token from having their positions updated
                    Tuple<int, int> excludingRange = new Tuple<int, int>(column.PrimarySource.Offset + 1, updates[0].Updates.First().StartOffset);
                    IList<ScriptHandle> handles = GetUpdatedHandles(updates);

                    // since we'll explicitly specify the column's nullability, we should delete possible ImplicitNullabilityAnnotation attached to it
                    IList<ImplicitNullabilityAnnotation> annos = column.GetAnnotations<ImplicitNullabilityAnnotation>();
                    if (annos.Count > 0)
                    {
                        SqlTracer.AssertTraceEvent(annos.Count == 1, TraceEventType.Error, SqlTraceId.TSqlModel, "Find more than one instance of ImplicitNullabilityAnnotation");
                        foreach (var anno in annos)
                        {
                            anno.Delete();
                        }
                    }

                    UpdatePositionsAndScriptCache(updates, handles, excludingRange);
                    column.IsNullable = isNullable;
                    ResetValidationStatus(column);
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> SetColumnIsIdentity(SqlSimpleColumn column, bool isIdentity)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            IList<SqlScriptUpdateInfo> updates = null;

            if (column.GraphType != SqlColumnGraphType.None)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Cannot edit graph columns");
            }

            if (column.IsIdentity != isIdentity)
            {
                updates = SqlScriptUpdater.SetColumnIsIdentity(column, isIdentity);
                NotifyModelUpdate(updates);
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> SetColumnIdentitySeed(SqlSimpleColumn column, SqlDecimal seed)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            if (!column.IsIdentity)
            {
                SqlModelUpdaterUtils.TraceAndThrow("should not try to modify identity seed if column is not identity");
            }

            return NotifyModelUpdate(SqlScriptUpdater.SetColumnIdentitySeed(column, seed));
        }

        public IList<SqlScriptUpdateInfo> SetColumnIdentityIncrement(SqlSimpleColumn column, SqlDecimal increment)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            if (!column.IsIdentity)
            {
                SqlModelUpdaterUtils.TraceAndThrow("should not try to modify identity increment if column is not identity");
            }

            return NotifyModelUpdate(SqlScriptUpdater.SetColumnIdentityIncrement(column, increment));
        }

        public IList<SqlScriptUpdateInfo> SetXmlColumnStyle(SqlSimpleColumn column, bool isXmlDocument)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                SqlXmlTypeSpecifier xmlTypeSpec = column.TypeSpecifier as SqlXmlTypeSpecifier;
                if (xmlTypeSpec == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Column is not of type XML");
                }

                IModelSingleRelationship<SqlXmlTypeSpecifier, SqlXmlSchemaCollection> relationshipToXmlSchema = xmlTypeSpec.GetXmlSchemaCollectionRelationship();
                IModelRelationshipEntry xmlSchemaEntry = relationshipToXmlSchema.GetRelationshipEntry();
                if (isXmlDocument == true && xmlSchemaEntry == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Can't change XML style to DOCUMENT for an XML column without an XML schema");
                }

                bool wasXmlDocument = xmlTypeSpec.XmlStyle == SqlXmlDataTypeStyle.Document;
                if (isXmlDocument != wasXmlDocument) // we need to update XML style
                {
                    updates = SqlScriptUpdater.SetXmlColumnStyle(column, isXmlDocument);
                    NotifyModelUpdate(updates);
                    
                    Tuple<int, int> excludingRange = null;

                    // we need exclude XML Schema from being updated if we insert something in front of it
                    if (updates.Count > 0)
                    {
                        SqlScriptUpdateItem updateItem = updates[0].Updates.FirstOrDefault();
                        if (updateItem != null && updateItem.Length == 0)
                        {
                            excludingRange = new Tuple<int, int>(updateItem.StartOffset, updateItem.StartOffset); // this is the start offset of the XML schema
                        }
                    }

                    UpdatePositionsAndScriptCache(updates, excludingRange);

                    xmlTypeSpec.XmlStyle = isXmlDocument ? SqlXmlDataTypeStyle.Document : SqlXmlDataTypeStyle.Content;
                    ResetValidationStatus(xmlTypeSpec);
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> SetXmlColumnXmlSchema(SqlSimpleColumn column, SqlXmlSchemaCollection xmlSchema)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                SqlXmlTypeSpecifier xmlTypeSpec = column.TypeSpecifier as SqlXmlTypeSpecifier;
                if (xmlTypeSpec == null)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Column is not of type XML");
                }

                IModelSingleRelationship<SqlXmlTypeSpecifier, SqlXmlSchemaCollection> relationshipToXmlSchema = xmlTypeSpec.GetXmlSchemaCollectionRelationship();
                IModelRelationshipEntry xmlSchemaEntry = relationshipToXmlSchema.GetRelationshipEntry();

                if (SameXmlSchema(xmlSchema, xmlSchemaEntry) == false) // we need to update XML schema
                {
                    updates = _scriptUpdater.SetXmlColumnXmlSchema(column, xmlSchema);
                    NotifyModelUpdate(updates);
                    UpdatePositionsAndScriptCache(updates);

                    if (xmlSchema == null)
                    {
                        // remove relationship entry for XML schema
                        relationshipToXmlSchema.Clear();
                        // when removing schema collection, style is 'content' by default
                        xmlTypeSpec.XmlStyle = SqlXmlDataTypeStyle.Content;
                    }
                    else if (xmlSchemaEntry != null)
                    {
                        // change existing XML schema 

                        // unhook
                        xmlSchemaEntry.Element = null;
                        SqlModelResolver.DeleteMismatchedNameAnnotations(xmlSchemaEntry);

                        // replace SqlModelBuilderResolvableAnnotation
                        IList<SqlModelBuilderResolvableAnnotation> annos = xmlSchemaEntry.GetAnnotations<SqlModelBuilderResolvableAnnotation>();
                        if (annos.Count == 1)
                        {
                            annos[0].Delete();
                        }
                        SqlModelBuilderResolvableAnnotation anno = column.Model.CreateNamedAnnotation<SqlModelBuilderResolvableAnnotation>(xmlSchema.Name);
                        anno.TargetType = typeof(SqlXmlSchemaCollection);
                        xmlSchemaEntry.AddAnnotation(anno);
                    }
                    else
                    {
                        // add a new XML schema: c1 XML -> cl XML (dbo.schemaName)

                        IModelRelationshipEntry newEntry = relationshipToXmlSchema.RecreateRelationshipEntry();
                        SqlModelBuilderResolvableAnnotation anno = column.Model.CreateNamedAnnotation<SqlModelBuilderResolvableAnnotation>(xmlSchema.Name);
                        anno.TargetType = typeof(SqlXmlSchemaCollection);
                        newEntry.AddAnnotation(anno);

                        // for this situation, we know we have only one item inside updates and only one item inside updates[0].Updates
                        // and we know the XML schema name was inserted after keyword XML
                        // can't find a better easy way to obtain the position info without exploiting this intimate knowledge
                        SqlScriptUpdateItem updateItem = updates[0].Updates.First();
                        SourceCodePosition position = new SourceCodePosition(
                            isPrimary: true,
                            startOffset: updateItem.StartOffset + 1, // 1 for left parenthesis
                            length: updateItem.NewText.Length - 2, // new XML schema, 2 for the parentheses
                            startLine: updateItem.StartLine,
                            startColumn: updateItem.StartColumn + 1, // 1 for space and left parenthesis
                            sourceName: updates[0].ScriptCacheIdentifier);

                        newEntry.SourceCodePositions.Add(position);
                    }

                    xmlTypeSpec.ResolutionStatus = ModelElementResolutionStatus.ResolveCandidate;
                    ResetValidationStatus(xmlTypeSpec);
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        // Public Methods that Operate on SqlComputedColumn

        public IList<SqlScriptUpdateInfo> SetColumnIsPersisted(SqlComputedColumn column, bool isPersisted)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            if (isPersisted == false &&
                column.IsPersisted == true &&
                SqlModelUpdaterUtils.DoesColumnHaveCheckOrForeignKeyOrNullableConstraint(column))
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't set computed column to non-persisted if a persisted computed column has check constraint, foreign key or nullable constraint");
            }

            IList<SqlScriptUpdateInfo> updates = null;

            if (column.IsPersisted != isPersisted)
            {
                updates = SqlScriptUpdater.SetColumnIsPersisted(column, isPersisted);
                NotifyModelUpdate(updates);
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> SetColumnIsPersistedNullable(SqlComputedColumn column, bool isPersistedNullable)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            IList<SqlScriptUpdateInfo> updates = null;

            if (!column.IsPersisted)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't set persistence nullability on non-persisted computed column");
            }

            // For the purposes of this particular comparison,
            // treat unspecified nullability of computed column the same as explicit nullability specified by isPersistedNullable parameter.
            bool columnNullability = column.IsPersistedNullable ?? true;

            if (SqlModelUpdaterUtils.IsInlinePrimaryKeyColumn(column))
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't set computed column persistednullability if a computed column is part of the primary key constraint");
            }

            if (columnNullability != isPersistedNullable)
            {
                updates = SqlScriptUpdater.SetColumnIsPersistedNullable(column, isPersistedNullable);
                NotifyModelUpdate(updates);
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> SetColumnExpressionScript(SqlComputedColumn column, string expression)
        {
            SqlExceptionUtils.ValidateNullParameter(column, "column");
            SqlExceptionUtils.ValidateNullParameter(expression, "expression");

            ValidateScalarExpression(_model.GetParser(column), expression);

            return NotifyModelUpdate(SqlScriptUpdater.SetColumnExpressionScript(column, expression));
        }
    
    }
}

