//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
{
    internal sealed class ConditionParser : XmlPlanHierarchyParser
    {
        /// <summary>
        /// Enumerates FunctionType blocks and removes all items from UDF property.
        /// </summary>
        /// <param name="parsedItem">The item being parsed.</param>
        /// <returns>Enumeration.</returns>
        public override IEnumerable<FunctionTypeItem> ExtractFunctions(object parsedItem)
        {
            StmtCondTypeCondition condition = parsedItem as StmtCondTypeCondition;
            if (condition != null && condition.UDF != null)
            {
                foreach (FunctionType function in condition.UDF)
                {
                    yield return new FunctionTypeItem(function, FunctionTypeItem.ItemType.Udf);
                }

                condition.UDF = null;
            }
        }

        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        private ConditionParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static ConditionParser conditionParser = null;
        public static new ConditionParser Instance
        {
            get
            {
                if (conditionParser == null)
                {
                    conditionParser = new ConditionParser();
                }
                return conditionParser;
            }
        }
    }
}
