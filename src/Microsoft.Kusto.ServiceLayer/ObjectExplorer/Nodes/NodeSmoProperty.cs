//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// Has information for SMO object properties to be loaded with the SMO object
    /// </summary>
    public class NodeSmoProperty
    {
        /// <summary>
        /// Property name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates which platforms a filter is valid for
        /// </summary>
        public ValidForFlag ValidFor { get; set; }
    }
}
