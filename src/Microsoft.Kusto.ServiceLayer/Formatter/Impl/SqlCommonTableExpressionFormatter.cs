//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlCommonTableExpressionFormatterFactory : ASTNodeFormatterFactoryT<SqlCommonTableExpression>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlCommonTableExpression codeObject)
        {
            return new SqlCommonTableExpressionFormatter(visitor, codeObject);
        }
    }

    internal class SqlCommonTableExpressionFormatter : SysCommentsFormatterBase<SqlCommonTableExpression>
    {        

        public SqlCommonTableExpressionFormatter(FormatterVisitor visitor, SqlCommonTableExpression codeObject)
            : base(visitor, codeObject)
        {
        }

        protected override bool ShouldPlaceEachElementOnNewLine()
        {
            return FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements;
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            IncrementIndentLevel();
            base.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            DecrementIndentLevel();
            base.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }

        public override void Format()
        {
            int nextToken = ProcessExpressionName(CodeObject.Position.startTokenNumber);

            nextToken = ProcessColumns(nextToken);

            // TODO: should we indent the AS statement and then decrement indent at the end?
            nextToken = ProcessAsToken(nextToken, indentAfterAs: false);
            
            nextToken = ProcessQueryExpression(nextToken);

        }

        private int ProcessQueryExpression(int nextToken)
        {
            NormalizeWhitespace normalizer = GetColumnWhitespaceNormalizer();
            nextToken = ProcessSectionInsideParentheses(nextToken,
                normalizer: FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum,
                isNewlineRequired: true,
                processSection: (n) => ProcessQuerySection(n, CodeObject.QueryExpression));            
            return nextToken;
        }
        
        private int ProcessColumns(int nextToken)
        {
            if (CodeObject.ColumnList != null && CodeObject.ColumnList.Count > 0)
            {
                NormalizeWhitespace normalizer = GetColumnWhitespaceNormalizer();
                nextToken = ProcessSectionInsideParentheses(nextToken, normalizer,
                    isNewlineRequired: FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements,
                    processSection: (n) => ProcessColumnList(n, CodeObject.ColumnList, normalizer));
            }
            return nextToken;            
        }
                
        private NormalizeWhitespace GetColumnWhitespaceNormalizer()
        {
            if (FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements)
            {
                return FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
            }
            return FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
        }

        private int ProcessExpressionName(int nextToken)
        {
            SqlIdentifier name = CodeObject.Name;
            for (int i = nextToken; i < name.Position.startTokenNumber; i++)
            {
                ProcessTokenEnsuringOneNewLineMinimum(i);
            }
            
            ProcessTokenRange(name.Position.startTokenNumber, name.Position.endTokenNumber);
            
            nextToken = name.Position.endTokenNumber;

            return nextToken;
        }
    }
}
