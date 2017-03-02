//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    internal class CommaSeparatedListFormatter : ASTNodeFormatterT<SqlCodeObject>
    {
        private bool PlaceEachElementOnNewLine { get; set; }

        internal CommaSeparatedListFormatter(FormatterVisitor visitor, SqlCodeObject codeObject, bool placeEachElementOnNewLine)
            : base(visitor, codeObject)
        {
            PlaceEachElementOnNewLine = placeEachElementOnNewLine;
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            IncrementIndentLevel();

            NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
            if (PlaceEachElementOnNewLine)
            {
                f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
            }

            for (int i = startTokenNumber; i < firstChildStartTokenNumber; i++)
            {
                SimpleProcessToken(i, f);
            }
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            DecrementIndentLevel();
            ProcessTokenRange(lastChildEndTokenNumber, endTokenNumber);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            int start = previousChild.Position.endTokenNumber;
            int end = nextChild.Position.startTokenNumber;

            bool foundWhitespaceTokenBeforeComma;
            int commaToken = FindCommaToken(start, end, out foundWhitespaceTokenBeforeComma);

            if (foundWhitespaceTokenBeforeComma)
            {
                ProcessTokenRange(start, commaToken);
            }
            else
            {
                // strip whitespace before comma
                for (int i = start; i < commaToken; i++)
                {
                    SimpleProcessToken(i, FormatterUtilities.StripAllWhitespace);
                }
            }

            if (FormatOptions.PlaceCommasBeforeNextStatement)
            {
                ProcessCommaListWithCommaMove(commaToken, end, foundWhitespaceTokenBeforeComma);
            }
            else
            {
                ProcessCommaList(commaToken, end);
            }
        }

        private void ProcessCommaListWithCommaMove(int commaToken, int end, bool foundWhitespaceTokenBeforeComma)
        {
            // Strip comma from current position
            TokenData token = GetTokenData(commaToken);
            AddReplacement(token.StartIndex, ",", string.Empty);

            // Process text between comma and next token

            // special case if there is no white space between comma token and end of region
            if (commaToken + 1 == end)
            {
                // Only handle this case if there is no whitespace before the comma. Otherwise can
                // ignore as we'll process & squash whitespace
                if (!foundWhitespaceTokenBeforeComma)
                {
                    AddNewlineOrSpace(end);
                }
            }
            else
            {
                ProcessWhitespaceInRange(commaToken, end);

            }

            // Add the comma just before the next token
            SimpleProcessToken(commaToken, FormatterUtilities.NormalizeNewLinesInWhitespace);
            TokenData tok = GetTokenData(end);
            AddReplacement(tok.StartIndex, string.Empty, ",");
        }

        private void ProcessCommaList(int commaToken, int end)
        {
            ProcessTokenRange(commaToken, commaToken + 1);

            // special case if there is no white space between comma token and end of region
            if (commaToken + 1 == end)
            {
                AddNewlineOrSpace(end);
            }
            else
            {
                ProcessWhitespaceInRange(commaToken, end);
            }
        }

        private void ProcessWhitespaceInRange(int commaToken, int end)
        {
            NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
            if (PlaceEachElementOnNewLine)
            {
                f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
            }

            for (int i = commaToken + 1; i <= end; i++)
            {
                SimpleProcessToken(i, f);
            }
        }

        private void AddNewlineOrSpace(int end)
        {
            string newValue = PlaceEachElementOnNewLine ? Environment.NewLine + GetIndentString() : " ";
            AddReplacement(
                GetTokenData(end).StartIndex,
                string.Empty,
                newValue);
        }

        private int FindCommaToken(int start, int end, out bool foundWhitespaceTokenBeforeComma)
        {
            foundWhitespaceTokenBeforeComma = false;
            int commaToken = -1;

            for (int i = start; i < end && HasToken(i); i++)
            {
                TokenData td = GetTokenData(i);
                if (td.TokenId == FormatterTokens.TOKEN_COMMA)
                {
                    commaToken = i;
                    break;
                }
                else if (IsTokenWhitespace(td))
                {
                    foundWhitespaceTokenBeforeComma = true;
                }
            }

            Debug.Assert(commaToken > -1, "No comma separating the children.");
            return commaToken;
        }
    }
}
