//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// Interface for any implementation of azure resource
    /// </summary>
    public interface IAzureResource
    {
        /// <summary>
        /// Azure Resource Name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Azure Resource Type
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Azure Resource Id
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Resource Group Name
        /// </summary>
        string ResourceGroupName
        {
            get;
            set;
        }

        /// <summary>
        /// Resource Location
        /// </summary>
        string Location { get; set; }
    }
}
