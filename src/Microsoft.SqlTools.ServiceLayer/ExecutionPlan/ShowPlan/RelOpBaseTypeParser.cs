//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using System.Collections;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Parses ShowPlan XML objects derived from RelOpBaseType type
    /// </summary>
    internal class RelOpBaseTypeParser : XmlPlanParser
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
        /// Enumerates children items of the item being parsed.
        /// </summary>
        /// <param name="parsedItem">The item being parsed.</param>
        /// <returns>Enumeration.</returns>
        public override IEnumerable GetChildren(object parsedItem)
        {
            PropertyDescriptor relOpProperty = TypeDescriptor.GetProperties(parsedItem)["RelOp"];
            if (relOpProperty != null)
            {
                object value = relOpProperty.GetValue(parsedItem);
                if (value != null)
                {
                    if (value is IEnumerable)
                    {
                        foreach (object item in (IEnumerable)value)
                        {
                            yield return item;
                        }
                    }
                    else
                    {
                        yield return value;
                    }
                }
            }

            yield break;
        }

        /// <summary>
        /// Protected constructor prevents this object from being externally instantiated
        /// </summary>
        protected RelOpBaseTypeParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static RelOpBaseTypeParser relOpBaseTypeParser = null;
        public static RelOpBaseTypeParser Instance
        {
            get
            {
                if (relOpBaseTypeParser == null)
                {
                    relOpBaseTypeParser = new RelOpBaseTypeParser();
                }
                return relOpBaseTypeParser;
            }
        }
    }
}