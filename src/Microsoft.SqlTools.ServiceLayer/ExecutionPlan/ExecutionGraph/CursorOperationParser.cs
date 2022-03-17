//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecutionGraph
{
    /// <summary>
    /// Parses StmtCursorType ShowPlan XML nodes
    /// </summary>
    internal class CursorOperationParser : XmlPlanParser
    {
        /// <summary>
        /// Creates new node and adds it to the graph.
        /// </summary>
        /// <param name="item">Item being parsed.</param>
        /// <param name="parentItem">Parent item.</param>
        /// <param name="parentNode">Parent node.</param>
        /// <param name="context">Node builder context.</param>
        /// <returns>The node that corresponds to the item being parsed.</returns>
        public override Node GetCurrentNode(object item, object parentItem, Node parentNode, NodeBuilderContext context)
        {
            return NewNode(context);
        }

        /// <summary>
        /// Determines Operation that corresponds to the object being parsed.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Operation that corresponds to the node.</returns>
        protected override Operation GetNodeOperation(Node node)
        {
            object cursorOperationName = node["OperationType"];

            Operation cursorOperation = cursorOperationName != null
                ? OperationTable.GetPhysicalOperation(cursorOperationName.ToString())
                : Operation.Unknown;


            return cursorOperation;
        }

        /// <summary>
        /// Determines node subtree cost from existing node properties.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Node subtree cost.</returns>
        protected override double GetNodeSubtreeCost(Node node)
        {
            // This node doesn't have subtree cost, so it
            // will be determined based on child nodes.
            return 0;
        }

        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        private CursorOperationParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static CursorOperationParser cursorOperationParser = null;
        public static CursorOperationParser Instance
        {
            get
            {
                if (cursorOperationParser == null)
                {
                    cursorOperationParser = new CursorOperationParser();
                }
                return cursorOperationParser;
            }
        }
    }
}
