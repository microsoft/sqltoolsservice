//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;
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

        private Server server;
        private ServerConnection sqlConnection;
        private ServerConfigService configService;

        protected ServerData currentState;
        private ServerData originalState;
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

        public bool? IsXTPSupported
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
        public ServerPrototype(Server server, ServerConnection connection)
        {
            this.server = server;
            this.sqlConnection = connection;
            this.configService = new ServerConfigService();
            this.currentState = new ServerData(server, this.configService);
            this.originalState = (ServerData)this.currentState.Clone();
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
            if (this.server != null)
            {
                Server server = this.server;

                if (UpdateMemoryValues(this.server))
                {
                    server.Configuration.Alter(true);
                }

                UpdateProcessorsValues(this.server);

                if (UpdateSecurityValues(this.server))
                {
                    server.Alter();
                }

                if (UpdateDBSettingsValues(this.server))
                {
                    server.Settings.Alter();
                }

                if (UpdateBackupConfig(this.server))
                {
                    server.Configuration.Alter();
                }

                if (UpdateAdvancedValues(this.server))
                {
                    server.Configuration.Alter();
                }

                if (UpdateFullTextService(this.server))
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
                server.Configuration.MinServerMemory.ConfigValue = this.currentState.MinMemory.Value;
            }

            if (this.currentState.MaxMemory.Value != this.originalState.MaxMemory.Value)
            {
                changesMade = true;
                server.Configuration.MaxServerMemory.ConfigValue = this.currentState.MaxMemory.Value;
            }
            return changesMade;
        }

        public void UpdateProcessorsValues(Server server)
        {
            bool alterServerConfig = false;
            bool sendCPUAffinityBeforeIO = false;
            bool sendIOAffinityBeforeCPU = false;
            bool sentCpuAffinity = false;
            if (this.currentState.AutoProcessorAffinityIOMaskForAll != this.originalState.AutoProcessorAffinityIOMaskForAll ||
                this.currentState.AutoProcessorAffinityMaskForAll != this.originalState.AutoProcessorAffinityMaskForAll)
            {
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
            }
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

            if (this.currentState.BlockedProcThreshold.Value != this.originalState.BlockedProcThreshold.Value)
            {
                server.Configuration.BlockedProcessThreshold.ConfigValue = this.currentState.BlockedProcThreshold.Value;
                alterServer = true;
            }

            if (this.currentState.CursorThreshold.Value != this.originalState.CursorThreshold.Value)
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

            if (this.currentState.CostThresholdParallelism.Value != this.originalState.CostThresholdParallelism.Value)
            {
                server.Configuration.CostThresholdForParallelism.ConfigValue = this.currentState.CostThresholdParallelism.Value;
                alterServer = true;
            }

            if (this.currentState.Locks.Value != this.originalState.Locks.Value)
            {
                server.Configuration.Locks.ConfigValue = this.currentState.Locks.Value;
                alterServer = true;
            }

            if (this.currentState.MaxDegreeParallelism.Value != this.originalState.MaxDegreeParallelism.Value)
            {
                server.Configuration.MaxDegreeOfParallelism.ConfigValue = this.currentState.MaxDegreeParallelism.Value;
                alterServer = true;
            }

            if (this.currentState.QueryWait.Value != this.originalState.QueryWait.Value)
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
        /// This will send data for Kilimanjaro specific things
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

        public virtual void ApplyInfoToPrototype(ServerInfo serverInfo)
        {
            this.Name = serverInfo.Name ?? string.Empty;
            this.Language = serverInfo.Language;
            this.MemoryInMB = serverInfo.MemoryInMB;
            this.Version = serverInfo.Version;
            this.Processors = serverInfo.Processors;
            this.Version = serverInfo.Version;
            this.IsClustered = serverInfo.IsClustered;
            this.IsHadrEnabled = serverInfo.IsHadrEnabled;
            this.IsXTPSupported = serverInfo.IsXTPSupported.GetValueOrDefault();
            this.Product = serverInfo.Product;
            this.RootDirectory = serverInfo.RootDirectory;
            this.ServerCollation = serverInfo.ServerCollation;
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
    }
}