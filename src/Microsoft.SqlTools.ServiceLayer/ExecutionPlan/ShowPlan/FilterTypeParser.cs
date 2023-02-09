//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Parses ShowPlan XML objects derived from RelOpBaseType type
    /// </summary>
    internal sealed class FilterTypeParser : RelOpBaseTypeParser
    {
        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        private FilterTypeParser()
        {
        }

        /// <summary>
        /// Updates node special properties.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        public override void ParseProperties(object parsedItem, PropertyDescriptorCollection targetPropertyBag, NodeBuilderContext context)
        {
            base.ParseProperties(parsedItem, targetPropertyBag, context);

            FilterType item = parsedItem as FilterType;
            Debug.Assert(item != null, "FilterType object expected");

            if (item.StartupExpression)
            {
                // If the filter has Predicate property, it has to be renamed to 
                // Startup Expression Predicate
                PropertyValue property = targetPropertyBag["Predicate"] as PropertyValue;
                if (property != null)
                {
                    property.SetDisplayNameAndDescription(SR.StartupExpressionPredicate, null);
                }
            }
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static FilterTypeParser filterTypeParser = null;
        public static new FilterTypeParser Instance
        {
            get
            {
                filterTypeParser ??= new FilterTypeParser();
                return filterTypeParser;
            }
        }
    }
}