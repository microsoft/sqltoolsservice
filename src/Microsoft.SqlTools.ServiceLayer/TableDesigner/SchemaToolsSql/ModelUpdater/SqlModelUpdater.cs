//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdater.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.Common;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed partial class SqlModelUpdater
    {
        // Notifies which script updates would be applied before actually 
        // performing any changes to the Model. 
        // Handler can check whether the affected scripts can be edited before allowing
        // the model update operation to continue.
        public event EventHandler<BeforeModelUpdateEventArgs> BeforeModelUpdate;

        private readonly SqlSchemaModel _model;
        private readonly IDataSchemaModelController _modelController;
        private readonly SqlScriptUpdater _scriptUpdater;
        private readonly DeleteActions _deleteActions;

        public SqlModelUpdater(SqlSchemaModel model, IDataSchemaModelController modelController, DeleteActions deleteActions)
        {
            SqlExceptionUtils.ValidateNullParameter(model, "model");
            SqlExceptionUtils.ValidateNullParameter(modelController, "modelController");
            SqlExceptionUtils.ValidateNullParameter(deleteActions, "deleteActions");

            _model = model;
            _modelController = modelController;
            _deleteActions = deleteActions;
            _scriptUpdater = new SqlScriptUpdater((SqlDatabaseSchemaProvider)model.DatabaseSchemaProvider);
        }

        // TODO, yangg: merge this logic with interpreter
        private static void SetDefaultTypeSpecifierProperties(SqlTypeSpecifier typeSpec, SqlDataType sqlType)
        {
            switch (sqlType)
            {
                case SqlDataType.Decimal:
                case SqlDataType.Numeric:
                    typeSpec.Precision = 18;
                    break;
                case SqlDataType.Float:
                    typeSpec.Precision = 53;
                    break;
                case SqlDataType.Char:
                case SqlDataType.VarChar:
                case SqlDataType.NChar:
                case SqlDataType.NVarChar:
                case SqlDataType.Binary:
                case SqlDataType.VarBinary:
                    typeSpec.Length = 1;
                    break;
                case SqlDataType.Time:
                case SqlDataType.DateTime2:
                case SqlDataType.DateTimeOffset:
                    typeSpec.Scale = 7;
                    break;
            }
        }

        private static bool SameXmlSchema(SqlXmlSchemaCollection newXmlSchema, IModelRelationshipEntry xmlSchemaEntry)
        {
            ModelIdentifier oldSchemaName = null;
            if (xmlSchemaEntry != null)
            {
                IList<SqlModelBuilderResolvableAnnotation> annos = xmlSchemaEntry.GetAnnotations<SqlModelBuilderResolvableAnnotation>();
                if (annos != null && annos.Count > 0)
                {
                    oldSchemaName = annos[0].Name;
                }
            }

            ModelIdentifier newSchemaName = null;
            if (newXmlSchema != null)
            {
                newSchemaName = newXmlSchema.Name;
            }

            bool same = true;
            if (oldSchemaName != null && newSchemaName != null)
            {
                same = oldSchemaName.Equals(newSchemaName);
            }
            else if (oldSchemaName != null || newSchemaName != null)
            {
                same = false;
            }
            return same;
        }


        #region Private Validation Helpers
        /// <summary>
        /// Validates that the specified index represents a valid position to insert a column into the specified table.
        /// If the index is invalid, the method will throw an exception.
        /// </summary>
        private static void ValidateColumnIndex(int count, int index)
        {
            // Validate the column position.
            if (index < 0 || index > count)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: index");
                throw new ArgumentOutOfRangeException("index");
            }
        }

        /// <summary>
        /// Determines whether the identifier is valid or not. The proposed identifier is validated
        /// using parser settings based on the specified model element.
        /// </summary>
        private void ValidateIdentifier(IModelElement element, string identifier, string parameterName)
        {
            TSqlParser parser = _model.GetParser(element);
            ValidateIdentifier(parser, identifier, parameterName);
        }

        /// <summary>
        /// Determines whether the identifier is valid or not. If the identifier is not valid
        /// and it cannot be escaped, this method will throw an exception.
        /// </summary>
        private static void ValidateIdentifier(TSqlParser parser, string identifier, string parameterName)
        {
            SqlExceptionUtils.ValidateNullOrEmptyParameter(identifier, parameterName);

            if (identifier.Length <= SqlCommonConstants.MaxIdentifierLength)
            {
                if (parser.ValidateIdentifier(identifier))
                {
                    return;
                }

                string escapedIdentifier = Identifier.EncodeIdentifier(identifier);
                if (parser.ValidateIdentifier(escapedIdentifier))
                {
                    return;
                }
            }

            throw new ArgumentException("Invalid identifier.", parameterName);
        }

        private static void ValidateScalarExpression(TSqlParser parser, string expression)
        {
            IList<ParseError> errors = null;

            // Validate that the expression is syntactically valid
            ScalarExpression expressionTree = parser.ParseExpression(new StringReader(expression), out errors);
            if (expressionTree == null || errors != null && errors.Count > 0)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Invalid argument value: expression");
                throw new ArgumentOutOfRangeException("expression");
            }
        }

        private static void ValidateBooleanExpression(TSqlParser parser, string expression)
        {
            IList<ParseError> errors = null;

            // Validate that the boolean expression is syntactically valid
            BooleanExpression expressionTree = parser.ParseBooleanExpression(new StringReader(expression), out errors);
            if (expressionTree == null || errors != null && errors.Count > 0)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Invalid argument value: expression");
                throw new ArgumentOutOfRangeException("expression");
            }
        }

        #endregion


        private void DeleteConstraints(SqlColumn column)
        {
            SqlTable table = (SqlTable)column.Parent;

            foreach (var constraint in table.Constraints)
            {
                if (SqlModelUpdaterUtils.IsInlineColumnConstraint(column, constraint) == true)
                {
                    DeleteElement(constraint);
                }
            }
        }

        private static Tuple<int, int> CreateExcludingRangeForDeleteColumn(SqlTable table, int endingOffset)
        {
            Tuple<int, int> excludingRange = null;

            foreach (var colum in table.Columns)
            {
                if (colum.PrimarySource != null && colum.PrimarySource.Offset + colum.PrimarySource.Length == endingOffset)
                {
                    excludingRange = new Tuple<int, int>(colum.PrimarySource.Offset, endingOffset);
                    break;
                }
            }

            // notice that if the preceding column has an inline constraint, we should already find it in the loop above

            if (excludingRange == null)
            {
                foreach (var constraint in table.Constraints)
                {
                    if (constraint.PrimarySource.Offset + constraint.PrimarySource.Length == endingOffset)
                    {
                        excludingRange = new Tuple<int, int>(constraint.PrimarySource.Offset, endingOffset);
                        break;
                    }
                }
            }

            return excludingRange;
        }

        private ColumnDefinition GetColumnAst(SqlTable table, int columnIndex)
        {
            if (table.IsNode)
            {
                // Node tables have 1 extra column that does not appear in the AST.
                columnIndex = columnIndex - 1;
            }
            else if (table.IsEdge)
            {
                // Edge tables have 3 extra columns that do not appear in the AST.
                columnIndex = columnIndex - 3;
            }

            CreateTableStatement tableAst = SqlModelUpdaterUtils.GetPrimaryAst<CreateTableStatement>(table);
            ColumnDefinition columnAst = tableAst.Definition.ColumnDefinitions[columnIndex];
            return columnAst;
        }

        private IList<SqlScriptUpdateInfo> NotifyModelUpdate(IList<SqlScriptUpdateInfo> updates)
        {
            if (BeforeModelUpdate != null)
            {
                BeforeModelUpdateEventArgs args = new BeforeModelUpdateEventArgs(updates);
                BeforeModelUpdate(this, args);
            }
            return updates;
        }

        public IList<SqlScriptUpdateInfo> RenameElement(ISqlModelElement element, string newName)
        {
            SqlExceptionUtils.ValidateNullParameter(element, "element");
            SqlExceptionUtils.ValidateNullOrEmptyParameter(newName, "newName");
            SqlExceptionUtils.ValidateParameterLength(newName, "newName", SqlInterpretationConstants.MaxIdentifierLength);

            return NotifyModelUpdate(SqlScriptUpdater.RenameElement(element, newName));
        }

        public IList<SqlScriptUpdateInfo> AddExtendedProperty(ISqlExtendedPropertyHost host, string name, string value)
        {
            SqlExceptionUtils.ValidateNullParameter(host, "host");
            SqlExceptionUtils.ValidateNullParameter(name, "name");
            SqlExceptionUtils.ValidateNullParameter(value, "value");

            if (host is SqlTable == false &&
                host is SqlSimpleColumn == false &&
                host is SqlComputedColumn == false &&
                host is SqlColumnSet == false)
            {
                SqlModelUpdaterUtils.TraceAndThrow("ModelUpdater doesn't support this type");
            }

            if (host is SqlSimpleColumn && (host as SqlSimpleColumn).GraphType != SqlColumnGraphType.None)
            {
                SqlModelUpdaterUtils.TraceAndThrow("ModelUpdater doesn't support this type");
            }

            return NotifyModelUpdate(_scriptUpdater.AddExtendedProperty(host, name, value));
        }

        public IList<SqlScriptUpdateInfo> SetExtendedProperty(SqlExtendedProperty extendedProperty, string value)
        {
            SqlExceptionUtils.ValidateNullParameter(extendedProperty, "extendedProperty");
            SqlExceptionUtils.ValidateNullParameter(value, "value");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                if (extendedProperty.Value == null || // can't think of scenarios that the value can be null, just check to be safe
                    _model.Comparer.Equals(extendedProperty.Value.Script, value) == false)
                {
                    updates = _scriptUpdater.SetExtendedProperty(extendedProperty, value);
                    NotifyModelUpdate(updates);
                    UpdatePositionsAndScriptCache(updates);

                    ModelPropertyClass valuePropertyClass = SqlExtendedProperty.ValueClass;

                    ModelIdentifier annoId = _model.CreateIdentifier(valuePropertyClass.Name);
                    IList<ExternalPropertyAnnotation> extAnnos = extendedProperty.GetAnnotations<ExternalPropertyAnnotation>(annoId);
                    Debug.Assert(extAnnos.Count == 1, "Find no or more than one ExternalPropertyAnnotation on extended property");
                    foreach (ExternalPropertyAnnotation anno in extAnnos)
                    {
                        anno.Delete();
                    }

                    ScalarExpression propertyValueAst = SqlModelUpdaterUtils.GetExtendedPropertyValueAst(extendedProperty);
                    _model.CreateExternalPropertyAnnotation(
                        extendedProperty,
                        valuePropertyClass,
                        extendedProperty.PrimarySource.SourceName,
                        propertyValueAst.StartOffset,
                        propertyValueAst.FragmentLength,
                        propertyValueAst.StartLine,
                        propertyValueAst.StartColumn);
                    ResetValidationStatusImpl(extendedProperty);
                }
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> DeleteExtendedProperty(SqlExtendedProperty extendedProperty)
        {
            SqlExceptionUtils.ValidateNullParameter(extendedProperty, "extendedProperty");

            IList<SqlScriptUpdateInfo> updates = null;

            _modelController.AcquireWriteAccess();
            try
            {
                updates = _scriptUpdater.DeleteExtendedProperty(extendedProperty);
                NotifyModelUpdate(updates);
                IList<ScriptHandle> handles = GetUpdatedHandles(updates);
                DeleteElement(extendedProperty);
                UpdatePositionsAndScriptCache(updates, handles);
            }
            finally
            {
                _modelController.ReleaseWriteAccess();
            }

            return updates;
        }

        public IList<SqlScriptUpdateInfo> CreateIndex(SqlTable table, string name)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            // Hekaton table only supports inline indexes.
            if (table.IsMemoryOptimized)
            {
               return AddInlineIndex(table, name);
            }
            ValidateIdentifier(table, name, "name");
            return NotifyModelUpdate(_scriptUpdater.CreateIndex(table, name));
                
           
        }

        public IList<SqlScriptUpdateInfo> CreateXmlIndex(SqlTable table, string name, bool isPrimary)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, name, "name");

            return NotifyModelUpdate(_scriptUpdater.CreateXmlIndex(table, name, isPrimary));
        }

        public IList<SqlScriptUpdateInfo> CreateSelectiveXmlIndex(SqlTable table, string name, bool isPrimary)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, name, "name");

            return NotifyModelUpdate(_scriptUpdater.CreateSelectiveXmlIndex(table, name, isPrimary));
        }

        public IList<SqlScriptUpdateInfo> CreateSpatialIndex(SqlTable table, string name)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, name, "name");

            return NotifyModelUpdate(_scriptUpdater.CreateSpatialIndex(table, name));
        }

        public IList<SqlScriptUpdateInfo> CreateFullTextIndex(SqlTable table)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            return NotifyModelUpdate(_scriptUpdater.CreateFullTextIndex(table));
        }

        public IList<SqlScriptUpdateInfo> CreateColumnStoreIndex(SqlTable table, string name)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, name, "name");

            return NotifyModelUpdate(_scriptUpdater.CreateColumnStoreIndex(table, name));
        }

        public IList<SqlScriptUpdateInfo> CreateDmlTrigger(SqlTable table, string name)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");
            ValidateIdentifier(table, name, "name");

            return NotifyModelUpdate(_scriptUpdater.CreateDmlTrigger(table, name));
        }

        public IList<SqlScriptUpdateInfo> DeleteTable(SqlTable table)
        {
            SqlExceptionUtils.ValidateNullParameter(table, "table");

            return NotifyModelUpdate(_scriptUpdater.DeleteTable(table));
        }

        public IList<SqlScriptUpdateInfo> DeleteView(SqlView view)
        {
            SqlExceptionUtils.ValidateNullParameter(view, "view");

            return NotifyModelUpdate(_scriptUpdater.DeleteView(view));
        }

        public IList<SqlScriptUpdateInfo> DeleteSubroutine(SqlSubroutine subroutine)
        {
            SqlExceptionUtils.ValidateNullParameter(subroutine, "subroutine");

            return NotifyModelUpdate(_scriptUpdater.DeleteSubroutine(subroutine));
        }

        public IList<SqlScriptUpdateInfo> DeleteIndex(SqlIndex index)
        {
            SqlExceptionUtils.ValidateNullParameter(index, "index");
            return NotifyModelUpdate(_scriptUpdater.DeleteIndex(index));
        }

        public IList<SqlScriptUpdateInfo> DeleteXmlIndex(SqlXmlIndex xmlIndex)
        {
            SqlExceptionUtils.ValidateNullParameter(xmlIndex, "xmlIndex");

            return NotifyModelUpdate(_scriptUpdater.DeleteIndex(xmlIndex));
        }

        public IList<SqlScriptUpdateInfo> DeleteSelectiveXmlIndex(SqlSelectiveXmlIndex selectiveXmlIndex)
        {
            SqlExceptionUtils.ValidateNullParameter(selectiveXmlIndex, "selectiveXmlIndex");

            return NotifyModelUpdate(_scriptUpdater.DeleteIndex(selectiveXmlIndex));
        }

        public IList<SqlScriptUpdateInfo> DeleteSpatialIndex(SqlSpatialIndex spatialIndex)
        {
            SqlExceptionUtils.ValidateNullParameter(spatialIndex, "spatialIndex");

            return NotifyModelUpdate(_scriptUpdater.DeleteIndex(spatialIndex));
        }

        public IList<SqlScriptUpdateInfo> DeleteFullTextIndex(SqlFullTextIndex fullTextIndex)
        {
            SqlExceptionUtils.ValidateNullParameter(fullTextIndex, "fullTextIndex");

            return NotifyModelUpdate(_scriptUpdater.DeleteIndex(fullTextIndex));
        }

        public IList<SqlScriptUpdateInfo> DeleteTrigger(SqlDmlTrigger trigger)
        {
            SqlExceptionUtils.ValidateNullParameter(trigger, "trigger");

            return NotifyModelUpdate(_scriptUpdater.DeleteTrigger(trigger));
        }

        public IList<SqlScriptUpdateInfo> DeleteColumnStoreIndex(SqlColumnStoreIndex columnStoreIndex)
        {
            SqlExceptionUtils.ValidateNullParameter(columnStoreIndex, "columnStoreIndex");

            return NotifyModelUpdate(_scriptUpdater.DeleteIndex(columnStoreIndex));
        }


        public IList<SqlScriptUpdateInfo> SetIndexIsClustered(SqlIndex index, bool isClustered)
        {
            SqlExceptionUtils.ValidateNullParameter(index, "index");

            if (isClustered == true &&
                index.IsClustered == false &&
                SqlModelUpdaterUtils.DoesIndexHaveFilterDefinition(index))
            {
                SqlModelUpdaterUtils.TraceAndThrow("Can't set IsClustered to true if a NONCLUSTERED index has filter definition");
            }

            return NotifyModelUpdate(SqlScriptUpdater.SetIndexIsClustered(index, isClustered));
        }

        public IList<SqlScriptUpdateInfo> SetIndexIsUnique(SqlIndex index, bool isUnique)
        {
            SqlExceptionUtils.ValidateNullParameter(index, "index");

            return NotifyModelUpdate(SqlScriptUpdater.SetIndexIsUnique(index, isUnique));
        }

        public IList<SqlScriptUpdateInfo> InsertFullTextIndexColumnAt(SqlFullTextIndex fullTextIndex, int index, SqlColumn column)
        {
            SqlExceptionUtils.ValidateNullParameter(fullTextIndex, "fullTextIndex");
            ValidateColumnIndex(fullTextIndex.Columns.Count, index);
            SqlExceptionUtils.ValidateNullParameter(column, "column");

            return NotifyModelUpdate(SqlScriptUpdater.InsertFullTextIndexColumnAt(fullTextIndex, index, column));
        }

        public IList<SqlScriptUpdateInfo> UpdateIndexColumns(SqlIndex sqlIndex, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            SqlExceptionUtils.ValidateNullParameter(sqlIndex, "sqlIndex");
            SqlExceptionUtils.ValidateNullParameter(columns, "columns");
            SqlExceptionUtils.ValidateNullParameter(sortOrder, "sortOrder");

            if (!columns.GetEnumerator().MoveNext())
            {
                SqlModelUpdaterUtils.TraceAndThrow("index should have at least 1 referencing column");
            }

            return NotifyModelUpdate(SqlScriptUpdater.UpdateIndexColumns(sqlIndex, columns, sortOrder));
        }

        public IList<SqlScriptUpdateInfo> UpdateColumnStoreIndexColumns(SqlColumnStoreIndex sqlColumnStoreIndex, IEnumerable<SqlColumn> columns, IEnumerable<bool> sortOrder)
        {
            SqlExceptionUtils.ValidateNullParameter(sqlColumnStoreIndex, "sqlColumnStoreIndex");
            SqlExceptionUtils.ValidateNullParameter(columns, "columns");
            SqlExceptionUtils.ValidateNullParameter(sortOrder, "sortOrder");

            if (!columns.GetEnumerator().MoveNext())
            {
                SqlModelUpdaterUtils.TraceAndThrow("columnstore index should have at least 1 referencing column");
            }

            return NotifyModelUpdate(SqlScriptUpdater.UpdateColumnStoreIndexColumns(sqlColumnStoreIndex, columns, sortOrder));
        }

        // get last column or last constraint
        private static TSqlFragment GetLastTableItem(CreateTableStatement tableAst)
        {
            TSqlFragment lastAst = tableAst.Definition.ColumnDefinitions[tableAst.Definition.ColumnDefinitions.Count - 1];
            if (tableAst.Definition.TableConstraints.Count > 0)
            {
                ConstraintDefinition lastConstraintAst = tableAst.Definition.TableConstraints[tableAst.Definition.TableConstraints.Count - 1];
                if (lastConstraintAst.StartOffset > lastAst.StartOffset)
                {
                    lastAst = lastConstraintAst;
                }
            }

            return lastAst;
        }

        private static SourceCodePosition FindPrimaryPosition(IEnumerable<SourceCodePosition> positions)
        {
            SourceCodePosition primary = null;
            foreach (var pos in positions)
            {
                if (pos.IsPrimary)
                {
                    primary = pos;
                    break;
                }
            }

            return primary;
        }

        private static bool IsXmlType(SqlType sqlType)
        {
            SqlBuiltInType builtIn = sqlType as SqlBuiltInType;
            return builtIn != null && builtIn.SqlDataType == SqlDataType.Xml;
        }

        private IList<ScriptHandle> GetUpdatedHandles(IList<SqlScriptUpdateInfo> updates)
        {
            List<ScriptHandle> handles = new List<ScriptHandle>();
            foreach (var updateInfo in updates)
            {
                handles.Add(GetUpdatedHandle(updateInfo));
            }
            return handles;
        }

        private void UpdatePositions(IList<SqlScriptUpdateInfo> updates, Tuple<int, int> excludingRange = null)
        {
            UpdatePositionsAndScriptCache(updates, excludingRange, false);   
        }
        
        private void UpdatePositionsAndScriptCache(IList<SqlScriptUpdateInfo> updates, Tuple<int, int> excludingRange = null , bool updateScriptCache = true)
        {
            IList<ScriptHandle> handles = GetUpdatedHandles(updates);
            UpdatePositionsAndScriptCache(updates, handles, excludingRange, updateScriptCache);
        }
    
        private void UpdatePositionsAndScriptCache(IList<SqlScriptUpdateInfo> updates, IList<ScriptHandle> handles, Tuple<int, int> excludingRange = null, bool updateScriptCache = true)
        {
            SqlTracer.AssertTraceEvent(updates.Count == handles.Count, TraceEventType.Error, SqlTraceId.TSqlModel,  "Updates and handles should have equal number of objects.");

            ErrorUpdater errorUpdater = new ErrorUpdater(_model);

            for (int i = 0; i < handles.Count; i++)
            {
                string newScript = handles[i].GetCachedScript().ToString();
                StringPositionConverter positionConverter = new StringPositionConverter(newScript);
                SqlScriptUpdateInfo updateInfo = updates[i];

                // we need to update errors before we update the scripts, since we need to use the old scripts
                errorUpdater.UpdateErrors(updateInfo, newScript, positionConverter);
                if (updateScriptCache)
                {
                    _model.ScriptCache.AddOrUpdateScript(handles[i]);
                }

                int startOffset;
                int lengthDelta;
                bool insertBefore; // new text was inserted at startOffset
                AggregateUpdates(updateInfo.Updates, out startOffset, out lengthDelta, out insertBefore);

                // update source code positions for elements and references
                _model.UpdateSourceCodePositions(updateInfo.ScriptCacheIdentifier, startOffset, lengthDelta, insertBefore, excludingRange, positionConverter);

                // update source code positions for annotations
                UpdateAnnotationSourcePositions(updateInfo.ScriptCacheIdentifier, startOffset, lengthDelta, insertBefore, excludingRange, positionConverter);
            }
        }

        private void UpdateAnnotationSourcePositions(
            string filename,
            int startOffset,
            int lengthDelta,
            bool insertBefore,
            Tuple<int, int> excludingRange,
            StringPositionConverter positionConverter)
        {
            IList<IModelElement> elements = _model.GetElementsFrom<IModelElement>(filename, ModelElementQueryFilter.Internal);
            foreach (var element in elements)
            {
                foreach (var anno in element.GetAnnotations())
                {
                    UpdataAnnotationPosition(anno, startOffset, lengthDelta, insertBefore, excludingRange, positionConverter);
                }

                foreach (var entry in element.GetReferencedRelationshipEntries())
                {
                    foreach (var anno in entry.GetAnnotations())
                    {
                        UpdataAnnotationPosition(anno, startOffset, lengthDelta, insertBefore, excludingRange, positionConverter);
                    }
                }
            }
        }

        private void UpdataAnnotationPosition(
            IModelAnnotation anno,
            int startOffset,
            int lengthDelta,
            bool insertBefore,
            Tuple<int, int> excludingRange,
            StringPositionConverter positionConverter)
        {
            ExternalPropertyAnnotation external = anno as ExternalPropertyAnnotation;
            if (external != null)
            {
                UpdatePosition(
                    external.Length,
                    external.Offset,
                    startOffset,
                    lengthDelta,
                    insertBefore,
                    excludingRange,
                    positionConverter,
                    newLength => external.Length = newLength,
                    newOffset => external.Offset = newOffset,
                    newLine => external.StartLine = newLine,
                    newColumn => external.StartColumn = newColumn);
            }
            else
            {
                SysCommentsObjectAnnotation sysComment = anno as SysCommentsObjectAnnotation;
                if (sysComment != null)
                {
                    UpdatePosition(
                        sysComment.Length,
                        sysComment.Offset,
                        startOffset,
                        lengthDelta,
                        insertBefore,
                        excludingRange,
                        positionConverter,
                        newLength => sysComment.Length = newLength,
                        newOffset => sysComment.Offset = newOffset,
                        newLine => sysComment.StartLine = newLine,
                        newColumn => sysComment.StartColumn = newColumn);
                }
                else
                {
                    ParameterOrVariableAnnotation paramOrVar = anno as ParameterOrVariableAnnotation;
                    if (paramOrVar != null)
                    {
                        UpdatePosition(
                            paramOrVar.Length,
                            paramOrVar.Offset,
                            startOffset,
                            lengthDelta,
                            insertBefore,
                            excludingRange,
                            positionConverter,
                            newLength => paramOrVar.Length = newLength,
                            newOffset => paramOrVar.Offset = newOffset,
                            newLine => paramOrVar.StartLine = newLine,
                            newColumn => paramOrVar.StartColumn = newColumn);
                    }
                }
            }
        }

        private static void UpdatePosition(
            int originalLength,
            int originalOffset,
            int startOffset,
            int delta,
            bool insertBefore,
            Tuple<int, int> excludingRange,
            StringPositionConverter positionConverter,
            Action<int> setLength,
            Action<int> setOffset,
            Action<int> setLine,
            Action<int> setColumn)
        {
            // update length if the original offset falls inside the changed portion
            if ((originalOffset <= startOffset) && ((originalOffset + originalLength) >= startOffset))
            {
                // see comments before the code that populates the excludingOffsets
                if (ModelStore.ShouldUpdateLength(originalOffset, originalLength, startOffset, excludingRange))
                {
                    setLength(originalLength + delta);
                }
            }

            // update offset 
            if ((originalOffset > startOffset) || // if the original offset is beyond the change point (startOffset), we need to update
                (insertBefore && originalOffset == startOffset)) // if the original offset is the change point and some text was inserted before it, we also need to update
            {
                int newOffset = originalOffset + delta;
                int newLine;
                int newColumn;
                positionConverter.GetLineColumnFromOffset(newOffset, out newLine, out newColumn);

                setOffset(newOffset);
                setLine(newLine + 1); //adjust to be one-based
                setColumn(newColumn + 1); // adjust to be one-based
            }
        }

        private ScriptHandle GetUpdatedHandle(SqlScriptUpdateInfo updateInfo)
        {
            string newScript = null;

            string oldScript = _model.ScriptCache.GetScript(updateInfo.ScriptCacheIdentifier);
            if (oldScript == null)
            {
                SqlModelUpdaterUtils.TraceAndThrow("Cannot obtain script for cache id: " + updateInfo.ScriptCacheIdentifier);
            }

            StringBuilder sb = new StringBuilder();
            Int32 pos = 0;
            foreach (var update in updateInfo.Updates)
            {
                if (pos > update.StartOffset)
                {
                    SqlModelUpdaterUtils.TraceAndThrow("Encounter overlapping updates");
                }

                sb.Append(oldScript.Substring(pos, update.StartOffset - pos));
                sb.Append(update.NewText);
                pos = update.StartOffset + update.Length;
            }
            sb.Append(oldScript.Substring(pos));

            newScript = sb.ToString();

            ScriptHandle scriptHandle = ScriptHandle.Create(updateInfo.ScriptCacheIdentifier, newScript);
            if (SqlModelBuilder.InputExceedsLimit(scriptHandle.Length))
            {
                throw new SqlModelUpdaterException(SqlErrorResource.ScriptHandle_SizeExceeded);
            }
            return scriptHandle;
        }

        // no positions (offsets) inside a set of changed portions will be referenced, so we can aggregate the portions
        // to calculate the total delta and the starting offset to simplify updating logic
        // exmaple: change
        //      CREATE TABLE t1(c1 DECIMAL(38 /*precision*/, 5 /*scale*/)
        // to
        //      CREATE TABLE t1(c1 DECIMAL(30 /*precision*/, 10 /*scale*/)
        // we have two updates around 38 and 5 respectively, and we expect no position or offset to be referenced
        // between the two portions
        private static void AggregateUpdates(IEnumerable<SqlScriptUpdateItem> updates, out Int32 startOffset, out Int32 lengthDelta, out bool insertBefore)
        {
            startOffset = 0;
            lengthDelta = 0;
            insertBefore = true;

            bool first = true;
            foreach (var update in updates)
            {
                if (first)
                {
                    first = false;
                    startOffset = update.StartOffset;
                }

                lengthDelta += (update.NewText.Length - update.Length);

                if (update.Length != 0)
                {
                    insertBefore = false;
                }
            }
        }

        private static Tuple<int, int> CreateExcludingRange(TSqlFragment fragment)
        {
            return new Tuple<int, int>(fragment.StartOffset, fragment.StartOffset + fragment.FragmentLength);
        }

        private void DeleteElement(ISqlModelElement element)
        {
            ResetValidationStatus(element);

            _deleteActions.Process(element);

            ModelIdentifier elementId = element.Name;
            if (elementId != null && element.ElementClass.IdentifierGroup != null)
            {
                SqlModelBuilderSchemaAnalyzer.HandleDuplicates(element, elementId);
                _model.Resolver.CheckForUnduplication(elementId, element.ElementClass.IdentifierGroup);
            }

            _model.ClearInterpreterProblems(element);

            element.Delete();
        }

        private static void ResetValidationStatus(IModelElement element)
        {
            ResetValidationStatusImpl(element);

            while (element != null)
            {
                ModelElementOwnerInfo ownerInfo = element.GetOwnerInfo();
                element = ownerInfo.Owner;
                if (ownerInfo.IsComposing)
                {
                    ResetValidationStatusImpl(element);
                }
                else
                {
                    element = null;
                }
            }
        }

        private static void ResetValidationStatusImpl(IModelElement element)
        {
            IList<IModelElement> referencingElements = element.GetReferencingElements();
            foreach (IModelElement e in referencingElements)
            {
                e.ValidationStatus = ModelElementValidationStatus.Initial;
            }
        }
    }
}
