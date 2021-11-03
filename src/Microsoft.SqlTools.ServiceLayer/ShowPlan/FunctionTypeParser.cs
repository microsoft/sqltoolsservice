//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    internal sealed class FunctionTypeParser : XmlPlanParser
    {
        /// <summary>
        /// This function doesn't do anything. It simply returns the parent node
        /// passed it.
        /// </summary>
        /// <param name="item">Item being parsed.</param>
        /// <param name="parentItem">Parent item.</param>
        /// <param name="parentNode">Parent node.</param>
        /// <param name="context">Node builder context.</param>
        /// <returns>The node that corresponds to the item being parsed.</returns>
        public override Node GetCurrentNode(object item, object parentItem, Node parentNode, NodeBuilderContext context)
        {
            Node currentNode = NewNode(context);

            bool isStoredProcedure = false;

            if (parentItem != null)
            {
                PropertyDescriptor storedProcProperty = TypeDescriptor.GetProperties(parentItem)["StoredProc"];
                
                // If parent item has "StoredProc" property and it references the current item
                // then this item is a Stored Procedure. Otherwise it is an UDF.
                if (storedProcProperty != null && storedProcProperty.GetValue(parentItem) == item)
                {
                    isStoredProcedure = true;
                }
            }

            currentNode.Operation = isStoredProcedure ? OperationTable.GetStoredProc() : OperationTable.GetUdf();

            return currentNode;
        }

        /// <summary>
        /// Determines Operation that corresponds to the object being parsed.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Operation that corresponds to the node.</returns>
        protected override Operation GetNodeOperation(Node node)
        {

            // Node operation is defined above based on parent item.
            return null;
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
        private FunctionTypeParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static FunctionTypeParser functionTypeParser = null;
        public static FunctionTypeParser Instance
        {
            get
            {
                if (functionTypeParser == null)
                {
                    functionTypeParser = new FunctionTypeParser();
                }
                return functionTypeParser;
            }
        }
    }
}
