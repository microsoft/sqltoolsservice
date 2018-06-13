//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    public class DatabaseTaskHelper: IDisposable
    {
        private static DateTime minBackupDate = new DateTime(1900, 1, 1);

        private DatabasePrototype prototype;

        private XmlDocument document;

        public CDataContainer DataContainer { get; set; }

        /// <summary>
        /// Expose database prototype to internal classes 
        /// </summary>
        public DatabasePrototype Prototype
        {
            get
            {
                return this.prototype;
            }
            set
            {
                this.prototype = value;
            }
        }

        public DatabaseTaskHelper(CDataContainer context)
        {
            Initialize(context);
        }

        internal void Initialize(CDataContainer context)
        {
            if (context != null)
            {
                this.DataContainer = context;
                this.document = context.Document;

                int majorVersionNumber = context.Server.Information.Version.Major;
                Version sql2000sp3 = new Version(8, 0, 760);
                Version sql2005sp2 = new Version(9, 0, 3000);

                if (context.Server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    this.prototype = new DatabasePrototypeAzure(context);
                }
                else if (Utils.IsSql11OrLater(context.Server.Version.Major))
                {
                    this.prototype = new DatabasePrototype110(context);
                }
                else if (majorVersionNumber == 10)
                {
                    this.prototype = new DatabasePrototype100(context);
                }
                else if ((sql2005sp2 <= context.Server.Information.Version) &&
                    (context.Server.Information.EngineEdition == Edition.EnterpriseOrDeveloper))
                {
                    this.prototype = new DatabasePrototype90EnterpriseSP2(context);
                }
                else if (8 < majorVersionNumber)
                {
                    this.prototype = new DatabasePrototype90(context);
                }
                else if (sql2000sp3 <= context.Server.Information.Version)
                {
                    this.prototype = new DatabasePrototype80SP3(context);
                }
                else if (7 < majorVersionNumber)
                {
                    this.prototype = new DatabasePrototype80(context);
                }
                else
                {
                    this.prototype = new DatabasePrototype(context);
                }

                this.prototype.Initialize();         
            }
            else
            {
                this.DataContainer = null;
                this.document = null;
                this.prototype = null;
            }
        }

        internal static DatabaseInfo DatabasePrototypeToDatabaseInfo(DatabasePrototype prototype)
        {
            var databaseInfo = new DatabaseInfo();
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.Name, prototype.Name);
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.Owner, prototype.Owner);
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.Collation, prototype.Collation);
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.DatabaseState, prototype.DatabaseState.ToString());
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.RecoveryModel, prototype.RecoveryModel.ToString());
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.IsSystemDB, prototype.IsSystemDB.ToString());
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.AnsiNulls, prototype.AnsiNulls.ToString());
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.CompatibilityLevel, (int)prototype.DatabaseCompatibilityLevel);
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.LastBackupDate, GetBackupDate(prototype.LastBackupDate));
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.LastLogBackupDate, GetBackupDate(prototype.LastLogBackupDate));

            databaseInfo.Options.Add(
                AdminServicesProviderOptionsHelper.FileGroups + "Count", 
                prototype.Filegroups.Count);
             
            for (int i = 0; i < prototype.Filegroups.Count; ++i)
            {
                var fileGroup = prototype.Filegroups[i];
                string itemPrefix = AdminServicesProviderOptionsHelper.FileGroups + "." + i + ".";
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.Name, fileGroup.Name);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.IsMemoryOptimized, fileGroup.IsMemoryOptimized);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.IsReadOnly, fileGroup.IsReadOnly);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.IsFileStream, fileGroup.IsFileStream);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.IsDefault, fileGroup.IsDefault);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.FileGroupType, fileGroup.FileGroupType.ToString());
            }

            databaseInfo.Options.Add(
                AdminServicesProviderOptionsHelper.DatabaseFiles + "Count", 
                prototype.Files.Count);

            for (int i = 0; i < prototype.Files.Count; ++i)
            {
                var file = prototype.Files[i];
                string itemPrefix = AdminServicesProviderOptionsHelper.DatabaseFiles + "." + i + ".";
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.Name, file.Name);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.PhysicalName, file.PhysicalName);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.Autogrowth, (file.DefaultAutogrowth != null ? file.DefaultAutogrowth.ToString() : string.Empty));
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.DatabaseFileType, file.DatabaseFileType.ToString());
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.Folder, file.DefaultFolder);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.Size, file.DefaultSize);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.FileGroup, file.FileGroup != null ? file.FileGroup.Name : string.Empty);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.InitialSize, file.InitialSize);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.IsPrimaryFile, file.IsPrimaryFile);
            }

            AddAzureProperties(databaseInfo, prototype as DatabasePrototypeAzure);

            return databaseInfo;
        }

        private static void AddAzureProperties(DatabaseInfo databaseInfo, DatabasePrototypeAzure prototype)
        {
            if (prototype == null) { return; }

            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.AzureEdition, prototype.AzureEditionDisplay);
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.ServiceLevelObjective, prototype.CurrentServiceLevelObjective);

        }

        private static string GetBackupDate(DateTime backupDate)
        {
            if (backupDate == null
                || backupDate < minBackupDate)
            {
                return SR.NeverBackedUp;
            }
            return backupDate.ToString();
        }

        private static T GetValueOrDefault<T>(string key, Dictionary<string, object> map, T defaultValue) 
        {
            if (map != null && map.ContainsKey(key))
            {
                return map[key] != null ? (T)map[key] : default(T);
            }
            return defaultValue;
        }

        private static int logicalNameCount = 0;

        public static DatabasePrototype ApplyToPrototype(DatabaseInfo databaseInfo, DatabasePrototype prototype)
        {
            if (databaseInfo != null && prototype != null)
            {
                prototype.Name = GetValueOrDefault(AdminServicesProviderOptionsHelper.Name, databaseInfo.Options, prototype.Name);
            
                foreach (var file in prototype.Files)
                {
                    if (string.IsNullOrWhiteSpace(file.Name))
                    {
                        file.Name = prototype.Name + "_" + logicalNameCount;
                    }
                    else
                    {
                        file.Name += prototype.Name + file.Name  + "_" + logicalNameCount;
                    }

                    ++logicalNameCount;
                }

            }
            return prototype;
        }

        public void Dispose()
        {
            try
            {
                if (this.DataContainer != null)
                {
                    this.DataContainer.Dispose();
                }
            }
            catch(Exception ex)
            {
                Logger.Write(LogLevel.Warning, $"Failed to disconnect Database task Helper connection. Error: {ex.Message}");
            }
        }
    }
}
