//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlViewDefinitionFormatterFactory : ASTNodeFormatterFactoryT<SqlViewDefinition>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlViewDefinition codeObject)
        {
            return new SqlViewDefinitionFormatter(visitor, codeObject);
        }
    }

    class SqlViewDefinitionFormatter : SysCommentsFormatterBase<SqlViewDefinition>
    {

        internal SqlViewDefinitionFormatter(FormatterVisitor visitor, SqlViewDefinition sqlCodeObject)
            : base(visitor, sqlCodeObject)
        {
        }

        protected override bool ShouldPlaceEachElementOnNewLine()
        {
            return true;
        }

        public override void Format()
        {
            LexLocation loc = CodeObject.Position;

            SqlCodeObject firstChild = CodeObject.Children.FirstOrDefault();
            if (firstChild != null)
            {
                //
                // format the text from the start of the object to the start of its first child
                //
                LexLocation firstChildStart = firstChild.Position;
                ProcessPrefixRegion(loc.startTokenNumber, firstChildStart.startTokenNumber);

                ProcessChild(firstChild);

                // keep track of the next token to process
                int nextToken = firstChildStart.endTokenNumber;

                // process the columns if available
                nextToken = ProcessColumns(nextToken);

                // process options if available
                nextToken = ProcessOptions(nextToken);

                // process the region containing the AS token
                nextToken = ProcessAsToken(nextToken, indentAfterAs: true);

                // process the query with clause if present
                nextToken = ProcessQueryWithClause(nextToken);

                // process the query expression
                nextToken = ProcessQueryExpression(nextToken);

                DecrementIndentLevel();

                // format text from end of last child to end of object.
                SqlCodeObject lastChild = CodeObject.Children.LastOrDefault();
                Debug.Assert(lastChild != null, "last child is null.  Need to write code to deal with this case");
                ProcessSuffixRegion(lastChild.Position.endTokenNumber, loc.endTokenNumber);
            }
            else
            {
                // no children
                Visitor.Context.ProcessTokenRange(loc.startTokenNumber, loc.endTokenNumber);
            }
        }

        private int ProcessColumns(int nextToken)
        {
            if (CodeObject.ColumnList != null && CodeObject.ColumnList.Count > 0)
            {
                nextToken = ProcessSectionInsideParentheses(nextToken, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum,
                    isNewlineRequired: true,
                    processSection: (n) => ProcessColumnList(n, CodeObject.ColumnList, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum));
            }
            return nextToken;
        }

        private int ProcessOptions(int nextToken)
        {
            if (CodeObject.Options != null && CodeObject.Options.Count > 0)
            {
                int withTokenIndex = FindTokenWithId(nextToken, FormatterTokens.TOKEN_WITH);

                // Preprocess
                ProcessTokenRangeEnsuringOneNewLineMinumum(nextToken, withTokenIndex);

                nextToken = ProcessWithStatementStart(nextToken, withTokenIndex);

                nextToken = ProcessOptionsSection(nextToken);

                DecrementIndentLevel();
            }
            return nextToken;
        }

        private int ProcessOptionsSection(int nextToken)
        {
            // find where the options start
            IEnumerator<SqlModuleOption> optionEnum = CodeObject.Options.GetEnumerator();
            if (optionEnum.MoveNext())
            {
                ProcessAndNormalizeWhitespaceRange(nextToken, optionEnum.Current.Position.startTokenNumber,
                    FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);

                // Process options
                ProcessChild(optionEnum.Current);
                SqlModuleOption previousOption = optionEnum.Current;
                while (optionEnum.MoveNext())
                {
                    CommaSeparatedList.ProcessInterChildRegion(previousOption, optionEnum.Current);
                    ProcessChild(optionEnum.Current);
                    previousOption = optionEnum.Current;
                }
                nextToken = previousOption.Position.endTokenNumber;
            }

            return nextToken;
        }

        private int ProcessQueryWithClause(int nextToken)
        {
            return ProcessQuerySection(nextToken, CodeObject.QueryWithClause);
        }

        private int ProcessQueryExpression(int nextToken)
        {
            return ProcessQuerySection(nextToken, CodeObject.QueryExpression);
        }        
    }
}
