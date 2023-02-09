//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using AzureEdition = Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper.AzureEdition;
using System;
using System.Data;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SQL Azure DB.
    /// </summary>
    internal class DatabasePrototypeAzure : DatabasePrototype160
    {

        #region Constants

        public const string Category_Azure = "Category_Azure";
        public const string Category_Azure_BRS = "Category_Azure_BRS";
        public const string Property_AzureMaxSize = "Property_AzureMaxSize";
        public const string Property_AzureCurrentServiceLevelObjective = "Property_AzureCurrentServiceLevelObjective";
        public const string Property_AzureConfiguredServiceLevelObjective = "Property_AzureConfiguredServiceLevelObjective";
        public const string Property_AzureEdition = "Property_AzureEdition";
        public const string Property_AzureBackupStorageRedundancy = "Property_AzureBackupStorageRedundancy";
        #endregion Constants

        public DatabasePrototypeAzure(CDataContainer context, DatabaseEngineEdition editionToCreate = DatabaseEngineEdition.SqlDatabase)
            : base(context)
        {
            EditionToCreate = editionToCreate;
        }

        #region Properties
       
        public string MaxSize
        {
            get
            {
                return this.currentState.maxSize == null ? null : this.currentState.maxSize.ToString();
            }
            set
            {
                this.currentState.maxSize = string.IsNullOrEmpty(value) ? null : DbSize.ParseDbSize(value);
                this.NotifyObservers();
            }
        }

        public string CurrentServiceLevelObjective
        {
            get
            {
                return this.currentState.currentServiceLevelObjective;
            }
            set
            {
                if (value != null && value.Contains('\''))
                {
                    throw new ArgumentException("Error_InvalidServiceLevelObjective");
                }
                this.currentState.currentServiceLevelObjective = value;
                this.NotifyObservers();
            }
        }

        [Browsable(false)]
        public AzureEdition AzureEdition
        {
            get
            {
                return this.currentState.azureEdition;
            }
        }

        //We have a separate property here so that the AzureEdition enum value is still exposed
        //(This property is for the name displayed in the drop down menu, which needs to be a string for casting purposes)
        public string AzureEditionDisplay
        {
            get
            {
                return AzureSqlDbHelper.GetAzureEditionDisplayName(this.currentState.azureEdition);
            }
            // set
            // {
            //     AzureEdition edition;
            //     if (AzureSqlDbHelper.TryGetAzureEditionFromDisplayName(value, out edition))
            //     {
            //         //Try to get the ServiceLevelObjective from the api,if not the default hardcoded service level objectives will be retrieved.
            //         string serverLevelObjective = AzureServiceLevelObjectiveProvider.TryGetAzureServiceLevelObjective(value, AzureServiceLocation);

            //         if (!string.IsNullOrEmpty(serverLevelObjective))
            //         {
            //             this.currentState.azureEdition = edition;
            //             this.currentState.currentServiceLevelObjective = serverLevelObjective;
            //             // Instead of creating db instance with default Edition, update EditionToCreate while selecting Edition from the UI.
            //             this.EditionToCreate = MapAzureEditionToDbEngineEdition(edition);
            //             string storageAccountType = AzureServiceLevelObjectiveProvider.TryGetAzureStorageType(value, AzureServiceLocation);
            //             if (!string.IsNullOrEmpty(storageAccountType))
            //             {
            //                 this.currentState.backupStorageRedundancy = storageAccountType;
            //             }

            //             // Try to get the azure maxsize from the api,if not the default hardcoded maxsize will be retrieved.
            //             DbSize dbSize = AzureServiceLevelObjectiveProvider.TryGetAzureMaxSize(value, serverLevelObjective, AzureServiceLocation);
            //             if (!string.IsNullOrEmpty(dbSize.ToString()))
            //             {
            //                 this.currentState.maxSize = new DbSize(dbSize.Size, dbSize.SizeUnit);
            //             }
            //         }
            //         else
            //         {
            //             if (edition == this.currentState.azureEdition)
            //             { //No changes, return early since we don't need to do any of the changes below
            //                 return;
            //             }

            //             this.currentState.azureEdition = edition;                        
            //             this.EditionToCreate = MapAzureEditionToDbEngineEdition(edition);
            //             this.CurrentServiceLevelObjective = AzureSqlDbHelper.GetDefaultServiceObjective(edition);
            //             this.BackupStorageRedundancy = AzureSqlDbHelper.GetDefaultBackupStorageRedundancy(edition);
            //             var defaultSize = AzureSqlDbHelper.GetDatabaseDefaultSize(edition);

            //             this.MaxSize = defaultSize == null ? String.Empty : defaultSize.ToString();
            //         }
            //         this.NotifyObservers();
            //     }
            //     else
            //     {
            //         //Can't really do much if we fail to parse the display name so just leave it as is and log a message
            //         System.Diagnostics.Debug.Assert(false,
            //             string.Format(CultureInfo.InvariantCulture,
            //                 "Failed to parse edition display name '{0}' back into AzureEdition", value));
            //     }
            // }
        }

        /// <summary>
        /// Mapping funtion to get the Database engine edition based on the selected AzureEdition value
        /// </summary>
        /// <param name="edition">Selected dropdown Azure Edition value</param>
        /// <returns>Corresponding DatabaseEngineEdition value</returns>
        private static DatabaseEngineEdition MapAzureEditionToDbEngineEdition(AzureEdition edition)
        {
            // As of now we only know for sure that AzureEdition.DataWarehouse maps to
            // DatabaseEngineEdition.SqlDataWarehouse, for all others we keep the default value
            // as before which was 'SqlDatabase'
            return edition == AzureEdition.DataWarehouse ? DatabaseEngineEdition.SqlDataWarehouse : DatabaseEngineEdition.SqlDatabase;
        }

        public override IList<FilegroupPrototype> Filegroups
        {
            get { return Enumerable.Empty<FilegroupPrototype>().ToList(); }
        }


        public override IList<DatabaseFilePrototype> Files
        {
            get { return Enumerable.Empty<DatabaseFilePrototype>().ToList(); }
        }

        [Browsable(false)]
        public override bool HideFileSettings
        {
            get { return true; }
        }

        [Browsable(false)]
        public override bool AllowScripting
        {
            get { return this.ServerVersion.Major > 11 && this.AzureEdition != AzureEdition.DataWarehouse; }
        }

        // [Browsable(false)]
        // public SubscriptionLocationKey AzureServiceLocation { get; set; }
        
        public string BackupStorageRedundancy
        {
            get
            {
                return this.currentState.backupStorageRedundancy;
            }
            set
            {
                this.currentState.backupStorageRedundancy = value;
                this.NotifyObservers();
            }
        }

        #endregion Properties

        #region DatabasePrototype overrides

        /// <summary>
        /// Commit changes to the database
        /// </summary>
        /// <param name="marshallingControl">The control through which UI interactions are to be marshalled</param>
        /// <returns>The SMO database object that was created or modified</returns>
        public override Database ApplyChanges()
        {
            Database database = base.ApplyChanges();
            if (this.AzureEdition != AzureEdition.DataWarehouse)
            {
                // We don't need to alter BSR value if the user is just scripting or if the DB is not creating.
                if (database != null && this.context.Server.ConnectionContext.SqlExecutionModes != SqlExecutionModes.CaptureSql)
                {
                    string alterAzureDbBackupStorageRedundancy = DatabasePrototypeAzure.CreateModifySqlDBBackupStorageRedundancyStatement(this.Name, this.currentState.backupStorageRedundancy);
                    using (var conn = this.context.ServerConnection.GetDatabaseConnection(this.Name).SqlConnectionObject)
                    {
                        //While scripting the database, there is already an open connection. So, we are checking the state of the connection here.
                        if (conn != null && conn.State == ConnectionState.Closed)
                        {
                            conn.Open();
                            using (var cmd = new SqlCommand { Connection = conn })
                            {
                                cmd.CommandText = alterAzureDbBackupStorageRedundancy;
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                return database;
            }

            string alterDbPropertiesStatement = DatabasePrototypeAzure.CreateModifySqlDwDbOptionsStatement(this.Name, this.MaxSize, this.CurrentServiceLevelObjective);

            string alterAzureDbRecursiveTriggersEnabledStatement = DatabasePrototypeAzure.CreateAzureDbSetRecursiveTriggersStatement(this.Name, this.RecursiveTriggers);
            string alterAzureDbIsReadOnlyStatement = DatabasePrototypeAzure.CreateAzureDbSetIsReadOnlyStatement(this.Name, this.IsReadOnly);

            Database db = this.GetDatabase();

            //Altering the DB needs to be done on the master DB
            using (var conn = this.context.ServerConnection.GetDatabaseConnection("master").SqlConnectionObject)
            {
                var cmd = new SqlCommand { Connection = conn };
                conn.Open();

                //Only run the alter statements for modifications made. This is mostly to allow the non-Azure specific
                //properties to be updated when a SLO change is in progress, but it also is beneficial to save trips to the
                //server whenever we can (especially when Azure is concerned)
                if ((currentState.azureEdition != null && currentState.azureEdition != originalState.azureEdition) ||
                   (!string.IsNullOrEmpty(currentState.currentServiceLevelObjective) && currentState.currentServiceLevelObjective != originalState.currentServiceLevelObjective) ||
                   (currentState.maxSize != null && currentState.maxSize != originalState.maxSize))
                {
                    cmd.CommandText = alterDbPropertiesStatement;
                    cmd.ExecuteNonQuery();
                }

                if (currentState.recursiveTriggers != originalState.recursiveTriggers)
                {
                    cmd.CommandText = alterAzureDbRecursiveTriggersEnabledStatement;
                    cmd.ExecuteNonQuery();
                }

                if (currentState.isReadOnly != originalState.isReadOnly)
                {
                    cmd.CommandText = alterAzureDbIsReadOnlyStatement;
                    cmd.ExecuteNonQuery();
                }
            }

            //Because we didn't use SMO to do the alter we should refresh the DB object so it picks up the correct properties
            db.Refresh();
            return db;
        }

        #endregion DatabasePrototype overrides

        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);

            // treat null as defaults/unchanged
            // SMO will only script changed values so if the user changes edition and size and SLO are empty the alter
            // will change the db to the default size and slo for the new edition
            // if the new combination of edition/size/slo is invalid the alter will fail
            if (this.currentState.maxSize != null && (!this.Exists || (this.originalState.maxSize != this.currentState.maxSize)))
            {
                db.MaxSizeInBytes = this.currentState.maxSize.SizeInBytes;
            }

            if (this.currentState.azureEdition != null && (!this.Exists || (this.originalState.azureEdition != this.currentState.azureEdition)))
            {
                db.AzureEdition = this.currentState.azureEdition.ToString();
            }

            if (!string.IsNullOrEmpty(this.currentState.currentServiceLevelObjective) && (!this.Exists || (this.originalState.currentServiceLevelObjective != this.currentState.currentServiceLevelObjective)))
            {
                db.AzureServiceObjective = this.currentState.currentServiceLevelObjective;
            }
        }

        private const string AlterDbStatementFormat = @"ALTER DATABASE [{0}] {1}";
        private const string ModifySqlDwDbStatementFormat = @"MODIFY (MAXSIZE={0} {1})";
        private const string AzureServiceLevelObjectiveOptionFormat = @"SERVICE_OBJECTIVE = '{0}'";
        private const string SetReadOnlyOption = @"SET READ_ONLY";
        private const string SetReadWriteOption = @"SET READ_WRITE";
        private const string SetRecursiveTriggersOptionFormat = @"SET RECURSIVE_TRIGGERS {0}";
        private const string On = @"ON";
        private const string Off = @"OFF";
        private const string ModifySqlDbBackupStorageRedundancy = @"MODIFY BACKUP_STORAGE_REDUNDANCY = '{0}'";

        /// <summary>
        /// Creates an ALTER DATABASE statement to modify the Read-Only status of the target DB
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="isReadOnly"></param>
        /// <returns></returns>
        protected static string CreateAzureDbSetIsReadOnlyStatement(string dbName, bool isReadOnly)
        {
            return CreateAzureAlterDbStatement(dbName,
                string.Format(CultureInfo.InvariantCulture,
                    isReadOnly ? SetReadOnlyOption : SetReadWriteOption));
        }

        /// <summary>
        /// Creates an ALTER DATABASE statement to modify the RECURSIVE_TRIGGERS option of the target DB
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="recursiveTriggersEnabled"></param>
        /// <returns></returns>
        protected static string CreateAzureDbSetRecursiveTriggersStatement(string dbName, bool recursiveTriggersEnabled)
        {
            return CreateAzureAlterDbStatement(dbName,
                string.Format(CultureInfo.InvariantCulture,
                    DatabasePrototypeAzure.SetRecursiveTriggersOptionFormat,
                        recursiveTriggersEnabled ? DatabasePrototypeAzure.On : DatabasePrototypeAzure.Off));
        }

        /// <summary>
        /// Creates an ALTER DATABASE statement to modify the Azure DataWarehouse properties  (MaxSize and Service Level Objective)
        /// for the target database
        /// </summary>
        /// <param name="dbName">Name of the database</param>
        /// <param name="maxSize">MaxSize of the database</param>
        /// <param name="serviceLevelObjective">New SLO of the database</param>
        /// <returns>Sql Statement to Alter the database.</returns>
        protected static string CreateModifySqlDwDbOptionsStatement(string dbName, string maxSize, string serviceLevelObjective)
        {
            //We might not have a SLO since some editions don't support it
            string sloOption = string.IsNullOrEmpty(serviceLevelObjective) ?
                string.Empty : ", " + string.Format(CultureInfo.InvariantCulture, AzureServiceLevelObjectiveOptionFormat, serviceLevelObjective);

            return CreateAzureAlterDbStatement(dbName,
                string.Format(CultureInfo.InvariantCulture,
                    ModifySqlDwDbStatementFormat,
                    maxSize,
                    sloOption));
        }

        /// <summary>
        /// Creates the ATLER DATABASE statement from the given backup storage redundancy option.
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        protected static string CreateModifySqlDBBackupStorageRedundancyStatement(string dbName, string option)
        {
            //Note: We allow user to select any one of the value from the UI for backupStorageRedundancy. So, we are inlining the value.
            return CreateAzureAlterDbStatement(dbName,
                string.Format(CultureInfo.InvariantCulture,
                    ModifySqlDbBackupStorageRedundancy,
                    option));
        }

        /// <summary>
        /// Creates the ALTER DATABASE statement from the given op
        /// </summary>
        /// <returns></returns>
        private static string CreateAzureAlterDbStatement(string dbName, string options)
        {
            return string.Format(CultureInfo.InvariantCulture, AlterDbStatementFormat,
                CUtils.EscapeString(CUtils.EscapeString(dbName, ']'), '\''),
                options);
        }
    }
}


