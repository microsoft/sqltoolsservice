//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// Defines a server grouping based on the type of server connection supported (SQL Server, Reporting Server, Analysis Server) 
    /// and a Category by which these should be shown to the user. 
    /// Built in examples of categories include Local, Network, and Azure and additional categories can be defined as needed. 
    /// Note that the Connection Dialog UI may require Category to be set for some resource types such as<see cref="IServerDiscoveryProvider" />. 
    /// In addition a UI section matching that category may be required, or else the provider will not be used by any UI part and never be called.
    /// </summary>
    public interface IServerDefinition
    {
        /// <summary>
        /// Category by which resources can be grouped. Built in examples of categories include Local, Network, and Azure and additional categories can be defined as needed. 
        /// </summary>
        string Category
        {
            get;
        }

        /// <summary>
        /// The type of server connection supported. Examples include SQL Server, Reporting Server, Analysis Server.
        /// </summary>
        string ServerType
        {
            get;
        }
    }

    /// <summary>
    /// The implementation of the server definition that implements the properties
    /// </summary>
    public sealed class ServerDefinition : IServerDefinition
    {
        private static ServerDefinition _default = new ServerDefinition(ServerTypes.SqlServer, string.Empty);

        public ServerDefinition(string serverType, string category)
        {
            ServerType = serverType;
            Category = category;
        }

        /// <summary>
        /// <see cref="IServerDefinition.ServerType"/>
        /// </summary>
        public string ServerType { private set; get; }

        /// <summary>
        /// <see cref="IServerDefinition.Category"/>
        /// </summary>
        public string Category { private set; get; }

        /// <summary>
        /// Default value for the server definition
        /// </summary>
        public static ServerDefinition Default
        {
            get { return _default; }
        }
    }
}
