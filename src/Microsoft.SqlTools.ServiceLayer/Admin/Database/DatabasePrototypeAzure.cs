//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Globalization;
using System.Data.SqlClient;
using AzureEdition = Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper.AzureEdition;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database properties for SQL Azure DB.
    ///  Business/Web editions are up to compat level 100 now   
    /// </summary>
    [TypeConverter(typeof(DynamicValueTypeConverter))]
    internal class DatabasePrototypeAzure : DatabasePrototype100
    {

        #region Constants

        public const string Category_Azure = "Category_Azure";
        public const string Property_AzureMaxSize = "Property_AzureMaxSize";
        public const string Property_AzureCurrentServiceLevelObjective = "Property_AzureCurrentServiceLevelObjective";
        public const string Property_AzureConfiguredServiceLevelObjective = "Property_AzureConfiguredServiceLevelObjective";
        public const string Property_AzureEdition = "Property_AzureEdition";
        #endregion Constants

        public DatabasePrototypeAzure(CDataContainer context, DatabaseEngineEdition editionToCreate = DatabaseEngineEdition.SqlDatabase)
            : base(context)
        {
            EditionToCreate = editionToCreate;
        }

        #region Properties

        [Category(Category_Azure),
         DisplayNameAttribute(Property_AzureMaxSize)]
        public string MaxSize
        {
            get
            {
                return this.currentState.maxSize == null ? null : this.currentState.maxSize.ToString();
            }
            set
            {
                this.currentState.maxSize = DbSize.ParseDbSize(value);
                this.NotifyObservers();
            }
        }

        [Category(Category_Azure),
         DisplayNameAttribute(Property_AzureCurrentServiceLevelObjective)]
        public string CurrentServiceLevelObjective
        {
            get
            {
                return this.currentState.currentServiceLevelObjective;
            }
            set
            {
                this.currentState.currentServiceLevelObjective = value;
                this.NotifyObservers();
            }
        }

        [Category(Category_Azure),
         DisplayNameAttribute(Property_AzureConfiguredServiceLevelObjective)]
        public string ConfiguredServiceLevelObjective
        {
            //This value is read only because it's changed by changing the current SLO,
            //we just expose this to show if the DB is currently transitioning
            get
            {
                return this.currentState.configuredServiceLevelObjective;
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

        [Category(Category_Azure),
         DisplayNameAttribute(Property_AzureEdition)]
        //We have a separate property here so that the AzureEdition enum value is still exposed
        //(This property is for the name displayed in the drop down menu, which needs to be a string for casting purposes)
        public string AzureEditionDisplay
        {
            get
            {
                return AzureSqlDbHelper.GetAzureEditionDisplayName(this.currentState.azureEdition);
            }
            set
            {
                AzureEdition edition;
                if (AzureSqlDbHelper.TryGetAzureEditionFromDisplayName(value, out edition))
                {
                    if (edition == this.currentState.azureEdition)
                    { //No changes, return early since we don't need to do any of the changes below
                        return;
                    }

                    this.currentState.azureEdition = edition;
                    this.CurrentServiceLevelObjective = AzureSqlDbHelper.GetDefaultServiceObjective(edition);
                    this.MaxSize = AzureSqlDbHelper.GetDatabaseDefaultSize(edition).ToString();
                    this.NotifyObservers();
                }              
            }
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

        #endregion Properties

        #region DatabasePrototype overrides

        /// <summary>
        /// Commit changes to the database
        /// </summary>
        /// <param name="marshallingControl">The control through which UI interactions are to be marshalled</param>
        /// <returns>The SMO database object that was created or modified</returns>
        public override Database ApplyChanges()
        {
           // For v12 Non-DW DBs lets use SMO
           if (this.ServerVersion.Major >= 12  && this.AzureEdition != AzureEdition.DataWarehouse)
           {
               return base.ApplyChanges();
           }

           //Note : We purposely don't call base.ApplyChanges() here since SMO doesn't fully support Azure yet and so will throw
           //an error if we try to modify the Database object directly            
           string alterDbPropertiesStatement = DatabasePrototypeAzure.CreateModifyAzureDbOptionsStatement(this.Name, this.AzureEdition, this.MaxSize, this.CurrentServiceLevelObjective);
           if (this.AzureEdition == AzureEdition.DataWarehouse)
           {
               alterDbPropertiesStatement = DatabasePrototypeAzure.CreateModifySqlDwDbOptionsStatement(this.Name, this.MaxSize, this.CurrentServiceLevelObjective);
           }

           string alterAzureDbRecursiveTriggersEnabledStatement = DatabasePrototypeAzure.CreateAzureDbSetRecursiveTriggersStatement(this.Name, this.RecursiveTriggers);
           string alterAzureDbIsReadOnlyStatement = DatabasePrototypeAzure.CreateAzureDbSetIsReadOnlyStatement(this.Name, this.IsReadOnly);

           Database db = this.GetDatabase();

           //Altering the DB needs to be done on the master DB
           using (var conn = new SqlConnection(this.context.ServerConnection.GetDatabaseConnection("master").ConnectionString))
           {
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    //Only run the alter statements for modifications made. This is mostly to allow the non-Azure specific
                    //properties to be updated when a SLO change is in progress, but it also is beneficial to save trips to the
                    //server whenever we can (especially when Azure is concerned)
                    if (currentState.azureEdition != originalState.azureEdition ||
                       currentState.currentServiceLevelObjective != originalState.currentServiceLevelObjective ||
                       currentState.maxSize != originalState.maxSize)
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
           }

           //Because we didn't use SMO to do the alter we should refresh the DB object so it picks up the correct properties
           db.Refresh();

           // For properties that are supported in Database.Alter(), call SaveProperties, and then alter the DB.
           //
           if (this.AzureEdition != AzureEdition.DataWarehouse)
           {
               this.SaveProperties(db);
               db.Alter(TerminationClause.FailOnOpenTransactions);
           }
           return db;
        }

        #endregion DatabasePrototype overrides

        protected override void SaveProperties(Database db)
        {
            base.SaveProperties(db);
            if (this.ServerVersion.Major >= 12 && this.AzureEdition != AzureEdition.DataWarehouse)
            {
                if (!this.Exists || (this.originalState.maxSize != this.currentState.maxSize))
                {
                    db.MaxSizeInBytes = this.currentState.maxSize.SizeInBytes;
                }

                if (!this.Exists || (this.originalState.azureEdition != this.currentState.azureEdition))
                {
                    db.AzureEdition = this.currentState.azureEdition.ToString();
                }

                if (!this.Exists || (this.originalState.currentServiceLevelObjective != this.currentState.currentServiceLevelObjective))
                {
                    db.AzureServiceObjective = this.currentState.currentServiceLevelObjective;
                }
            }
            
        }

        private const string AlterDbStatementFormat =
            @"ALTER DATABASE [{0}] {1}";

        private const string ModifyAzureDbStatementFormat = @"MODIFY (EDITION = '{0}', MAXSIZE={1} {2})";
        private const string ModifySqlDwDbStatementFormat = @"MODIFY (MAXSIZE={0} {1})";
        private const string AzureServiceLevelObjectiveOptionFormat = @"SERVICE_OBJECTIVE = '{0}'";
        private const string SetReadOnlyOption = @"SET READ_ONLY";
        private const string SetReadWriteOption = @"SET READ_WRITE";
        private const string SetRecursiveTriggersOptionFormat = @"SET RECURSIVE_TRIGGERS {0}";
        private const string On = @"ON";
        private const string Off = @"OFF";

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
        /// Creates an ALTER DATABASE statement to modify the Azure Database properties (Edition, MaxSize and Service Level Objective)
        /// for the target database
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="edition"></param>
        /// <param name="maxSize"></param>
        /// <param name="serviceLevelObjective"></param>
        /// <returns></returns>
        protected static string CreateModifyAzureDbOptionsStatement(string dbName, AzureEdition edition, string maxSize, string serviceLevelObjective)
        {
            //We might not have a SLO since some editions don't support it
            string sloOption = string.IsNullOrEmpty(serviceLevelObjective) ?
                string.Empty : ", " + string.Format(CultureInfo.InvariantCulture, AzureServiceLevelObjectiveOptionFormat, serviceLevelObjective);

            return CreateAzureAlterDbStatement(dbName,
                string.Format(CultureInfo.InvariantCulture,
                    ModifyAzureDbStatementFormat,
                    edition,
                    maxSize,
                    sloOption));
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


