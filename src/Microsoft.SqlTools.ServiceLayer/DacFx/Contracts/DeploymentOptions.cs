//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using System.Reflection;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Class to define deployment option default value and the description
    /// </summary>
    public class DeploymentOptionProperty<T>
    {
        public DeploymentOptionProperty(T value, string description = "", string propertyName = "")
        {
            this.Value = value;
            this.Description = description;
            this.PropertyName = propertyName;
        }

        // Default and selected value of the deployment options
        public T Value { get; set; }

        // Description of the deployment options
        public string Description { get; set; }

        // To display the options in ADS extensions UI in SchemaCompare/SQL-DB-Project/Dacpac extensions
        public string PropertyName { get; set; }
    }

    /// <summary>
    /// Class to define deployment options.
    /// Keeping the order and defaults same as DacFx
    /// The default values here should also match the default values in ADS UX
    /// NOTE: When new deployment options are added in DacFx, they need to be added here too
    /// </summary>
    public class DeploymentOptions
    {
        #region Properties

        public DeploymentOptionProperty<bool> IgnoreTableOptions { get; set; }

        public DeploymentOptionProperty<bool> IgnoreSemicolonBetweenStatements { get; set; }

        public DeploymentOptionProperty<bool> IgnoreRouteLifetime { get; set; }

        public DeploymentOptionProperty<bool> IgnoreRoleMembership { get; set; }

        public DeploymentOptionProperty<bool> IgnoreQuotedIdentifiers { get; set; }

        public DeploymentOptionProperty<bool> IgnorePermissions { get; set; }

        public DeploymentOptionProperty<bool> IgnorePartitionSchemes { get; set; }

        public DeploymentOptionProperty<bool> IgnoreObjectPlacementOnPartitionScheme { get; set; }

        public DeploymentOptionProperty<bool> IgnoreNotForReplication { get; set; }

        public DeploymentOptionProperty<bool> IgnoreLoginSids { get; set; }

        public DeploymentOptionProperty<bool> IgnoreLockHintsOnIndexes { get; set; }

        public DeploymentOptionProperty<bool> IgnoreKeywordCasing { get; set; }

        public DeploymentOptionProperty<bool> IgnoreIndexPadding { get; set; }

        public DeploymentOptionProperty<bool> IgnoreIndexOptions { get; set; }

        public DeploymentOptionProperty<bool> IgnoreIncrement { get; set; }

        public DeploymentOptionProperty<bool> IgnoreIdentitySeed { get; set; }

        public DeploymentOptionProperty<bool> IgnoreUserSettingsObjects { get; set; }

        public DeploymentOptionProperty<bool> IgnoreFullTextCatalogFilePath { get; set; }

        public DeploymentOptionProperty<bool> IgnoreWhitespace { get; set; }

        public DeploymentOptionProperty<bool> IgnoreWithNocheckOnForeignKeys { get; set; }

        public DeploymentOptionProperty<bool> VerifyCollationCompatibility { get; set; }

        public DeploymentOptionProperty<bool> UnmodifiableObjectWarnings { get; set; }

        public DeploymentOptionProperty<bool> TreatVerificationErrorsAsWarnings { get; set; }

        public DeploymentOptionProperty<bool> ScriptRefreshModule { get; set; }

        public DeploymentOptionProperty<bool> ScriptNewConstraintValidation { get; set; }

        public DeploymentOptionProperty<bool> ScriptFileSize { get; set; }

        public DeploymentOptionProperty<bool> ScriptDeployStateChecks { get; set; }

        public DeploymentOptionProperty<bool> ScriptDatabaseOptions { get; set; }

        public DeploymentOptionProperty<bool> ScriptDatabaseCompatibility { get; set; }

        public DeploymentOptionProperty<bool> ScriptDatabaseCollation { get; set; }

        public DeploymentOptionProperty<bool> RunDeploymentPlanExecutors { get; set; }

        public DeploymentOptionProperty<bool> RegisterDataTierApplication { get; set; }

        public DeploymentOptionProperty<bool> PopulateFilesOnFileGroups { get; set; }

        public DeploymentOptionProperty<bool> NoAlterStatementsToChangeClrTypes { get; set; }

        public DeploymentOptionProperty<bool> IncludeTransactionalScripts { get; set; }

        public DeploymentOptionProperty<bool> IncludeCompositeObjects { get; set; }

        public DeploymentOptionProperty<bool> AllowUnsafeRowLevelSecurityDataMovement { get; set; }

        public DeploymentOptionProperty<bool> IgnoreWithNocheckOnCheckConstraints { get; set; }

        public DeploymentOptionProperty<bool> IgnoreFillFactor { get; set; }

        public DeploymentOptionProperty<bool> IgnoreFileSize { get; set; }

        public DeploymentOptionProperty<bool> IgnoreFilegroupPlacement { get; set; }

        public DeploymentOptionProperty<bool> DoNotAlterReplicatedObjects { get; set; }

        public DeploymentOptionProperty<bool> DoNotAlterChangeDataCaptureObjects { get; set; }

        public DeploymentOptionProperty<bool> DisableAndReenableDdlTriggers { get; set; }

        public DeploymentOptionProperty<bool> DeployDatabaseInSingleUserMode { get; set; }

        public DeploymentOptionProperty<bool> CreateNewDatabase { get; set; }

        public DeploymentOptionProperty<bool> CompareUsingTargetCollation { get; set; }

        public DeploymentOptionProperty<bool> CommentOutSetVarDeclarations { get; set; }

        // Command timeout to 120 seconds when executing queries against SQL Server.
        public DeploymentOptionProperty<int> CommandTimeout { get; set; } = new DeploymentOptionProperty<int>(120);

        // LongRunningCommandTimeout 0 seconds to wait indefinitely.
        public DeploymentOptionProperty<int> LongRunningCommandTimeout { get; set; } = new DeploymentOptionProperty<int>(0);

        // Wait 60 seconds to lock database when executing queries against SQL Server.
        public DeploymentOptionProperty<int> DatabaseLockTimeout { get; set; } = new DeploymentOptionProperty<int>(60);

        public DeploymentOptionProperty<bool> BlockWhenDriftDetected { get; set; }

        public DeploymentOptionProperty<bool> BlockOnPossibleDataLoss { get; set; }

        public DeploymentOptionProperty<bool> BackupDatabaseBeforeChanges { get; set; }

        public DeploymentOptionProperty<bool> AllowIncompatiblePlatform { get; set; }

        public DeploymentOptionProperty<bool> AllowDropBlockingAssemblies { get; set; }

        public DeploymentOptionProperty<string> AdditionalDeploymentContributorArguments { get; set; }

        public DeploymentOptionProperty<string> AdditionalDeploymentContributors { get; set; }

        public DeploymentOptionProperty<bool> DropConstraintsNotInSource { get; set; }

        public DeploymentOptionProperty<bool> DropDmlTriggersNotInSource { get; set; }

        public DeploymentOptionProperty<bool> DropExtendedPropertiesNotInSource { get; set; }

        public DeploymentOptionProperty<bool> DropIndexesNotInSource { get; set; }

        public DeploymentOptionProperty<bool> IgnoreFileAndLogFilePath { get; set; }

        public DeploymentOptionProperty<bool> IgnoreExtendedProperties { get; set; }

        public DeploymentOptionProperty<bool> IgnoreDmlTriggerState { get; set; }

        public DeploymentOptionProperty<bool> IgnoreDmlTriggerOrder { get; set; }

        public DeploymentOptionProperty<bool> IgnoreDefaultSchema { get; set; }

        public DeploymentOptionProperty<bool> IgnoreDdlTriggerState { get; set; }

        public DeploymentOptionProperty<bool> IgnoreDdlTriggerOrder { get; set; }

        public DeploymentOptionProperty<bool> IgnoreCryptographicProviderFilePath { get; set; }

        public DeploymentOptionProperty<bool> VerifyDeployment { get; set; }

        public DeploymentOptionProperty<bool> IgnoreComments { get; set; }

        public DeploymentOptionProperty<bool> IgnoreColumnCollation { get; set; }

        public DeploymentOptionProperty<bool> IgnoreAuthorizer { get; set; }

        public DeploymentOptionProperty<bool> IgnoreAnsiNulls { get; set; }

        public DeploymentOptionProperty<bool> GenerateSmartDefaults { get; set; }

        public DeploymentOptionProperty<bool> DropStatisticsNotInSource { get; set; }

        public DeploymentOptionProperty<bool> DropRoleMembersNotInSource { get; set; }

        public DeploymentOptionProperty<bool> DropPermissionsNotInSource { get; set; }

        public DeploymentOptionProperty<bool> DropObjectsNotInSource { get; set; }

        public DeploymentOptionProperty<bool> IgnoreColumnOrder { get; set; }

        public DeploymentOptionProperty<bool> IgnoreTablePartitionOptions { get; set; } // DW Specific

        public DeploymentOptionProperty<string> AdditionalDeploymentContributorPaths { get; set; }

        public DeploymentOptionProperty<ObjectType[]> DoNotDropObjectTypes { get; set; }

        public DeploymentOptionProperty<ObjectType[]> ExcludeObjectTypes { get; set; } = new DeploymentOptionProperty<ObjectType[]>
        (
            new ObjectType[] {
                ObjectType.ServerTriggers,
                ObjectType.Routes,
                ObjectType.LinkedServerLogins,
                ObjectType.Endpoints,
                ObjectType.ErrorMessages,
                ObjectType.Files,
                ObjectType.Logins,
                ObjectType.LinkedServers,
                ObjectType.Credentials,
                ObjectType.DatabaseScopedCredentials,
                ObjectType.DatabaseEncryptionKeys,
                ObjectType.MasterKeys,
                ObjectType.DatabaseAuditSpecifications,
                ObjectType.Audits,
                ObjectType.ServerAuditSpecifications,
                ObjectType.CryptographicProviders,
                ObjectType.ServerRoles,
                ObjectType.EventSessions,
                ObjectType.DatabaseOptions,
                ObjectType.EventNotifications,
                ObjectType.ServerRoleMembership,
                ObjectType.AssemblyFiles
            }
        );

        public DeploymentOptionProperty<bool> AllowExternalLibraryPaths { get; set; }

        public DeploymentOptionProperty<bool> AllowExternalLanguagePaths { get; set; }

        public DeploymentOptionProperty<bool> DoNotEvaluateSqlCmdVariables { get; set; }

        public DeploymentOptionProperty<bool> DisableParallelismForEnablingIndexes { get; set; }

        public DeploymentOptionProperty<bool> DoNotDropWorkloadClassifiers { get; set; }

        public DeploymentOptionProperty<bool> DisableIndexesForDataPhase { get; set; }

        public DeploymentOptionProperty<bool> DoNotDropDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionProperty<bool> HashObjectNamesInLogs { get; set; }

        public DeploymentOptionProperty<bool> IgnoreWorkloadClassifiers { get; set; }

        public DeploymentOptionProperty<bool> IgnoreDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionProperty<bool> IsAlwaysEncryptedParameterizationEnabled { get; set; }

        public DeploymentOptionProperty<bool> PreserveIdentityLastValues { get; set; }

        public DeploymentOptionProperty<bool> RestoreSequenceCurrentValue { get; set; }

        public DeploymentOptionProperty<bool> RebuildIndexesOfflineForDataPhase { get; set; }

        public DeploymentOptionProperty<bool> IgnoreSensitivityClassifications { get; set; }

        public Dictionary<string, DeploymentOptionProperty<bool>> OptionsMapTable { get; set; }


        public Dictionary<string, int> IncludeObjectsTable;

        #endregion

        public DeploymentOptions()
        {
            DacDeployOptions options = new DacDeployOptions();
            OptionsMapTable = new Dictionary<string, DeploymentOptionProperty<bool>>();

            // Prepare include objects types table
            CreateIncludeObjectsTable();

            // Adding these defaults to ensure behavior similarity with other tools. Dacfx and SSMS import/export wizards use these defaults.
            // Tracking the full fix : https://github.com/microsoft/azuredatastudio/issues/5599
            options.AllowDropBlockingAssemblies = true;
            options.AllowIncompatiblePlatform = true;
            options.DropObjectsNotInSource = true;
            options.DropPermissionsNotInSource = true;
            options.DropRoleMembersNotInSource = true;
            options.IgnoreKeywordCasing = false;
            options.IgnoreSemicolonBetweenStatements = false;

            PropertyInfo[] deploymentOptionsProperties = this.GetType().GetProperties();

            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                var prop = options.GetType().GetProperty(deployOptionsProp.Name);

                // Note that we set excluded object types here since dacfx has this value as null;
                if (prop != null && deployOptionsProp.Name != "ExcludeObjectTypes")
                {
                    // Setting DacFx default values to the generic deployment options properties.
                    SetGenericDeployOptionProps(prop, options, deployOptionsProp);
                }
            }
        }

        public DeploymentOptions(DacDeployOptions options)
        {
            OptionsMapTable = new Dictionary<string, DeploymentOptionProperty<bool>>();

            // Prepare include objects types table
            CreateIncludeObjectsTable();

            SetOptions(options);
        }

        /// <summary>
        /// Sets include objects enum values and number in to the dictionary
        /// </summary>
        public void CreateIncludeObjectsTable()
        {
            // Set include objects table data
            var objectTypeEnum = typeof(ObjectType);
            IncludeObjectsTable = Enum.GetNames(objectTypeEnum).ToDictionary(t => t, t => (int)System.Enum.Parse(objectTypeEnum, t));
        }

        /// <summary>
        /// initialize deployment options from the options in a publish profile.xml
        /// </summary>
        /// <param name="options">options created from the profile</param>
        /// <param name="profilePath"></param>
        public async Task InitializeFromProfile(DacDeployOptions options, string profilePath)
        {
            // check if defaults need to be set if they aren't specified in the profile
            string contents = await File.ReadAllTextAsync(profilePath);
            if (!contents.Contains("<AllowDropBlockingAssemblies>"))
            {
                options.AllowDropBlockingAssemblies = true;
            }
            if (!contents.Contains("<AllowIncompatiblePlatform>"))
            {
                options.AllowIncompatiblePlatform = true;
            }
            if (!contents.Contains("<DropObjectsNotInSource>"))
            {
                options.DropObjectsNotInSource = true;
            }
            if (!contents.Contains("<DropPermissionsNotInSource>"))
            {
                options.DropPermissionsNotInSource = true;
            }
            if (!contents.Contains("<DropRoleMembersNotInSource>"))
            {
                options.DropRoleMembersNotInSource = true;
            }
            if (!contents.Contains("<IgnoreKeywordCasing>"))
            {
                options.IgnoreKeywordCasing = false;
            }
            if (!contents.Contains("<IgnoreSemicolonBetweenStatements>"))
            {
                options.IgnoreSemicolonBetweenStatements = false;
            }

            SetOptions(options);
        }

        public void SetOptions(DacDeployOptions options)
        {
            System.Reflection.PropertyInfo[] deploymentOptionsProperties = this.GetType().GetProperties();

            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                var prop = options.GetType().GetProperty(deployOptionsProp.Name);
                // Note that we set excluded object types here since dacfx has this value as null;
                if (prop != null)
                {
                    SetGenericDeployOptionProps(prop, options, deployOptionsProp);
                }
            }
        }

        /// <summary>
        /// Sets the Value and Description to all properties
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="val"></param>
        /// <param name="deployOptionsProp"></param>
        public void SetGenericDeployOptionProps(PropertyInfo prop, DacDeployOptions options, PropertyInfo deployOptionsProp)
        {
            var val = prop.GetValue(options);
            var descriptionAttribute = prop.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault();
            var displayNameAttribute = prop.GetCustomAttributes<DisplayNameAttribute>(true).FirstOrDefault();
            Type type = val != null ? typeof(DeploymentOptionProperty<>).MakeGenericType(val.GetType()) 
                : typeof(DeploymentOptionProperty<>).MakeGenericType(prop.PropertyType);
            object setProp = Activator.CreateInstance(type, val, descriptionAttribute.Description, deployOptionsProp.Name);
            deployOptionsProp.SetValue(this, setProp);

            // All boolean options must go into optionsMapTable
            if (setProp.GetType() == typeof(DeploymentOptionProperty<bool>))
            {
                this.OptionsMapTable[displayNameAttribute.DisplayName] = (DeploymentOptionProperty<bool>)setProp;
            }
        }

        public static DeploymentOptions GetDefaultSchemaCompareOptions()
        {
            return new DeploymentOptions();
        }

        public static DeploymentOptions GetDefaultPublishOptions()
        {
            DeploymentOptions result = new DeploymentOptions();

            result.ExcludeObjectTypes.Value = result.ExcludeObjectTypes.Value.Where(x => x != ObjectType.DatabaseScopedCredentials).ToArray(); // re-include database-scoped credentials

            return result;
        }
    }
}
