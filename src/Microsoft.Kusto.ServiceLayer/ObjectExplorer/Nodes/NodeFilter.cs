//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// Has information for filtering a SMO object by properties 
    /// </summary>
    public class NodeFilter
    {
        /// <summary>
        /// Property name
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Filter values
        /// </summary>
        public List<object> Values { get; set; }

        /// <summary>
        /// Type of the filter values
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Indicates which platforms a filter is valid for
        /// </summary>
        public ValidForFlag ValidFor { get; set; }

        /// <summary>
        /// The type of the Querier the filter can be applied to
        /// </summary>
        public Type TypeToReverse { get; set; }
    }
}
