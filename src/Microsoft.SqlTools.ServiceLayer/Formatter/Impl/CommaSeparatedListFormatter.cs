//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

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

            bool foundNonWhitespaceTokenBeforeComma = false;
            int commaToken = -1;

            for (int i = start; i < end && HasToken(i); i++)
            {
                TokenData td = GetTokenData(i);
                if (td.TokenId == 44)
                {
                    commaToken = i;
                    break;
                }
                else if (IsTokenWhitespace(td))
                {
                    foundNonWhitespaceTokenBeforeComma = true;
                }
            }

            Debug.Assert(commaToken > -1, "No comma separating the children.");

            if (foundNonWhitespaceTokenBeforeComma)
            {
                ProcessTokenRange(start, commaToken);
            }
            else
            {

#if DEBUG
                for (int i = start; i < commaToken && HasToken(i); i++)
                {
                    TokenData td = GetTokenData(i);
                    if (!IsTokenWhitespace(td))
                    {
                        Debug.Fail("unexpected token type of " + td.TokenId);
                    }
                }
#endif
                
                // strip whitespace before comma
                for (int i = start; i < commaToken; i++)
                {
                    SimpleProcessToken(i, FormatterUtilities.StripAllWhitespace);
                }
            }

            // include comma after each element?
            if (!FormatOptions.PlaceCommasBeforeNextStatement)
            {
                ProcessTokenRange(commaToken, commaToken + 1);
            }
            else
            {
                TokenData token = GetTokenData(commaToken);
                AddReplacement(new Replacement(token.StartIndex, ",", ""));
            }

            // special case if there is no white space between comma token and end of region
            if (commaToken + 1 == end)
            {
                string newValue = PlaceEachElementOnNewLine ? Environment.NewLine + GetIndentString() : " ";
                AddReplacement(new Replacement(
                    GetTokenData(end).StartIndex,
                    string.Empty,
                    newValue
                    ));
            }
            else
            {
                NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
                if (PlaceEachElementOnNewLine)
                {
                    f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
                }

                for (int i = commaToken + 1; i < end; i++)
                {
                    SimpleProcessToken(i, f);
                }

            }

            // do we need to place the comma before the next statement in the list?
            if (FormatOptions.PlaceCommasBeforeNextStatement)
            {
                SimpleProcessToken(commaToken, FormatterUtilities.NormalizeNewLinesInWhitespace);
                TokenData tok = GetTokenData(end);
                AddReplacement(new Replacement(tok.StartIndex, "", ","));
            }
        }
    }

}
