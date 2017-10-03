//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// Contains information about a server discovered by <see cref="IServerDiscoveryProvider" />
    /// </summary>
    public class ServerInstanceInfo
    {
        /// <summary>
        /// Initializes the new instance with server and location
        /// </summary>
        public ServerInstanceInfo(IServerDefinition serverDefinition)
        {
            ServerDefinition = serverDefinition;
        }

        public ServerInstanceInfo()
        {            
        }

        public IServerDefinition ServerDefinition
        {
            get; private set; 
        }

        /// <summary>
        /// Server Name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Fully qualified domain name
        /// </summary>
        public string FullyQualifiedDomainName
        {
            get;
            set;
        }

        /// <summary>
        /// Administrator Login
        /// </summary>
        public string AdministratorLogin
        {
            get;
            set;
        }
    }
}
