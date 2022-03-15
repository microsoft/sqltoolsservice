//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    internal class XmlPlanHierarchyParser : XmlPlanParser
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
            return parentNode;
        }

        /// <summary>
        /// Extracts FunctionType blocks.
        /// </summary>
        /// <param name="parsedItem">The item being parsed.</param>
        /// <returns>Enumeration.</returns>
        public override IEnumerable<FunctionTypeItem> ExtractFunctions(object parsedItem)
        {
            // Recursively call ExtractFunctions for each children.
            foreach (object item in GetChildren(parsedItem))
            {
                XmlPlanParser parser = XmlPlanParserFactory.GetParser(item.GetType());
                foreach (FunctionTypeItem functionItem in parser.ExtractFunctions(item))
                {
                    yield return functionItem;
                }
            }
        }

        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        protected XmlPlanHierarchyParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static XmlPlanHierarchyParser xmlPlanHierarchyParser = null;
        public static XmlPlanHierarchyParser Instance
        {
            get
            {
                if (xmlPlanHierarchyParser == null)
                {
                    xmlPlanHierarchyParser = new XmlPlanHierarchyParser();
                }
                return xmlPlanHierarchyParser;
            }
        }
    }
}
