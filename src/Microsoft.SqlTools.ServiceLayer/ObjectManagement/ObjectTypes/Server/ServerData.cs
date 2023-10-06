//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;
using Microsoft.SqlTools.Utility;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.ObjectTypes.Server;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Represents a collection of server properties for a SQL Server instance.
    /// </summary>
    /// <remarks>
    /// Isolating this data allows for an easy implementation of Reset() and
    /// simplifies difference detection when committing changes to the server.
    /// </remarks>
    internal class ServerData : ICloneable
    {
        #region data members
        private string serverName = string.Empty;
        private string hardwareGeneration = string.Empty;
        private string language = string.Empty;
        private int memoryInMB = 0;
        private string operatingSystem = string.Empty;
        private string platform = string.Empty;
        private int processors = 0;
        private bool isClustered = false;
        private bool isHadrEnabled = false;
        private bool isPolyBaseInstalled = false;
        private bool? isXTPSupported;
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
        private ServerConfigService configService;
        private AffinityManager affinityManagerIOMask;
        private AffinityManager affinityManagerProcessorMask;

        private bool isYukonOrLater = false;
        private bool isSqlServer64Bit;
        private bool isIOAffinitySupported = false;
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

        public bool? IsXTPSupported
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
        private ServerData()
        {
        }


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context">The context in which we are modifying an existing server</param>
        /// <param name="server">The server we are modifying</param>
        public ServerData(Server server, ServerConfigService service)
        {
            this.server = server;
            this.configService = service;
            this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
            this.isSqlServer64Bit = (this.server.Edition.Contains("(64 - bit)"));
            this.affinityManagerIOMask = new AffinityManager();
            this.affinityManagerProcessorMask = new AffinityManager();
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
            ServerData result = new ServerData();
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
            this.language = server.Language;
            this.memoryInMB = server.PhysicalMemory;
            this.processors = server.Processors;
            this.isClustered = server.IsClustered;
            this.isHadrEnabled = server.IsHadrEnabled;
            this.isPolyBaseInstalled = server.IsPolyBaseInstalled;

            this.product = server.Product;
            this.rootDirectory = server.RootDirectory;
            this.serverCollation = server.Collation;
            this.version = server.VersionString;
            if (server.EngineEdition == Edition.SqlManagedInstance)
            {
                this.hardwareGeneration = server.HardwareGeneration;
                this.serviceTier = server.ServiceTier;
                this.reservedStorageSizeMB = server.ReservedStorageSizeMB;
                this.storageSpaceUsageInMB = server.UsedStorageSizeMB;
            }
            else
            {
                this.isXTPSupported = server.IsXTPSupported;
            }
            if (server.VersionMajor >= 14)
            {
                this.operatingSystem = server.HostDistribution;
                this.platform = server.HostPlatform;
            }
            if (server.VersionMajor >= 13)
            {
                this.isPolyBaseInstalled = server.IsPolyBaseInstalled;
            }
        }

        private void LoadMemoryProperties()
        {
            this.maxMemory.Value = server.Configuration.MaxServerMemory.ConfigValue;
            this.maxMemory.MaximumValue = server.Configuration.MaxServerMemory.Maximum;
            this.maxMemory.MinimumValue = server.Configuration.MaxServerMemory.Minimum;

            this.minMemory.Value = server.Configuration.MinServerMemory.ConfigValue;
            this.minMemory.MaximumValue = server.Configuration.MinServerMemory.Maximum;
            this.minMemory.MinimumValue = server.Configuration.MinServerMemory.Minimum;
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
            }
            catch
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