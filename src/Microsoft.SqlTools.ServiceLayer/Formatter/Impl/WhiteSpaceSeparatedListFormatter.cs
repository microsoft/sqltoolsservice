//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    /// <summary>
    /// Base class for a set of utility formatters that are used by Node-specific formatters when dealing with whitespace
    /// </summary>
    internal abstract class WhiteSpaceSeparatedListFormatter : ASTNodeFormatterT<SqlCodeObject>
    {
        private bool IncrementIndentLevelOnPrefixRegion { get; set; }

        /// <summary>
        /// This constructor initalizes the <see cref="Visitor"/> and <see cref="CodeObject"/> properties since the formatter's entry point
        /// is not the Format method
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="codeObject"></param>
        /// <param name="incrementIndentLevelOnPrefixRegion"></param>
        internal WhiteSpaceSeparatedListFormatter(FormatterVisitor visitor, SqlCodeObject codeObject, bool incrementIndentLevelOnPrefixRegion)
            : base(visitor, codeObject)
        {
            IncrementIndentLevelOnPrefixRegion = incrementIndentLevelOnPrefixRegion;
        }

        internal void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber, bool allowIncrement)
        {
            if (allowIncrement && IncrementIndentLevelOnPrefixRegion)
            {
                IncrementIndentLevel();
            }
            base.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber, true);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber, true);
        }
        internal void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber, bool allowDecrement)
        {
            if (allowDecrement && IncrementIndentLevelOnPrefixRegion)
            {
                DecrementIndentLevel();
            }
            base.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            int start = previousChild.Position.endTokenNumber;
            int end = nextChild.Position.startTokenNumber;

            if (start < end)
            {
                for (int i = start; i < end; i++)
                {
                    SimpleProcessToken(i, FormatWhitespace);
                }
            }
            else
            {
                // Insert the minimum whitespace
                string minWhite = FormatWhitespace(" ", Visitor.Context);
                int insertLocation = TokenManager.TokenList[start].StartIndex;
                AddReplacement(insertLocation, "", minWhite);
            }
        }

        internal abstract string FormatWhitespace(string original, FormatContext context);
    }
}
