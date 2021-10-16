//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater;
using Microsoft.Data.Tools.Schema.Utilities.Sql.Common;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Sql.DesignServices
{
    internal class SingleFileTableColumnLocatorSchemaAnalyzer : SqlSchemaAnalyzer
    {
        private readonly List<SqlScriptUpdateItem> _updates;
        private readonly SortedSet<SqlIntegerRange> _locationRanges;
        private readonly SqlElementDescriptor _descriptor;
        private readonly SqlElementDescriptorComparer _comparer;
        private readonly string _newColumnName;
        private readonly string _newColumnNameForStringLiteral;

        public IList<SqlScriptUpdateItem> Updates { get { return _updates; } }

        public SingleFileTableColumnLocatorSchemaAnalyzer(
            ModelStore model, 
            SortedSet<SqlIntegerRange> locationRanges,
            string newColumnName,
            string newColumnNameForStringLiteral,
            IEnumerable<string> identifiers)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            if (locationRanges == null)
            {
                throw new ArgumentNullException("locationRanges");
            }
            if (string.IsNullOrWhiteSpace(newColumnName))
            {
                throw new ArgumentOutOfRangeException("newColumnName");
            }
            if (string.IsNullOrWhiteSpace(newColumnNameForStringLiteral))
            {
                throw new ArgumentOutOfRangeException("newColumnNameForStringLiteral");
            }

            if (identifiers == null)
            {
                throw new ArgumentNullException("identifiers");
            }

            _updates = new List<SqlScriptUpdateItem>();
            _locationRanges = locationRanges;
            _descriptor = new SqlElementDescriptor(typeof(SqlColumn), identifiers);
            _comparer = new SqlElementDescriptorComparer(model.Comparer, model.Schema);
            _newColumnName = newColumnName;
            _newColumnNameForStringLiteral = newColumnNameForStringLiteral;
        }

        public override void VisitFragment(TSqlFragment fragment, SqlElementDescriptor sqlElementDescriptor, SqlElementDescriptorRelevance relevance)
        {
            if (relevance == SqlElementDescriptorRelevance.SelfId
                && sqlElementDescriptor != null
                && IsValidReference(fragment, sqlElementDescriptor))
            {
                SqlPotentialElementDescriptor potential = sqlElementDescriptor as SqlPotentialElementDescriptor;
                if (potential != null)
                {
                    ProcessExpression(fragment, potential.ClrParts.Count);
                }
                else
                {
                    ProcessExpression(fragment, 0);
                }
            }
        }

        public override void VisitAmbiguousFragment(TSqlFragment fragment, IEnumerable<SqlPotentialElementDescriptor> possibilities)
        {
            foreach (SqlPotentialElementDescriptor potential in possibilities)
            {
                if (potential != null
                    && IsValidReference(fragment, potential))
                {
                    ProcessExpression(fragment, potential.ClrParts.Count);
                }
            }
        }

        private bool IsValidReference(TSqlFragment fragment, SqlElementDescriptor sqlElementDescriptor)
        {
            return _comparer.IsSqlElementDescriptorEqual(_descriptor, sqlElementDescriptor)
                && _locationRanges.Contains(new SqlIntegerRange(fragment.StartOffset, fragment.StartOffset + fragment.FragmentLength));
        }

        private void ProcessExpression(TSqlFragment fragment, int clrPartsCount)
        {
            while (true)
            {
                FunctionCall functionCall = fragment as FunctionCall;
                UserDefinedTypePropertyAccess propertyAccess;
                SelectScalarExpression selectScalarExpression;
                ExpressionCallTarget expressionCallTarget;
                ParenthesisExpression parenthesisExpression;
                if (functionCall != null)
                {
                    fragment = functionCall.CallTarget;
                    --clrPartsCount;
                }
                else if ((propertyAccess = fragment as UserDefinedTypePropertyAccess) != null)
                {
                    fragment = propertyAccess.CallTarget;
                    --clrPartsCount;
                }
                else if ((selectScalarExpression = fragment as SelectScalarExpression) != null)
                {
                    fragment = selectScalarExpression.Expression;
                }
                else if ((expressionCallTarget = fragment as ExpressionCallTarget) != null)
                {
                    fragment = expressionCallTarget.Expression;
                }
                else if ((parenthesisExpression = fragment as ParenthesisExpression) != null)
                {
                    fragment = parenthesisExpression.Expression;
                }
                else
                {
                    break;
                }
            }
            SqlTracer.AssertTraceEvent(clrPartsCount >= 0, TraceEventType.Error, SqlTraceId.CoreServices,
                "Expecting 0 or greater for clr parts");

            ColumnReferenceExpression columnRef = fragment as ColumnReferenceExpression;
            Identifier identifier;
            StringLiteral literal;
            if (columnRef != null)
            {
                int index = columnRef.MultiPartIdentifier.Identifiers.Count - clrPartsCount - 1;
                fragment = columnRef.MultiPartIdentifier.Identifiers[index];
                AddUpdate(fragment);
            }
            else if ((identifier = fragment as Identifier) != null)
            {
                SqlTracer.AssertTraceEvent(clrPartsCount == 0, TraceEventType.Error, SqlTraceId.CoreServices,
                    "Expecting 0 count");
                fragment = identifier;
                AddUpdate(fragment);
            }
            else if ((literal = fragment as StringLiteral) != null)
            {
                SqlTracer.AssertTraceEvent(clrPartsCount == 0, TraceEventType.Error, SqlTraceId.CoreServices,
                    "Expecting 0 count");
                fragment = literal;
                AddUpdate(fragment, stringLiteral: true);
            }
            else
            {
                MultiPartIdentifierCallTarget multiPartIdentifierCallTarget = fragment as MultiPartIdentifierCallTarget;
                SqlTracer.AssertTraceEvent(multiPartIdentifierCallTarget != null, TraceEventType.Error, SqlTraceId.CoreServices,
                    "Expecting MultiPartIdentifierCallTarget");
                if (multiPartIdentifierCallTarget != null)
                {
                    int index = multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers.Count - clrPartsCount - 1;
                    fragment = multiPartIdentifierCallTarget.MultiPartIdentifier.Identifiers[index];
                    AddUpdate(fragment);
                }
            }
        }

        private void AddUpdate(TSqlFragment fragment, bool stringLiteral = false)
        {
            SqlScriptUpdateItem item = new SqlScriptUpdateItem(
                fragment.StartOffset, 
                fragment.StartLine, 
                fragment.StartColumn, 
                fragment.FragmentLength, 
                stringLiteral ? _newColumnNameForStringLiteral :  _newColumnName);
            _updates.Add(item);
        }
    }
}
