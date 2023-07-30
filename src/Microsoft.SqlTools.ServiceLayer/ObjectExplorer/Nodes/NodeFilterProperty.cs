//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes
{

    /// <summary>
    /// The filterable properties that a node supports
    /// </summary>
    public class NodeFilterProperty
    {
        /// <summary>
        /// The name of the filter property
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The name of the filter property displayed to the user
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The description of the filter property
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The data type of the filter property
        /// </summary>
        public NodeFilterPropertyDataType Type { get; set; }
        /// <summary>
        /// The list of choices for the filter property if the type is choice
        /// </summary>
        public NodeFilterPropertyChoice[] Choices { get; set; }
    }


    /// <summary>
    /// The data type of the filter property. Matches NodeFilterPropertyDataType enum in ADS : https://github.com/microsoft/azuredatastudio/blob/main/src/sql/azdata.proposed.d.ts#L1847-L1853
    /// </summary>
    public enum NodeFilterPropertyDataType
    {
        String = 0,
        Number = 1,
        Boolean = 2,
        Date = 3,
        Choice = 4
    }

    /// <summary>
    /// The choice for the filter property if the type is choice
    /// </summary>
    public class NodeFilterPropertyChoice
    {
        /// <summary>
        /// The dropdown display value for the choice
        /// </summary>
        /// <value></value>
        public string DisplayName { get; set; }

        /// <summary>
        /// The value of the choice
        /// </summary>
        public string Value { get; set; }
    }

}