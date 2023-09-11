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
using Microsoft.SqlTools.ServiceLayer.Utility;

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

        public bool CheckBackupChecksum
        {
            get
            {
                return this.currentState.CheckBackupChecksum;
            }
            set
            {
                this.currentState.CheckBackupChecksum = value;
            }
        }

        public bool CheckCompressBackup
        {
            get
            {
                return this.currentState.CheckCompressBackup;
            }
            set
            {
                this.currentState.CheckCompressBackup = value;
            }
        }

        public string DataLocation
        {
            get
            {
                return this.currentState.DataLocation;
            }
            set
            {
                this.currentState.DataLocation = value;
            }
        }

        public string LogLocation
        {
            get
            {
                return this.currentState.LogLocation;
            }
            set
            {
                this.currentState.LogLocation = value;
            }
        }

        public string BackupLocation
        {
            get
            {
                return this.currentState.BackupLocation;
            }
            set
            {
                this.currentState.BackupLocation = value;
            }
        }

        public bool AllowTriggerToFireOthers
        {
            get
            {
                return this.currentState.AllowTriggerToFireOthers;
            }

            set
            {
                this.currentState.AllowTriggerToFireOthers = value;
            }
        }

        public NumericServerProperty BlockedProcThreshold
        {
            get
            {
                return this.currentState.BlockedProcThreshold;
            }

            set
            {
                this.currentState.BlockedProcThreshold = value;
            }
        }

        public NumericServerProperty CursorThreshold
        {
            get
            {
                return this.currentState.CursorThreshold;
            }

            set
            {
                this.currentState.CursorThreshold = value;
            }
        }

        public string DefaultFullTextLanguage
        {
            get
            {
                return this.currentState.DefaultFullTextLanguage;
            }

            set
            {
                this.currentState.DefaultFullTextLanguage = value;
            }
        }

        public string DefaultLanguage
        {
            get
            {
                return this.currentState.DefaultLanguage;
            }

            set
            {
                this.currentState.DefaultLanguage = value;
            }
        }

        public string FullTextUpgradeOption
        {
            get
            {
                return this.currentState.FullTextUpgradeOption;
            }

            set
            {
                this.currentState.FullTextUpgradeOption = value;
            }
        }

        public NumericServerProperty MaxTextReplicationSize
        {
            get
            {
                return this.currentState.MaxTextReplicationSize;
            }

            set
            {
                this.currentState.MaxTextReplicationSize = value;
            }
        }

        public bool OptimizeAdHocWorkloads
        {
            get
            {
                return this.currentState.OptimizeAdHocWorkloads;
            }

            set
            {
                this.currentState.OptimizeAdHocWorkloads = value;
            }
        }

        public bool ScanStartupProcs
        {
            get
            {
                return this.currentState.ScanStartupProcs;
            }

            set
            {
                this.currentState.ScanStartupProcs = value;
            }
        }

        public int TwoDigitYearCutoff
        {
            get
            {
                return this.currentState.TwoDigitYearCutoff;
            }

            set
            {
                this.currentState.TwoDigitYearCutoff = value;
            }
        }

        public NumericServerProperty CostThresholdParallelism
        {
            get
            {
                return this.currentState.CostThresholdParallelism;
            }

            set
            {
                this.currentState.CostThresholdParallelism = value;
            }
        }

        public NumericServerProperty Locks
        {
            get
            {
                return this.currentState.Locks;
            }

            set
            {
                this.currentState.Locks = value;
            }
        }

        public NumericServerProperty MaxDegreeParallelism
        {
            get
            {
                return this.currentState.MaxDegreeParallelism;
            }

            set
            {
                this.currentState.MaxDegreeParallelism = value;
            }
        }

        public NumericServerProperty QueryWait
        {
            get
            {
                return this.currentState.QueryWait;
            }

            set
            {
                this.currentState.QueryWait = value;
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

                if (UpdateDBSettingsValues(this.dataContainer.Server))
                {
                    server.Settings.Alter();
                }

                if (UpdateBackupConfig(this.dataContainer.Server))
                {
                    server.Configuration.Alter();
                }

                if (UpdateAdvancedValues(this.dataContainer.Server))
                {
                    server.Configuration.Alter();
                }

                if(UpdateFullTextService(this.dataContainer.Server))
                {
                    server.FullTextService.Alter();
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

        public bool UpdateBackupConfig(Server server)
        {
            bool alterServer = false;
            if (this.currentState.CheckBackupChecksum != this.originalState.CheckBackupChecksum)
            {
                server.Configuration.DefaultBackupChecksum.ConfigValue = this.currentState.CheckBackupChecksum ? 1 : 0;
                alterServer = true;
            }

            if (this.currentState.CheckCompressBackup != this.originalState.CheckCompressBackup)
            {
                server.Configuration.DefaultBackupCompression.ConfigValue = this.currentState.CheckCompressBackup ? 1 : 0;
                alterServer = true;
            }

            return alterServer;
        }

        public bool UpdateDBSettingsValues(Server server)
        {
            bool alterServer = false;

            if (this.currentState.DataLocation != this.originalState.DataLocation)
            {
                server.Settings.DefaultFile = this.currentState.DataLocation;
                alterServer = true;
            }

            if (this.currentState.LogLocation != this.originalState.LogLocation)
            {
                server.Settings.DefaultLog = this.currentState.LogLocation;
                alterServer = true;
            }

            if (this.currentState.BackupLocation != this.originalState.BackupLocation)
            {
                server.Settings.BackupDirectory = this.currentState.BackupLocation;
                alterServer = true;
            }
            return alterServer;
        }
        
        public bool UpdateFullTextService(Server server)
        {
            bool alterServer = false;
            if (this.currentState.FullTextUpgradeOption != this.originalState.FullTextUpgradeOption)
            {
                server.FullTextService.CatalogUpgradeOption = (FullTextCatalogUpgradeOption)Enum.Parse(typeof(FullTextCatalogUpgradeOption), this.currentState.FullTextUpgradeOption);
                alterServer = true;
            }
            return alterServer;
        }

        public bool UpdateAdvancedValues(Server server)
        {
            bool alterServer = false;
            if (this.currentState.AllowTriggerToFireOthers != this.originalState.AllowTriggerToFireOthers)
            {
                server.Configuration.NestedTriggers.ConfigValue = this.currentState.AllowTriggerToFireOthers ? 1 : 0;
                alterServer = true;
            }

            if (this.currentState.BlockedProcThreshold != this.originalState.BlockedProcThreshold)
            {
                server.Configuration.BlockedProcessThreshold.ConfigValue = this.currentState.BlockedProcThreshold.Value;
                alterServer = true;
            }

            if (this.currentState.CursorThreshold != this.originalState.CursorThreshold)
            {
                server.Configuration.CursorThreshold.ConfigValue = this.currentState.CursorThreshold.Value;
                alterServer = true;
            }

            if (this.currentState.DefaultFullTextLanguage != this.originalState.DefaultFullTextLanguage)
            {
                server.Configuration.DefaultFullTextLanguage.ConfigValue = LanguageUtils.GetLangIdFromAlias(server, this.currentState.DefaultFullTextLanguage);
                alterServer = true;
            }

            if (this.currentState.DefaultLanguage != this.originalState.DefaultLanguage)
            {
                server.Configuration.DefaultLanguage.ConfigValue = LanguageUtils.GetLangIdFromAlias(server, this.currentState.DefaultLanguage);
                alterServer = true;
            }

            if (this.currentState.MaxTextReplicationSize.Value != this.originalState.MaxTextReplicationSize.Value)
            {
                server.Configuration.ReplicationMaxTextSize.ConfigValue = this.currentState.MaxTextReplicationSize.Value;
                alterServer = true;
            }

            if (this.currentState.OptimizeAdHocWorkloads != this.originalState.OptimizeAdHocWorkloads)
            {
                server.Configuration.OptimizeAdhocWorkloads.ConfigValue = this.currentState.OptimizeAdHocWorkloads ? 1 : 0;
                alterServer = true;
            }

            if (this.currentState.ScanStartupProcs != this.originalState.ScanStartupProcs)
            {
                server.Configuration.ScanForStartupProcedures.ConfigValue = this.currentState.ScanStartupProcs ? 1 : 0;
                alterServer = true;
            }

            if (this.currentState.TwoDigitYearCutoff != this.originalState.TwoDigitYearCutoff)
            {
                server.Configuration.TwoDigitYearCutoff.ConfigValue = this.currentState.TwoDigitYearCutoff;
                alterServer = true;
            }

            if (this.currentState.CostThresholdParallelism != this.originalState.CostThresholdParallelism)
            {
                server.Configuration.CostThresholdForParallelism.ConfigValue = this.currentState.CostThresholdParallelism.Value;
                alterServer = true;
            }

            if (this.currentState.Locks != this.originalState.Locks)
            {
                server.Configuration.Locks.ConfigValue = this.currentState.Locks.Value;
                alterServer = true;
            }

            if (this.currentState.MaxDegreeParallelism != this.originalState.MaxDegreeParallelism)
            {
                server.Configuration.MaxDegreeOfParallelism.ConfigValue = this.currentState.MaxDegreeParallelism.Value;
                alterServer = true;
            }

            if (this.currentState.QueryWait != this.originalState.QueryWait)
            {
                server.Configuration.QueryWait.ConfigValue = this.currentState.QueryWait.Value;
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
            this.CheckBackupChecksum = serverInfo.CheckBackupChecksum;
            this.CheckCompressBackup = serverInfo.CheckCompressBackup;
            this.DataLocation = serverInfo.DataLocation;
            this.LogLocation = serverInfo.LogLocation;
            this.BackupLocation = serverInfo.BackupLocation;
            this.AllowTriggerToFireOthers = serverInfo.AllowTriggerToFireOthers;
            this.BlockedProcThreshold = serverInfo.BlockedProcThreshold;
            this.CursorThreshold = serverInfo.CursorThreshold;
            this.DefaultFullTextLanguage = serverInfo.DefaultFullTextLanguage;
            this.DefaultLanguage = serverInfo.DefaultLanguage;
            this.FullTextUpgradeOption = serverInfo.FullTextUpgradeOption;
            this.MaxTextReplicationSize = serverInfo.MaxTextReplicationSize;
            this.OptimizeAdHocWorkloads = serverInfo.OptimizeAdHocWorkloads;
            this.ScanStartupProcs = serverInfo.ScanStartupProcs;
            this.TwoDigitYearCutoff = serverInfo.TwoDigitYearCutoff;
            this.CostThresholdParallelism = serverInfo.CostThresholdParallelism;
            this.Locks = serverInfo.Locks;
            this.MaxDegreeParallelism = serverInfo.MaxDegreeParallelism;
            this.QueryWait = serverInfo.QueryWait;
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
            private bool checkCompressBackup = false;
            private bool checkBackupChecksum = false;
            private string dataLocation = String.Empty;
            private string logLocation = String.Empty;
            private string backupLocation = String.Empty;
            private bool allowTriggerToFireOthers = false;
            private NumericServerProperty blockedProcThreshold;
            private NumericServerProperty cursorThreshold;
            private string defaultFullTextLanguage = String.Empty;
            private string defaultLanguage = String.Empty;
            private string fullTextUpgradeOption = String.Empty;
            private NumericServerProperty maxTextReplicationSize;
            private bool optimizeAdHocWorkloads = false;
            private bool scanStartupProcs = false;
            private int twoDigitYearCutoff = 0;
            private NumericServerProperty costThresholdParallelism;
            private NumericServerProperty locks;
            private NumericServerProperty maxDegreeParallelism;
            private NumericServerProperty queryWait;
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
            public bool CheckBackupChecksum
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.checkBackupChecksum;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("CheckBackupChecksum"));
                    }
                    this.checkBackupChecksum = value;
                }
            }
            public bool CheckCompressBackup
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.checkCompressBackup;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("CheckCompressBackup"));
                    }
                    this.checkCompressBackup = value;
                }
            }

            public string DataLocation
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.dataLocation;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("DataLocation"));
                    }
                    this.dataLocation = value;
                }
            }

            public string LogLocation
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.logLocation;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("LogLocation"));
                    }
                    this.logLocation = value;
                }
            }

            public string BackupLocation
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.backupLocation;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("BackupLocation"));
                    }
                    this.backupLocation = value;
                }
            }

            public bool AllowTriggerToFireOthers
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.allowTriggerToFireOthers;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("AllowTriggerToFireOthers"));
                    }
                    this.allowTriggerToFireOthers = value;
                }
            }

            public NumericServerProperty BlockedProcThreshold
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.blockedProcThreshold;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("BlockedProcThreshold"));
                    }

                    this.blockedProcThreshold = value;
                }
            }

            public NumericServerProperty CursorThreshold
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.cursorThreshold;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("CursorThreshold"));
                    }

                    this.cursorThreshold = value;
                }
            }

            public string DefaultFullTextLanguage
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.defaultFullTextLanguage;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("DefaultFullTextLanguage"));
                    }
                    this.defaultFullTextLanguage = value;
                }
            }

            public string DefaultLanguage
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.defaultLanguage;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("DefaultLanguage"));
                    }
                    this.defaultLanguage = value;
                }
            }

            public string FullTextUpgradeOption
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.fullTextUpgradeOption;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("FullTextUpgradeOption"));
                    }
                    this.fullTextUpgradeOption = value;
                }
            }

            public NumericServerProperty MaxTextReplicationSize
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.maxTextReplicationSize;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("MaxTextReplicationSize"));
                    }

                    this.maxTextReplicationSize = value;
                }
            }

            public bool OptimizeAdHocWorkloads
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.optimizeAdHocWorkloads;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("OptimizeAdHocWorkloads"));
                    }
                    this.optimizeAdHocWorkloads = value;
                }
            }

            public bool ScanStartupProcs
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.scanStartupProcs;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("ScanStartupProcs"));
                    }
                    this.scanStartupProcs = value;
                }
            }

            public int TwoDigitYearCutoff
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.twoDigitYearCutoff;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("TwoDigitYearCutoff"));
                    }
                    this.twoDigitYearCutoff = value;
                }
            }

            public NumericServerProperty CostThresholdParallelism
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.costThresholdParallelism;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("CostThresholdParallelism"));
                    }
                    this.costThresholdParallelism = value;
                }
            }

            public NumericServerProperty Locks
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.locks;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("Locks"));
                    }
                    this.locks = value;
                }
            }

            public NumericServerProperty MaxDegreeParallelism
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.maxDegreeParallelism;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("MaxDegreeParallelism"));
                    }
                    this.maxDegreeParallelism = value;
                }
            }

            public NumericServerProperty QueryWait
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.queryWait;
                }

                set
                {
                    if (this.initialized)
                    {
                        Logger.Error(SR.PropertyNotInitialized("QueryWait"));
                    }
                    this.queryWait = value;
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
                this.blockedProcThreshold = new NumericServerProperty();
                this.cursorThreshold = new NumericServerProperty();
                this.maxTextReplicationSize = new NumericServerProperty();
                this.costThresholdParallelism = new NumericServerProperty();
                this.locks = new NumericServerProperty();
                this.maxDegreeParallelism = new NumericServerProperty();
                this.queryWait = new NumericServerProperty();
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
                result.checkBackupChecksum = this.checkBackupChecksum;
                result.checkCompressBackup = this.checkCompressBackup;
                result.dataLocation = this.dataLocation;
                result.logLocation = this.logLocation;
                result.backupLocation = this.backupLocation;
                result.allowTriggerToFireOthers = this.allowTriggerToFireOthers;
                result.blockedProcThreshold = this.blockedProcThreshold;
                result.cursorThreshold = this.cursorThreshold;
                result.defaultFullTextLanguage = this.defaultFullTextLanguage;
                result.defaultLanguage = this.defaultLanguage;
                result.fullTextUpgradeOption = this.fullTextUpgradeOption;
                result.maxTextReplicationSize = this.maxTextReplicationSize;
                result.optimizeAdHocWorkloads = this.optimizeAdHocWorkloads;
                result.scanStartupProcs = this.scanStartupProcs;
                result.twoDigitYearCutoff = this.twoDigitYearCutoff;
                result.costThresholdParallelism = this.costThresholdParallelism;
                result.locks = this.locks;
                result.maxDegreeParallelism = this.maxDegreeParallelism;
                result.queryWait = this.queryWait;
                result.server = this.server;
                return result;
            }

            private void LoadData()
            {
                this.initialized = true;
                LoadGeneralProperties();
                LoadMemoryProperties();
                LoadProcessorsProperties();
                LoadSecurityProperties();
                LoadDBSettingsProperties();
                LoadAdvancedProperties();
            }

            private void LoadGeneralProperties()
            {
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

            private void LoadProcessorsProperties()
            {
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
            }

            private void LoadSecurityProperties()
            {
                this.authenticationMode = server.LoginMode;
                this.loginAuditing = server.AuditLevel;
            }

            private void LoadDBSettingsProperties()
            {
                this.checkBackupChecksum = server.Configuration.DefaultBackupChecksum.ConfigValue == 1;
                this.checkCompressBackup = server.Configuration.DefaultBackupCompression.ConfigValue == 1;
                this.dataLocation = server.Settings.DefaultFile;
                this.logLocation = server.Settings.DefaultLog;
                this.backupLocation = server.Settings.BackupDirectory;
            }

            private void LoadAdvancedProperties()
            {
                this.allowTriggerToFireOthers = server.Configuration.NestedTriggers.ConfigValue == 1;
                this.blockedProcThreshold.Value = server.Configuration.BlockedProcessThreshold.ConfigValue;
                this.blockedProcThreshold.MinimumValue = server.Configuration.BlockedProcessThreshold.Minimum;
                this.blockedProcThreshold.MaximumValue = server.Configuration.BlockedProcessThreshold.Maximum;
                this.cursorThreshold.Value = server.Configuration.CursorThreshold.ConfigValue;
                this.cursorThreshold.MinimumValue = server.Configuration.CursorThreshold.Minimum;
                this.cursorThreshold.MaximumValue = server.Configuration.CursorThreshold.Maximum;
                this.defaultFullTextLanguage = LanguageUtils.GetLanguageChoiceAlias(server, server.Configuration.DefaultFullTextLanguage.ConfigValue).alias;
                var defaultLanguageLcid = LanguageUtils.GetLcidFromLangId(server, server.Configuration.DefaultLanguage.ConfigValue);
                this.defaultLanguage = (LanguageUtils.GetLanguageChoiceAlias(server, defaultLanguageLcid)).ToString();
                this.maxTextReplicationSize.Value = server.Configuration.ReplicationMaxTextSize.ConfigValue;
                this.maxTextReplicationSize.MinimumValue = server.Configuration.ReplicationMaxTextSize.Minimum;
                this.maxTextReplicationSize.MaximumValue = server.Configuration.ReplicationMaxTextSize.Maximum;
                this.optimizeAdHocWorkloads = server.Configuration.OptimizeAdhocWorkloads.ConfigValue == 1;
                this.scanStartupProcs = server.Configuration.ScanForStartupProcedures.ConfigValue == 1;
                this.twoDigitYearCutoff = server.Configuration.TwoDigitYearCutoff.ConfigValue;
                this.costThresholdParallelism.Value = server.Configuration.CostThresholdForParallelism.ConfigValue;
                this.costThresholdParallelism.MinimumValue = server.Configuration.CostThresholdForParallelism.Minimum;
                this.costThresholdParallelism.MaximumValue = server.Configuration.CostThresholdForParallelism.Maximum;
                this.locks.Value = server.Configuration.Locks.ConfigValue;
                this.locks.MinimumValue = server.Configuration.Locks.Minimum;
                this.locks.MaximumValue = server.Configuration.Locks.Maximum;
                this.maxDegreeParallelism.Value = server.Configuration.MaxDegreeOfParallelism.ConfigValue;
                this.maxDegreeParallelism.MinimumValue = server.Configuration.MaxDegreeOfParallelism.Minimum;
                this.maxDegreeParallelism.MaximumValue = server.Configuration.MaxDegreeOfParallelism.Maximum;
                this.queryWait.Value = server.Configuration.QueryWait.ConfigValue;
                this.queryWait.MinimumValue = server.Configuration.QueryWait.Minimum;
                this.queryWait.MaximumValue = server.Configuration.QueryWait.Maximum;
                try
                {
                    this.fullTextUpgradeOption = server.FullTextService.CatalogUpgradeOption.ToString();
                } catch
                {
                    this.fullTextUpgradeOption = String.Empty;
                }
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