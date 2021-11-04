//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
{
    /// <summary>
    /// Parses StmtCursorType ShowPlan XML nodes
    /// </summary>
    internal class CursorStatementParser : StatementParser
    {
        /// <summary>
        /// Determines Operation that corresponds to the object being parsed.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Operation that corresponds to the node.</returns>
        protected override Operation GetNodeOperation(Node node)
        {
            object cursorType = node["CursorActualType"];

            if (cursorType == null)
            {
                cursorType = node["StatementType"];
            }

            Operation cursor = cursorType != null
                ? OperationTable.GetCursorType(cursorType.ToString())
                : Operation.Unknown;

            return cursor;
        }

        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        private CursorStatementParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static CursorStatementParser cursorStatementParser = null;
        public static new CursorStatementParser Instance
        {
            get
            {
                if (cursorStatementParser == null)
                {
                    cursorStatementParser = new CursorStatementParser();
                }
                return cursorStatementParser;
            }
        }
    }
}
