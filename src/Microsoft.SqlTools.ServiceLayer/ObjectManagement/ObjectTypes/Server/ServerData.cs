//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;
using System.Diagnostics;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class ServerPrototype
    {
        #region Members

        /// <summary>
        /// data container member that contains data specific information like
        /// connection infor, SMO server object or an AMO server object as well
        /// as a hash table where one can manipulate custom data
        /// </summary>
        private CDataContainer dataContainer;
        private ServerConnection sqlConnection;
        private ServerConfigService configService;


        private ServerPrototypeData currentState;
        private ServerPrototypeData originalState;

        private ConfigProperty serverMinMemoryProperty;
        private ConfigProperty serverMaxMemoryProperty;
        #endregion

        #region Trace support
        private const string componentName = "Server";

        public string ComponentName
        {
            get
            {
                return componentName;
            }
        }
        #endregion

        #region Properties
        public string Name
        {
            get
            {
                return this.currentState.ServerName;
            }
            set
            {
                this.currentState.ServerName = value;
            }
        }

        public string Product
        {
            get
            {
                return this.currentState.Product;
            }
            set
            {
                this.currentState.Product = value;
            }
        }

        public string OperatingSystem
        {
            get
            {
                return this.currentState.OperatingSystem;
            }
            set
            {
                this.currentState.OperatingSystem = value;
            }
        }

        public string Version
        {
            get
            {
                return this.currentState.Version;
            }
            set
            {
                this.currentState.Version = value;
            }
        }

        public string Language
        {
            get
            {
                return this.currentState.Language;
            }
            set
            {
                this.currentState.Language = value;
            }
        }

        public string Platform
        {
            get
            {
                return this.currentState.Platform;
            }
            set
            {
                this.currentState.Platform = value;
            }
        }

        public int MemoryInMB
        {
            get
            {
                return this.currentState.MemoryInMB;
            }
            set
            {
                this.currentState.MemoryInMB = value;
            }
        }

        public int Processors
        {
            get
            {
                return this.currentState.Processors;
            }
            set
            {
                this.currentState.Processors = value;
            }
        }

        public string RootDirectory
        {
            get
            {
                return this.currentState.RootDirectory;
            }
            set
            {
                this.currentState.RootDirectory = value;
            }
        }

        public string ServerCollation
        {
            get
            {
                return this.currentState.ServerCollation;
            }
            set
            {
                this.currentState.ServerCollation = value;
            }
        }

        public bool IsClustered
        {
            get
            {
                return this.currentState.IsClustered;
            }
            set
            {
                this.currentState.IsClustered = value;
            }
        }

        public bool IsHadrEnabled
        {
            get
            {
                return this.currentState.IsHadrEnabled;
            }
            set
            {
                this.currentState.IsHadrEnabled = value;
            }
        }

        public bool IsXTPSupported
        {
            get
            {
                return this.currentState.IsXTPSupported;
            }
            set
            {
                this.currentState.IsXTPSupported = value;
            }
        }

        public bool IsPolyBaseInstalled
        {
            get
            {
                return this.currentState.IsPolyBaseInstalled;
            }
            set
            {
                this.currentState.IsPolyBaseInstalled = value;
            }
        }


        public string HardwareGeneration
        {
            get
            {
                return this.currentState.HardwareGeneration;
            }
            set
            {
                this.currentState.HardwareGeneration = value;
            }
        }

        public string ServiceTier
        {
            get
            {
                return this.currentState.ServiceTier;
            }
            set
            {
                this.currentState.ServiceTier = value;
            }
        }

        public int StorageSpaceUsageInMB
        {
            get
            {
                return this.currentState.StorageSpaceUsageInMB;
            }
            set
            {
                this.currentState.StorageSpaceUsageInMB = value;
            }
        }


        public int ReservedStorageSizeMB
        {
            get
            {
                return this.currentState.ReservedStorageSizeMB;
            }
            set
            {
                this.currentState.ReservedStorageSizeMB = value;
            }
        }

        public int MaxServerMemory
        {
            get
            {
                return this.currentState.MaxMemory;
            }
            set
            {
                this.currentState.MaxMemory = value;
            }
        }

        public int MinServerMemory
        {
            get
            {
                return this.currentState.MinMemory;
            }
            set
            {
                this.currentState.MinMemory = value;
            }

        }
        #endregion


        #region Constructors / Dispose

        /// <summary>
        /// ServerPrototype for editing an existing server 
        /// </summary>
        public ServerPrototype(CDataContainer context)
        {
            this.dataContainer = context;
            this.sqlConnection = context.ServerConnection;
            this.configService = new ServerConfigService();
            this.currentState = new ServerPrototypeData(context, context.Server, this.configService);
            this.originalState = (ServerPrototypeData)this.currentState.Clone();
            this.serverMaxMemoryProperty = this.configService.GetServerSmoConfig(context.Server, this.configService.MaxServerMemoryPropertyNumber);
            this.serverMinMemoryProperty = this.configService.GetServerSmoConfig(context.Server, this.configService.MinServerMemoryPropertyNumber);
        }

        #endregion

        #region Implementation: SendDataToServer()
        /// <summary>
        /// SendDataToServer
        ///
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        public void SendDataToServer()
        {
            if (this.dataContainer.Server != null)
            {
                Microsoft.SqlServer.Management.Smo.Server server = this.dataContainer.Server;
                bool changesMade = false;

                changesMade = UpdateMemoryValues(this.dataContainer.Server);

                if (changesMade)
                {
                    server.Configuration.Alter(true);
                }
            }
        }

        public bool UpdateMemoryValues(Microsoft.SqlServer.Management.Smo.Server server)
        {
            bool changesMade = false;

            if (this.currentState.MinMemory != this.originalState.MinMemory)
            {
                changesMade = true;
                ConfigProperty serverConfig = this.configService.GetServerSmoConfig(server, this.configService.MinServerMemoryPropertyNumber);
                serverConfig.ConfigValue = this.currentState.MinMemory;
            }

            if (this.currentState.MaxMemory != this.originalState.MaxMemory)
            {
                changesMade = true;
                ConfigProperty serverConfig = this.configService.GetServerSmoConfig(server, this.configService.MaxServerMemoryPropertyNumber);
                serverConfig.ConfigValue = this.currentState.MaxMemory;
            }
            return changesMade;
        }
        #endregion

        public void ApplyInfoToPrototype(ServerInfo serverInfo)
        {
            this.Name = serverInfo.Name;
            this.Language = serverInfo.Language;
            this.MemoryInMB = serverInfo.MemoryInMB;
            this.OperatingSystem = serverInfo.OperatingSystem;
            this.Platform = serverInfo.Platform;
            this.Version = serverInfo.Version;
            this.Processors = serverInfo.Processors;
            this.Version = serverInfo.Version;
            this.IsClustered = serverInfo.IsClustered;
            this.IsHadrEnabled = serverInfo.IsHadrEnabled;
            this.IsPolyBaseInstalled = serverInfo.IsPolyBaseInstalled;
            this.IsXTPSupported = (bool)(serverInfo.IsXTPSupported);
            this.Product = serverInfo.Product;
            this.ReservedStorageSizeMB = (int)(serverInfo.ReservedStorageSizeMB);
            this.RootDirectory = serverInfo.RootDirectory;
            this.ServerCollation = serverInfo.ServerCollation;
            this.ServiceTier = serverInfo.ServiceTier;
            this.StorageSpaceUsageInMB = (int)(serverInfo.StorageSpaceUsageInMB);
            this.MaxServerMemory = serverInfo.MaxServerMemory;
            this.MinServerMemory = serverInfo.MinServerMemory;
        }

        /// <summary>
        /// Private class encapsulating the data that is changed by the UI.
        /// </summary>
        /// <remarks>
        /// Isolating this data allows for an easy implementation of Reset() and
        /// simplifies difference detection when committing changes to the server.
        /// </remarks>
        private class ServerPrototypeData : ICloneable
        {
            #region data members
            private string serverName = string.Empty;
            private string hardwareGeneration = String.Empty;
            private string language = String.Empty;
            private int memoryInMB = 0;
            private string operatingSystem = String.Empty;
            private string platform = String.Empty;
            private int processors = 0;
            private bool isClustered = false;
            private bool isHadrEnabled = false;
            private bool isPolyBaseInstalled = false;
            private bool isXTPSupported = false;
            private string product = String.Empty;
            private string rootDirectory = String.Empty;
            private string serverCollation = String.Empty;
            private string version = String.Empty;
            private string serviceTier = String.Empty;
            private int reservedStorageSizeMB = 0;
            private int storageSpaceUsageInMB = 0;
            private int minMemory = 0;
            private int maxMemory = 0;

            private bool initialized = false;
            private Server server;
            private CDataContainer context;
            private ServerConfigService configService;
            private bool isYukonOrLater = false;

            ConfigProperty serverMaxMemoryProperty;
            ConfigProperty serverMinMemoryProperty;
            #endregion

            #region Properties

            // General properties


            public string ServerName
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.serverName;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.serverName = value;
                }
            }

            public string HardwareGeneration
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.hardwareGeneration;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.hardwareGeneration = value;
                }
            }

            public string Language
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.language;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.language = value;
                }
            }

            public int MemoryInMB
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.memoryInMB;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.memoryInMB = value;
                }
            }

            public string OperatingSystem
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.operatingSystem;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.operatingSystem = value;
                }
            }

            public string Platform
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.platform;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.platform = value;
                }
            }

            public int Processors
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.processors;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.processors = value;
                }
            }

            public bool IsClustered
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.isClustered;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.isClustered = value;
                }
            }

            public bool IsHadrEnabled
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.isHadrEnabled;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.isHadrEnabled = value;
                }
            }

            public bool IsPolyBaseInstalled
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.isPolyBaseInstalled;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.isPolyBaseInstalled = value;
                }
            }

            public bool IsXTPSupported
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.isXTPSupported;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.isXTPSupported = value;
                }
            }


            public string Product
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.product;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.product = value;
                }
            }

            public string RootDirectory
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.rootDirectory;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.rootDirectory = value;
                }
            }

            public string ServerCollation
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.serverCollation;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.serverCollation = value;
                }
            }

            public string Version
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.version;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.version = value;
                }
            }

            public string ServiceTier
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.serviceTier;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.serviceTier = value;
                }
            }

            public int StorageSpaceUsageInMB
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.storageSpaceUsageInMB;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.storageSpaceUsageInMB = value;
                }
            }


            public int ReservedStorageSizeMB
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.reservedStorageSizeMB;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }
                    this.reservedStorageSizeMB = value;
                }
            }


            public int MinMemory
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.minMemory;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }

                     this.minMemory = value; 
                }
            }

            public int MaxMemory
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.maxMemory;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Write(TraceEventType.Error, $"Unexpected property set before initialization");
                    }

                    this.maxMemory = value;
                }
            }


            public Microsoft.SqlServer.Management.Smo.Server Server
            {
                get
                {
                    return this.server;
                }
            }

            public bool IsYukonOrLater
            {
                get
                {
                    return this.isYukonOrLater;
                }
            }

            #endregion

            /// <summary>
            /// private default constructor - used by Clone()
            /// </summary>
            private ServerPrototypeData()
            {
            }


            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="context">The context in which we are modifying an existing server</param>
            /// <param name="server">The server we are modifying</param>
            public ServerPrototypeData(CDataContainer context, Server server, ServerConfigService service)
            {
                this.server = context.Server;
                this.context = context;
                this.configService = service;
                this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
                this.serverMaxMemoryProperty = this.configService.GetServerSmoConfig(server, this.configService.MaxServerMemoryPropertyNumber);
                this.serverMinMemoryProperty = this.configService.GetServerSmoConfig(server, this.configService.MinServerMemoryPropertyNumber);
                LoadData();
            }

            /// <summary>
            /// Create a clone of this ServerRolePrototypeData object
            /// </summary>
            /// <returns>The clone ServerRolePrototypeData object</returns>
            public object Clone()
            {
                ServerPrototypeData result = new ServerPrototypeData();
                result.serverName = this.serverName;
                result.initialized = this.initialized;
                result.hardwareGeneration = this.hardwareGeneration;
                result.language = this.language;
                result.memoryInMB = this.memoryInMB;
                result.operatingSystem = this.operatingSystem;
                result.platform = this.platform;
                result.processors = this.processors;
                result.isClustered = this.isClustered;
                result.isHadrEnabled = this.isHadrEnabled;
                result.isPolyBaseInstalled = this.isPolyBaseInstalled;
                result.isXTPSupported = this.isXTPSupported;
                result.product = this.product;
                result.reservedStorageSizeMB = this.reservedStorageSizeMB;
                result.rootDirectory = this.rootDirectory;
                result.serverCollation = this.serverCollation;
                result.serviceTier = this.serviceTier;
                result.storageSpaceUsageInMB = this.storageSpaceUsageInMB;
                result.version = this.version;
                result.maxMemory = this.maxMemory;
                result.minMemory = this.minMemory;
                result.server = this.server;
                return result;
            }

            private void LoadData()
            {
                this.initialized = true;
                this.serverName = server.Name;
                this.hardwareGeneration = server.HardwareGeneration;
                this.language = server.Language;
                this.memoryInMB = server.PhysicalMemory;
                this.operatingSystem = server.HostDistribution;
                this.platform = server.HostPlatform;
                this.processors = server.Processors;
                this.isClustered = server.IsClustered;
                this.isHadrEnabled = server.IsHadrEnabled;
                this.isPolyBaseInstalled = server.IsPolyBaseInstalled;
                this.isXTPSupported = server.IsXTPSupported;
                this.product = server.Product;
                this.rootDirectory = server.RootDirectory;
                this.serverCollation = server.Collation;
                this.version = server.VersionString;
                this.reservedStorageSizeMB = server.ReservedStorageSizeMB;
                this.serviceTier = server.ServiceTier;
                this.storageSpaceUsageInMB = server.UsedStorageSizeMB;
                this.maxMemory = serverMaxMemoryProperty.ConfigValue;
                this.minMemory = serverMinMemoryProperty.ConfigValue;
            }
        }
    }
}
