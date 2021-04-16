//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// A discovery provider capable of finding servers for a given  server type and category.
    /// For example: finding SQL Servers in Azure, or on the local network.
    /// Implementing classes must add a <see cref="ExportableAttribute" />
    /// to the class in order to be found by the extension manager,
    /// and to define the type and category supported
    /// </summary>  
    public interface IServerDiscoveryProvider : IExportable
    {        
        /// <summary>
        /// Discovers the server instances
        /// </summary>
        Task<ServiceResponse<ServerInstanceInfo>> GetServerInstancesAsync();            
    }
}
