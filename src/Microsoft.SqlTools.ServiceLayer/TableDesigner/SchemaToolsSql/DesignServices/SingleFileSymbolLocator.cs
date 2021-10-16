//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Tools.Schema.Common;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    /// <summary>
    /// Finds the symbol occurrences in the primary source file of the element.
    /// </summary>
    internal static class SingleFileSymbolLocator
    {
        private static HashSet<ModelRelationshipClass> _columnRelationshipClasses;

        static SingleFileSymbolLocator()
        {
            _columnRelationshipClasses = new HashSet<ModelRelationshipClass>();

            HashSet<ModelElementClass> classesToInspect = GetTableAndChildrenElementClasses();

            Type sqlColumnType = typeof(SqlColumn);
            foreach (ModelElementClass item in classesToInspect)
            {
                foreach (ModelRelationshipClass relationshipClass in item.RelationshipClasses)
                {
                    if (sqlColumnType.IsAssignableFrom(relationshipClass.RelatedElement))
                    {
                        _columnRelationshipClasses.Add(relationshipClass);
                    }
                }
            }

            // Add expression dependencies
            _columnRelationshipClasses.Add(SqlCheckConstraint.CheckExpressionDependenciesClass);
            _columnRelationshipClasses.Add(SqlComputedColumn.ExpressionDependenciesClass);
            _columnRelationshipClasses.Add(SqlDefaultConstraint.ExpressionDependenciesClass);
            _columnRelationshipClasses.Add(SqlDmlTrigger.BodyDependenciesClass);
            _columnRelationshipClasses.Add(SqlIndex.BodyDependenciesClass);
            _columnRelationshipClasses.Add(SqlStatistic.ExpressionDependenciesClass);
            _columnRelationshipClasses.Add(SqlExtendedProperty.HostClass);
            _columnRelationshipClasses.Add(SqlPermissionStatement.SecuredObjectClass);
        }

        private static HashSet<ModelElementClass> GetTableAndChildrenElementClasses()
        {
            ModelSchema schema = SqlSchemaModel.ModelSchema;
            HashSet<ModelElementClass> classesToInspect = new HashSet<ModelElementClass>();
            Queue<ModelElementClass> classesQueue = new Queue<ModelElementClass>();
            classesQueue.Enqueue(SqlTable.SqlTableClass);
            while (classesQueue.Count > 0)
            {
                ModelElementClass current = classesQueue.Dequeue();
                foreach (ModelElementClass item in schema.GetChildElementClasses(current))
                {
                    if (!classesToInspect.Contains(item))
                    {
                        classesToInspect.Add(item);
                        classesQueue.Enqueue(item);
                    }
                }
            }
            return classesToInspect;
        }


        /// <summary>
        /// Computes update information for the locations that the column name is used in the
        /// primary file. 
        /// </summary>
        public static IList<SqlScriptUpdateInfo> ComputeUpdateInformation(SqlColumn column, string newName)
        {
            if (column == null)
            {
                throw new ArgumentNullException("column");
            }

            if (column is SqlSimpleColumn && (column as SqlSimpleColumn).GraphType != SqlColumnGraphType.None)
            {
                throw new ArgumentException("Cannot edit graph columns");
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentOutOfRangeException("newName");
            }

            List<SqlScriptUpdateInfo> locations = new List<SqlScriptUpdateInfo>();

            // Since we're updating file text, we always use an ordinal comparison instead of the comparer associated with the model.
            // This allows the user to do things like refactor-rename an object name to the casing scheme they prefer (for text
            // readability for instance), even though it may have no effect on the structure of the model if the model is case insensitive.
            if (!column.IsDeleted() 
                && !column.IsExternal() 
                && column.Name != null
                && !newName.Equals(column.Name.Parts[column.Name.Parts.Count - 1], StringComparison.Ordinal))
            {
                // newName is un-escaped (plain text for the new column name)
                // we always quote the new column name to be consistent with inserting columns
                string stringLiteralName = "'" + newName.Replace("'", "''") + "'";
                newName = Identifier.EncodeIdentifier(newName);

                ISourceInformation primarySourceInformation = column.PrimarySource;
                string sourceName = primarySourceInformation.SourceName;
                SqlTable table = (SqlTable)column.Parent;

                if (primarySourceInformation != null && !string.IsNullOrWhiteSpace(primarySourceInformation.SourceName))
                {
                    ResolveUserDefinedTypes((SqlSchemaModel)column.Model);
                    ResolveColumnAndComposingChildren(column);
                    ResolvePotentialIncoming(table, column, sourceName);

                    SortedDictionary<string, SortedSet<SqlIntegerRange>> locationsRanges = new SortedDictionary<string, SortedSet<SqlIntegerRange>>();

                    ColumnDefinition unnamedTimestampColumnAST;
                    if (!IsUnnamedTimestampColumn(column, out unnamedTimestampColumnAST))
                    {
                        SortedSet<SqlIntegerRange> primaryLocationRange = new SortedSet<SqlIntegerRange>(new SqlIntegerRangeOverlapComparer());
                        primaryLocationRange.Add(new SqlIntegerRange(primarySourceInformation.Offset, primarySourceInformation.Offset + primarySourceInformation.Length));
                        locationsRanges.Add(sourceName, primaryLocationRange);
                    }

                    foreach (IModelRelationshipEntry entry in column.GetReferencingRelationshipEntries())
                    {
                        if (IsRelevantColumnReference(table, entry))
                        {
                            ISqlModelElement element = (ISqlModelElement)entry.FromElement;
                            foreach (ISourceInformation entrySourceInformation in element.GetRelationshipEntrySources(entry))
                            {
                                string entrySourceName = entrySourceInformation.SourceName;
                                SortedSet<SqlIntegerRange> locationRange = null;
                                if (!locationsRanges.TryGetValue(entrySourceName, out locationRange))
                                {
                                    locationRange = new SortedSet<SqlIntegerRange>(new SqlIntegerRangeOverlapComparer());
                                    locationsRanges.Add(entrySourceName, locationRange);
                                }
                                locationRange.Add(new SqlIntegerRange(entrySourceInformation.Offset, entrySourceInformation.Offset + entrySourceInformation.Length));
                            }
                        }
                    }

                    foreach (KeyValuePair<string, SortedSet<SqlIntegerRange>> pair in locationsRanges)
                    {
                        locations.Add(AnalyzeScriptForColumn(column, pair.Key, newName, stringLiteralName, pair.Value));
                    }

                    if (unnamedTimestampColumnAST != null)
                    {
                        SqlScriptUpdateInfo updateInfo = null;
                        foreach (SqlScriptUpdateInfo info in locations)
                        {
                            if (String.Compare(info.ScriptCacheIdentifier, sourceName, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                updateInfo = info;
                                break;
                            }
                        }

                        if (updateInfo == null)
                        {
                            updateInfo = new SqlScriptUpdateInfo(sourceName);
                            locations.Add(updateInfo);
                        }

                        updateInfo.AddUpdate(primarySourceInformation.Offset, primarySourceInformation.StartLine, primarySourceInformation.StartColumn, 0, string.Concat(newName, SqlModelUpdaterConstants.Space));
                    }
                }
            }

            return locations;
        }

        private static bool IsUnnamedTimestampColumn(SqlColumn column, out ColumnDefinition timestampColumnAST)
        {
            timestampColumnAST = null;
            ISourceInformation primarySourceInformation = column.PrimarySource;

            if (primarySourceInformation != null)
            {
                ColumnDefinition columnAst = primarySourceInformation.ScriptDom as ColumnDefinition;

                if (columnAst != null &&
                    columnAst.DataType == null &&
                    columnAst.ColumnIdentifier != null &&
                    column.Model.Comparer.Compare(columnAst.ColumnIdentifier.Value, SqlCommonConstants.SqlBuiltInTypeNames.Timestamp) == 0)
                {
                    timestampColumnAST = columnAst;
                    return true;
                }
            }

            return false;
        }

        private static bool IsRelevantColumnReference(SqlTable table, IModelRelationshipEntry entry)
        {
            bool result = false;
            if (_columnRelationshipClasses.Contains(entry.RelationshipClass))
            {
                // If it's a dml trigger, check that the trigger is on the columns table, otherwise accept
                if (entry.RelationshipClass == SqlDmlTrigger.BodyDependenciesClass)
                {
                    SqlDmlTrigger trigger = (SqlDmlTrigger)entry.FromElement;
                    // We need to resolve the trigger if it isn't already.
                    if (trigger.Parent == null && trigger.ResolutionStatus == ModelElementResolutionStatus.ResolveCandidate)
                    {
                        ((SqlSchemaModel)table.Model).Resolve(trigger);
                    }
                    result = trigger.Parent == table;
                }
                else if (entry.RelationshipClass == SqlComputedColumn.ExpressionDependenciesClass)
                {
                    SqlComputedColumn column = (SqlComputedColumn)entry.FromElement;
                    result = column.Parent == table;
                }
                else
                {
                    result = true;
                }
            }

            return result;
        }

        private static void ResolveUserDefinedTypes(SqlSchemaModel model)
        {
            foreach (SqlUserDefinedType type in model.GetElements<SqlUserDefinedType>(ModelElementQueryFilter.Internal))
            {
                model.Resolve(type);
            }
        }

        private static void ResolveColumnAndComposingChildren(SqlColumn column)
        {
            SqlSchemaModel model = (SqlSchemaModel)column.Model;
            model.Resolve(column);
            foreach (IModelElement child in column.GetComposingChildren())
            {
                model.Resolve(child);
            }
        }

        private static void ResolvePotentialIncoming(SqlTable table, SqlColumn column, string sourceName)
        {
            HashSet<IModelElement> elements = new HashSet<IModelElement>();
            SqlSchemaModel model = (SqlSchemaModel)column.Model;

            foreach (SqlModelBuilderResolvableAnnotation annotation in model.GetAllAnnotations<SqlModelBuilderResolvableAnnotation>(column.Name))
            {
                foreach (IModelRelationshipEntry entry in annotation.GetRelationshipEntries())
                {
                    ISqlModelElement element = (ISqlModelElement)entry.FromElement;
                    if (element.ResolutionStatus == ModelElementResolutionStatus.ResolveCandidate
                        && !elements.Contains(element)
                        && IsRelevantColumnReference(table, entry))
                    {
                        elements.Add(element);
                    }                
                }
            }

            foreach (IModelElement element in elements)
            {
                model.Resolve(element);
            }
        }

        private static SqlScriptUpdateInfo AnalyzeScriptForColumn(
            ISqlModelElement column, 
            string sourceName, 
            string newName,
            string stringLiteralName,
            SortedSet<SqlIntegerRange> locationRanges)
        {
            SqlSchemaModel model = (SqlSchemaModel)column.Model;
            TSqlParser parser = model.GetParser(column);
            SqlScriptUpdateInfo locations = null;

            using (StringReader reader = new StringReader(model.ScriptCache.GetScript(sourceName)))
            {
                IList<ParseError> errors;
                TSqlScript script = parser.Parse(reader, out errors) as TSqlScript;

                if (script != null)
                {
                    SqlInterpreterConstructor interpreterConstructor = model.DatabaseSchemaProvider.GetServiceConstructor<SqlInterpreterConstructor>();
                    interpreterConstructor.Comparer = model.Comparer;
                    SqlInterpreter interpreter = interpreterConstructor.ConstructService();

                    locations = new SqlScriptUpdateInfo(sourceName);

                    foreach (TSqlBatch batch in SqlModelBuilder.GetBatchesWithoutErrors(script, errors))
                    {
                        if (batch.Statements.Count == 1)
                        {
                            TSqlStatement statement = batch.Statements[0];
                            IList<InterpretationError> errorList = null;

                            SingleFileTableColumnLocatorSchemaAnalyzer analyzer = new SingleFileTableColumnLocatorSchemaAnalyzer(
                                column.Model,
                                locationRanges,
                                newName,
                                stringLiteralName,
                                column.Name.Parts);

                            interpreter.Interpret(batch, analyzer, out errorList);

                            foreach (SqlScriptUpdateItem update in analyzer.Updates)
                            {
                                locations.AddUpdate(update);
                            }
                        }
                    }
                }
            }
            return locations;
        }
    }
}
