//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Management.Sql.Models;

namespace Microsoft.SqlTools.ResourceProvider.Core.Impl
{
    /// <summary>
    /// Implementation for <see cref="IAzureResource" /> using VS services. 
    /// Provides information about an Azure resource
    /// </summary>
    internal class AzureResourceWrapper : IAzureResource
    {
        /// <summary>
        /// Initializes the resource 
        /// </summary>
        public AzureResourceWrapper(TrackedResource azureResource)
        {
            CommonUtil.CheckForNull(azureResource, nameof(azureResource));
            AzureResource = azureResource;
        }

        /// <summary>
        /// Resource name
        /// </summary>
        public string Name
        {
            get
            {
                return AzureResource != null ? AzureResource.Name : string.Empty;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Resource type
        /// </summary>
        public string Type
        {
            get
            {
                return AzureResource != null ? AzureResource.Type : string.Empty;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Resource id
        /// </summary>
        public string Id
        {
            get
            {
                return AzureResource != null ? AzureResource.Id : string.Empty;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Resource Group Name
        /// </summary>
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Resource Location
        /// </summary>
        public string Location
        {
            get
            {
                return AzureResource != null ? AzureResource.Location : string.Empty;
            }
            set
            {
                if (AzureResource != null)
                {
                    AzureResource.Location = value;
                }
            }
        }

        /// <summary>
        /// The resource wrapped by this class
        /// </summary>
        protected TrackedResource AzureResource
        {
            get;
            set;
        }
    }   
}
