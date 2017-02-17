//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    internal class PaddedSpaceSeparatedListFormatter : SpaceSeparatedListFormatter
    {
        private List<ColumnSpacingFormatDefinition> ColumnSpacingDefinitions { get; set; }
        private int nextColumn = 0;


        internal PaddedSpaceSeparatedListFormatter(FormatterVisitor visitor, SqlCodeObject codeObject, List<ColumnSpacingFormatDefinition> spacingDefinitions, bool incrementIndentLevelOnPrefixRegion)
            : base(visitor, codeObject, incrementIndentLevelOnPrefixRegion)
        {
            ColumnSpacingDefinitions = spacingDefinitions;
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            // first, figure out how big to make the pad
            int padLength = 1;
            if (ColumnSpacingDefinitions != null && nextColumn < ColumnSpacingDefinitions.Count)
            {
                if (previousChild.GetType() == ColumnSpacingDefinitions[nextColumn].PreviousType &&
                    (ColumnSpacingDefinitions[nextColumn].NextType == null || nextChild.GetType() == ColumnSpacingDefinitions[nextColumn].NextType))
                {
                    string text = previousChild.TokenManager.GetText(previousChild.Position.startTokenNumber, previousChild.Position.endTokenNumber);
                    int stringLength = text.Length;
                    padLength = ColumnSpacingDefinitions[nextColumn].PaddedLength - stringLength;

                    Debug.Assert(padLength > 0, "unexpected value for Pad Length");
                    padLength = Math.Max(padLength, 1);

                    ++nextColumn;
                }
            }
            // next, normalize the tokens
            int start = previousChild.Position.endTokenNumber;
            int end = nextChild.Position.startTokenNumber;

            for (int i = start; i < end; i++)
            {
                SimpleProcessToken(i, (string original, FormatContext context) => { return FormatterUtilities.NormalizeNewLinesOrCondenseToNSpaces(original, context, padLength); });
            }

        }

        internal class ColumnSpacingFormatDefinition
        {
            internal ColumnSpacingFormatDefinition(Type previousType, Type nextType, int padLength)
            {
                PreviousType = previousType;
                NextType = nextType;
                PaddedLength = padLength;
            }

            internal Type PreviousType { get; private set; }
            internal Type NextType { get; private set; }
            internal int PaddedLength { get; private set; }
        }
    }
}
