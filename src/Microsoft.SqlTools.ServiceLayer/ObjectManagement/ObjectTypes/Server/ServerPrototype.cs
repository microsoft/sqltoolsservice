//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;
using Microsoft.SqlTools.Utility;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.ObjectTypes.Server;
using System.Linq;
using System.Collections;

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


        public NumericServerProperty MaxServerMemory
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

        public NumericServerProperty MinServerMemory
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

        public bool AutoProcessorAffinityMaskForAll
        {
            get
            {
                return this.currentState.AutoProcessorAffinityMaskForAll;
            }
            set
            {
                this.currentState.AutoProcessorAffinityMaskForAll = value;
            }

        }

        public bool AutoProcessorAffinityIOMaskForAll
        {
            get
            {
                return this.currentState.AutoProcessorAffinityMaskForAll;
            }
            set
            {
                this.currentState.AutoProcessorAffinityMaskForAll = value;
            }

        }

        public List<NumaNode> NumaNodes
        {
            get
            {
                return this.currentState.NumaNodes;
            }
            set
            {
                this.currentState.NumaNodes = value;
            }

        }

        public ServerLoginMode AuthenticationMode
        {
            get
            {
                return this.currentState.AuthenticationMode;
            }
            set
            {
                this.currentState.AuthenticationMode = value;
            }

        }

        public AuditLevel LoginAuditing
        {
            get
            {
                return this.currentState.LoginAuditing;
            }
            set
            {
                this.currentState.LoginAuditing = value;
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
                Server server = this.dataContainer.Server;
                bool alterServerConfig = false;
                bool sendCPUAffinityBeforeIO = false;
                bool sendIOAffinityBeforeCPU = false;
                bool sentCpuAffinity = false;

                sendCPUAffinityBeforeIO = this.CheckCPUAffinityBeforeIO(server);
                sendIOAffinityBeforeCPU = this.CheckIOAffinityBeforeCPU(server);
                alterServerConfig = this.CheckIOAffinityTsqlGenerated(server);
                if (!sendIOAffinityBeforeCPU)
                {
                    SendDataForKJ(server);
                    sentCpuAffinity = true;
                }

                if (alterServerConfig)
                {
                    try
                    {
                        server.Configuration.Alter((sendCPUAffinityBeforeIO && sendIOAffinityBeforeCPU));
                    }
                    finally
                    {
                        server.Configuration.Refresh();
                    }
                }
                if (!sentCpuAffinity)
                {
                    SendDataForKJ(server);
                }
                this.currentState.AffinityManagerProcessorMask.Clear();
                this.currentState.AffinityManagerIOMask.Clear();

                if (UpdateMemoryValues(this.dataContainer.Server))
                {
                    server.Configuration.Alter(true);
                }

                if (UpdateSecurityValues(this.dataContainer.Server))
                {
                    server.Alter();
                }
            }
        }

        public bool UpdateMemoryValues(Server server)
        {
            bool changesMade = false;
            if (this.currentState.MinMemory.Value != this.originalState.MinMemory.Value)
            {
                changesMade = true;
                ConfigProperty serverConfig = this.configService.GetServerSmoConfig(server, this.configService.MinServerMemoryPropertyNumber);
                serverConfig.ConfigValue = this.currentState.MinMemory.Value;
            }

            if (this.currentState.MaxMemory.Value != this.originalState.MaxMemory.Value)
            {
                changesMade = true;
                ConfigProperty serverConfig = this.configService.GetServerSmoConfig(server, this.configService.MaxServerMemoryPropertyNumber);
                serverConfig.ConfigValue = this.currentState.MaxMemory.Value;
            }
            return changesMade;
        }

        public bool UpdateSecurityValues(Server server)
        {
            bool alterServer = false;

            if (this.currentState.AuthenticationMode != this.originalState.AuthenticationMode)
            {
                // set authentication
                server.Settings.LoginMode = this.currentState.AuthenticationMode;
                alterServer = true;
            }

            if (this.currentState.LoginAuditing != this.originalState.LoginAuditing)
            {
                server.Settings.AuditLevel = this.currentState.LoginAuditing;
                alterServer = true;
            }
            return alterServer;
        }

        private bool CheckCPUAffinityBeforeIO(SMO.Server smoServer)
        {
            for (int i = 0; i < this.NumaNodes.Count; i++)
            {
                SMO.NumaNode nNode = smoServer.AffinityInfo.NumaNodes[i];
                for (int cpuCount = 0; cpuCount < this.NumaNodes[i].Processors.Count; cpuCount++)
                {
                    SMO.Cpu cpu = nNode.Cpus[cpuCount];
                    if (cpu.GroupID == 0)
                    {
                        if (cpu.AffinityMask == this.NumaNodes[i].Processors[cpuCount].IOAffinity && cpu.AffinityMask)
                        {
                            //if Current IO affinity is equal to initial cpu.Affinity then script Cpu Affinity first
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool CheckIOAffinityBeforeCPU(SMO.Server smoServer)
        {
            for (int i = 0; i < this.NumaNodes.Count; i++)
            {
                SMO.NumaNode nNode = smoServer.AffinityInfo.NumaNodes[i];
                for (int cpuCount = 0; cpuCount < this.NumaNodes[i].Processors.Count; cpuCount++)
                {
                    SMO.Cpu cpu = nNode.Cpus[cpuCount];
                    if (cpu.GroupID == 0)
                    {
                        if (this.currentState.AffinityManagerIOMask.initialIOAffinityArray[cpu.ID] == this.NumaNodes[i].Processors[cpuCount].Affinity && this.currentState.AffinityManagerIOMask.initialIOAffinityArray[cpu.ID])
                        {
                            //if Current IO affinity is equal to initial cpu.Affinity then script Cpu Affinity first
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// This will send data for KJ specific things
        /// Also Checks if Alter needs to be generated
        /// </summary>
        private void SendDataForKJ(SMO.Server smoServer)
        {
            BitArray finalCpuAffinity = new BitArray(64, false);
            bool sendAffinityInfoAlter = false;

            if (this.AutoProcessorAffinityMaskForAll)
            {
                if (smoServer.AffinityInfo.AffinityType != Microsoft.SqlServer.Management.Smo.AffinityType.Auto)
                {
                    smoServer.AffinityInfo.AffinityType = Microsoft.SqlServer.Management.Smo.AffinityType.Auto;
                    sendAffinityInfoAlter = true;
                }
            }
            else
            {
                if (smoServer.AffinityInfo.AffinityType != Microsoft.SqlServer.Management.Smo.AffinityType.Manual)
                {
                    smoServer.AffinityInfo.AffinityType = Microsoft.SqlServer.Management.Smo.AffinityType.Manual;
                    sendAffinityInfoAlter = true;
                }
            }

            for (int i = 0; i < this.NumaNodes.Count; i++)
            {
                SMO.NumaNode node = smoServer.AffinityInfo.NumaNodes[i];
                for (int cpuCount = 0; cpuCount < this.NumaNodes[i].Processors.Count; cpuCount++)
                {
                    SMO.Cpu cpu = node.Cpus[cpuCount];
                    if (this.NumaNodes[i].Processors[cpuCount].Affinity != cpu.AffinityMask)
                    {
                        sendAffinityInfoAlter = true;
                        if (!this.AutoProcessorAffinityMaskForAll)
                        { 
                            cpu.AffinityMask = this.NumaNodes[i].Processors[cpuCount].Affinity;
                        }
                    }
                }
            }

            if (sendAffinityInfoAlter)
            {
                try
                {
                    smoServer.AffinityInfo.Alter();
                    smoServer.Configuration.Alter();
                }
                finally
                {
                    smoServer.AffinityInfo.Refresh();
                    smoServer.Configuration.Refresh();
                }
            }

        }

        private bool CheckIOAffinityTsqlGenerated(SMO.Server smoServer)
        {
            bool sendAlter = false;
            bool send64AffinityIOAlter = false;
            BitArray finalCpuIOAffinity = new BitArray(64, false);
            for (int i = 0; i < this.NumaNodes.Count; i++)
            {
                SMO.NumaNode nNode = smoServer.AffinityInfo.NumaNodes[i];
                for (int cpuCount = 0; cpuCount < this.NumaNodes[i].Processors.Count; cpuCount++)
                {
                    SMO.Cpu cpu = nNode.Cpus[cpuCount];
                    if (cpu.GroupID == 0)
                    {
                        finalCpuIOAffinity[cpu.ID] = this.NumaNodes[i].Processors[cpuCount].IOAffinity;
                        if (this.currentState.AffinityManagerIOMask.initialIOAffinityArray[cpu.ID] != finalCpuIOAffinity[cpu.ID])
                        {
                            if (cpu.ID < AffinityManager.MAX32CPU)
                            {
                                sendAlter = true;
                            }
                            else
                            {
                                send64AffinityIOAlter = true;
                            }
                        }
                    }
                }
            }

            if (sendAlter || send64AffinityIOAlter)
            {
                int[] intArray = new int[2];
                finalCpuIOAffinity.CopyTo(intArray, 0);
                if (sendAlter)
                {
                    smoServer.Configuration.AffinityIOMask.ConfigValue = intArray[0];
                }
                if (send64AffinityIOAlter)
                {
                    smoServer.Configuration.Affinity64IOMask.ConfigValue = intArray[1];
                }
                //update current state initialIO after update
                if (this.currentState.AffinityManagerIOMask.initialIOAffinityArray != finalCpuIOAffinity)
                {
                    this.currentState.AffinityManagerIOMask.initialIOAffinityArray = finalCpuIOAffinity;
                }
            }
            return (sendAlter || send64AffinityIOAlter);
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
            this.AutoProcessorAffinityMaskForAll = serverInfo.AutoProcessorAffinityMaskForAll;
            this.AutoProcessorAffinityIOMaskForAll = serverInfo.AutoProcessorAffinityIOMaskForAll;
            this.NumaNodes = serverInfo.NumaNodes.ToList();
            this.AuthenticationMode = serverInfo.AuthenticationMode;
            this.LoginAuditing = serverInfo.LoginAuditing;
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
            private NumericServerProperty minMemory;
            private NumericServerProperty maxMemory;
            private bool autoProcessorAffinityMaskForAll = false;
            private bool autoProcessorAffinityIOMaskForAll = false;
            private List<NumaNode> numaNodes = new List<NumaNode>();
            private ServerLoginMode authenticationMode = ServerLoginMode.Integrated;
            private AuditLevel loginAuditing = AuditLevel.None;

            private bool initialized = false;
            private Server server;
            private CDataContainer context;
            private ServerConfigService configService;
            private AffinityManager affinityManagerIOMask;
            private AffinityManager affinityManagerProcessorMask;

            private bool isYukonOrLater = false;
            private bool isSqlServer64Bit;
            private bool isIOAffinitySupported = false;

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
                        Logger.Error(SR.PropertyNotInitialized("ServerName"));
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
                        Logger.Error(SR.PropertyNotInitialized("HardwareGeneration"));
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
                        Logger.Error(SR.PropertyNotInitialized("Language"));
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
                        Logger.Error(SR.PropertyNotInitialized("MemoryInMB"));
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
                        Logger.Error(SR.PropertyNotInitialized("OperatingSystem"));
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
                        Logger.Error(SR.PropertyNotInitialized("Platform"));
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
                        Logger.Error(SR.PropertyNotInitialized("Processors"));
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
                        Logger.Error(SR.PropertyNotInitialized("IsClustered"));
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
                        Logger.Error(SR.PropertyNotInitialized("IsHadrEnabled"));
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
                        Logger.Error(SR.PropertyNotInitialized("IsPolyBaseInstalled"));
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
                        Logger.Error(SR.PropertyNotInitialized("IsXTPSupported"));
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
                        Logger.Error(SR.PropertyNotInitialized("Product"));
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
                        Logger.Error(SR.PropertyNotInitialized("RootDirectory"));
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
                        Logger.Error(SR.PropertyNotInitialized("ServerCollation"));
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
                        Logger.Error(SR.PropertyNotInitialized("Version"));
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
                        Logger.Error(SR.PropertyNotInitialized("ServiceTier"));
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
                        Logger.Error(SR.PropertyNotInitialized("StorageSpaceUsageInMB"));
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
                        Logger.Error(SR.PropertyNotInitialized("ReservedStorageSizeMB"));
                    }
                    this.reservedStorageSizeMB = value;
                }
            }


            public NumericServerProperty MinMemory
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
                        Logger.Error(SR.PropertyNotInitialized("MinMemory"));
                    }

                    this.minMemory = value;
                }
            }

            public NumericServerProperty MaxMemory
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
                        Logger.Error(SR.PropertyNotInitialized("MaxMemory"));
                    }

                    this.maxMemory = value;
                }
            }

            public bool AutoProcessorAffinityMaskForAll
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.autoProcessorAffinityMaskForAll;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("AutoProcessorAffinityMaskForAll"));
                    }

                    this.autoProcessorAffinityMaskForAll = value;
                }
            }

            public bool AutoProcessorAffinityIOMaskForAll
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.autoProcessorAffinityIOMaskForAll;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("AutoProcessorAffinityIOMaskForAll"));
                    }

                    this.autoProcessorAffinityIOMaskForAll = value;
                }
            }

            public List<NumaNode> NumaNodes
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.numaNodes;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("NumaNodes"));
                    }

                    this.numaNodes = value;
                }
            }

            public ServerLoginMode AuthenticationMode
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.authenticationMode;
                }
                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("AuthenticationMode"));
                    }

                    this.authenticationMode = value;
                }
            }

            public AuditLevel LoginAuditing
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.loginAuditing;
                }
                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("LoginAuditing"));
                    }

                    this.loginAuditing = value;
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

            public AffinityManager AffinityManagerIOMask
            {
                get
                {
                    return this.affinityManagerIOMask;
                }

                set
                {
                    this.affinityManagerIOMask = value;
                }
            }

            public AffinityManager AffinityManagerProcessorMask
            {
                get
                {
                    return this.affinityManagerProcessorMask;
                }

                set
                {
                    this.affinityManagerProcessorMask = value;
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
                this.isSqlServer64Bit = (this.server.Edition.Contains("(64 - bit)"));
                this.affinityManagerIOMask = new AffinityManager();
                this.affinityManagerProcessorMask = new AffinityManager();
                this.serverMaxMemoryProperty = this.configService.GetServerSmoConfig(server, this.configService.MaxServerMemoryPropertyNumber);
                this.serverMinMemoryProperty = this.configService.GetServerSmoConfig(server, this.configService.MinServerMemoryPropertyNumber);
                this.minMemory = new NumericServerProperty();
                this.maxMemory = new NumericServerProperty();
                this.NumaNodes = new List<NumaNode>();
         
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
                result.autoProcessorAffinityMaskForAll = this.autoProcessorAffinityMaskForAll;
                result.autoProcessorAffinityIOMaskForAll = this.autoProcessorAffinityIOMaskForAll;
                result.numaNodes = this.numaNodes;
                result.authenticationMode = this.authenticationMode;
                result.loginAuditing = this.loginAuditing;
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
                LoadMemoryProperties();
                try
                {
                    this.affinityManagerIOMask.InitializeAffinity(this.server.Configuration.AffinityIOMask, this.server.Configuration.Affinity64IOMask);
                    this.isIOAffinitySupported = true;
                }
                catch
                {
                    this.isIOAffinitySupported = false;
                }
                this.affinityManagerProcessorMask.InitializeAffinity(this.server.Configuration.AffinityMask, this.server.Configuration.Affinity64Mask);

                this.numaNodes = GetNumaNodes();
                GetAutoProcessorsAffinity();
                this.authenticationMode = server.LoginMode;
                this.loginAuditing = server.AuditLevel;
            }

            private void LoadMemoryProperties()
            {
                this.maxMemory.Value = serverMaxMemoryProperty.ConfigValue;
                this.maxMemory.MaximumValue = serverMaxMemoryProperty.Maximum;
                this.maxMemory.MinimumValue = serverMaxMemoryProperty.Minimum;

                this.minMemory.Value = serverMinMemoryProperty.ConfigValue;
                this.minMemory.MaximumValue = serverMinMemoryProperty.Maximum;
                this.minMemory.MinimumValue = serverMinMemoryProperty.Minimum;
            }

            /// <summary>
            /// Get affinity masks for first 32 and next 32 processors (total 64 processors) if the
            /// processor masks have been modified after being read from the server.
            /// </summary>
            /// <param name="affinityConfig">returns the affinity for first 32 processors. null if not changed</param>
            /// <param name="affinity64Config">return the affinity for CPUs 33-64. null if not changed.</param>
            private List<NumaNode> GetNumaNodes()
            {
                List<NumaNode> results = new List<NumaNode>();
                foreach (SMO.NumaNode node in this.server.AffinityInfo.NumaNodes)
                {
                    var processors = new List<ProcessorAffinity>();
                    foreach (SMO.Cpu cpu in node.Cpus)
                    {
                        if (cpu.GroupID == 0)
                        {
                            var affinityIO = this.AffinityManagerIOMask.GetAffinity(cpu.ID, true);
                            if (!cpu.AffinityMask && this.isIOAffinitySupported && affinityIO) // if it's false then check if io affinity is checked
                            {
                                this.AffinityManagerIOMask.initialIOAffinityArray[cpu.ID] = true;
                            }
                    
                            // get affinityIO info if group id is 0
                            processors.Add(new ProcessorAffinity() { ProcessorId = cpu.ID.ToString(), Affinity = cpu.AffinityMask, IOAffinity = affinityIO });
                        }
                    }
                    var result = new NumaNode() { NumaNodeId = node.ID.ToString(), Processors = processors };
                    results.Add(result);
                }
                return results;
            }

            private void GetAutoProcessorsAffinity()
            {
                if (this.server.AffinityInfo.AffinityType == Microsoft.SqlServer.Management.Smo.AffinityType.Auto)
                {
                    this.autoProcessorAffinityMaskForAll = this.autoProcessorAffinityIOMaskForAll = true;
                }
                else
                {
                    this.autoProcessorAffinityMaskForAll = this.autoProcessorAffinityIOMaskForAll = false;
                }
            }
        }
    }
}
