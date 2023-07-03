//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// ServerGeneral - main app server page
    /// </summary>
    internal class ServerPrototype
    {
        #region Members

        /// <summary>
        /// data container member that contains data specific information like
        /// connection infor, SMO server object or an AMO server object as well
        /// as a hash table where one can manipulate custom data
        /// </summary>
        private CDataContainer dataContainer = null;
        private ServerConnection sqlConnection = null;
        private ServerConfigService configService = new ServerConfigService();


        private ServerPrototypeData currentState;
        private ServerPrototypeData originalState;
        #endregion

        #region Trace support
        private const string componentName = "ServerGeneral";

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

        public int Memory
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

        //private string m_strIsClustered = String.Empty;

        //public string IsClustered
        //{
        //    get
        //    {
        //        return m_strIsClustered;
        //    }
        //}

        //private string m_strIsHadrEnabled = String.Empty;

        //public string IsHadrEnabled
        //{
        //    get
        //    {
        //        return m_strIsHadrEnabled;
        //    }
        //}

        //private string m_strIsXTPSupported = String.Empty;

        //public string IsXTPSupported
        //{
        //    get
        //    {
        //        return m_strIsXTPSupported;
        //    }
        //}

        //private string m_strIsPolybaseInstalled = String.Empty;

        //public string IsPolybaseInstalled
        //{
        //    get
        //    {
        //        return m_strIsPolybaseInstalled;
        //    }
        //}

        //private string m_strHardwareGeneration = null;

        //public string HardwareGeneration
        //{
        //    get
        //    {
        //        return m_strHardwareGeneration;
        //    }
        //}

        //private string m_strServiceTier = null;

        //public string ServiceTier
        //{
        //    get
        //    {
        //        return m_strServiceTier;
        //    }
        //}

        //private string m_strStorageSizeUsage = null;

        //public string StorageSizeUsage
        //{
        //    get
        //    {
        //        return m_strStorageSizeUsage;
        //    }
        //}

        //private string m_strStorageSizeReserved = null;

        //public string StorageSizeReserved
        //{
        //    get
        //    {
        //        return m_strStorageSizeReserved;
        //    }
        //}

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
            this.currentState = new ServerPrototypeData(context, context.Server);
            this.originalState = (ServerPrototypeData)this.currentState.Clone();
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
            Microsoft.SqlServer.Management.Smo.Server server = this.dataContainer.Server;
            bool changesMade = false;
            System.Diagnostics.Debug.Assert(server != null, "server object is null");


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

            if (changesMade)
            {
                server.Configuration.Alter(true);
            }

        }
        #endregion

        public void ApplyInfoToPrototype(ServerInfo serverInfo)
        {
            this.Name = serverInfo.Name;
            this.Language = serverInfo.Language;
            this.Memory = serverInfo.MemoryInMB;
            this.OperatingSystem = serverInfo.OperatingSystem;
            this.Platform = serverInfo.Platform;
            this.Version = serverInfo.Version;
            this.Processors = serverInfo.Processors;
            this.Version = serverInfo.Version;
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
            private int minMemory = 0;
            private int maxMemory = 0;

            private bool initialized = false;
            private Server server = null;
            private CDataContainer context = null;
            private bool isYukonOrLater = false;
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.version = value;
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
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
            /// <param name="context">The context in which we are modifying an existing serverRole</param>
            /// <param name="serverRole">The serverRole we are modifying</param>
            public ServerPrototypeData(CDataContainer context, Server server)
            {
                this.server = context.Server;
                this.context = context;
                this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
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
                result.server = this.server;
                return result;
            }

            private void LoadData()
            {
                this.initialized = true;
            }
        }
    }
}
