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

        public DeploymentOptionProperty(T value, string description = "", string displayName = "")
        {
            this.Value = value;
            this.Description = description;
            this.DisplayName = displayName;
        }

        public T Value { get; set; }
        public string Description { get; set; } = string.Empty;

        // To Diaply the options in ADS extensions UI in SchemaCompare/SQL-DB-Project/Dacpac extensions
        public string DisplayName { get; set; }
    }
    /// <summary>
    /// Class to define deployment options. 
    /// Keeping the order and defaults same as DacFx
    /// The default values here should also match the default values in ADS UX
    /// </summary>
    public class DeploymentOptions
    {
        #region Properties

        public DeploymentOptionProperty<bool> IgnoreTableOptions { get; set; }

        public DeploymentOptionProperty<object> IgnoreSemicolonBetweenStatements { get; set; }

        public DeploymentOptionProperty<object> IgnoreRouteLifetime { get; set; }

        public DeploymentOptionProperty<object> IgnoreRoleMembership { get; set; }

        public DeploymentOptionProperty<object> IgnoreQuotedIdentifiers { get; set; }

        public DeploymentOptionProperty<object> IgnorePermissions { get; set; }

        public DeploymentOptionProperty<object> IgnorePartitionSchemes { get; set; }

        public DeploymentOptionProperty<object> IgnoreObjectPlacementOnPartitionScheme { get; set; }

        public DeploymentOptionProperty<object> IgnoreNotForReplication { get; set; }

        public DeploymentOptionProperty<object> IgnoreLoginSids { get; set; }

        public DeploymentOptionProperty<object> IgnoreLockHintsOnIndexes { get; set; }

        public DeploymentOptionProperty<object> IgnoreKeywordCasing { get; set; }

        public DeploymentOptionProperty<object> IgnoreIndexPadding { get; set; }

        public DeploymentOptionProperty<object> IgnoreIndexOptions { get; set; }

        public DeploymentOptionProperty<object> IgnoreIncrement { get; set; }

        public DeploymentOptionProperty<object> IgnoreIdentitySeed { get; set; }

        public DeploymentOptionProperty<object> IgnoreUserSettingsObjects { get; set; }

        public DeploymentOptionProperty<object> IgnoreFullTextCatalogFilePath { get; set; }

        public DeploymentOptionProperty<object> IgnoreWhitespace { get; set; }

        public DeploymentOptionProperty<object> IgnoreWithNocheckOnForeignKeys { get; set; }

        public DeploymentOptionProperty<object> VerifyCollationCompatibility { get; set; }

        public DeploymentOptionProperty<object> UnmodifiableObjectWarnings { get; set; }

        public DeploymentOptionProperty<object> TreatVerificationErrorsAsWarnings { get; set; }

        public DeploymentOptionProperty<object> ScriptRefreshModule { get; set; }

        public DeploymentOptionProperty<object> ScriptNewConstraintValidation { get; set; }

        public DeploymentOptionProperty<object> ScriptFileSize { get; set; }

        public DeploymentOptionProperty<object> ScriptDeployStateChecks { get; set; }

        public DeploymentOptionProperty<object> ScriptDatabaseOptions { get; set; }

        public DeploymentOptionProperty<object> ScriptDatabaseCompatibility { get; set; }

        public DeploymentOptionProperty<object> ScriptDatabaseCollation { get; set; }

        public DeploymentOptionProperty<object> RunDeploymentPlanExecutors { get; set; }

        public DeploymentOptionProperty<object> RegisterDataTierApplication { get; set; }

        public DeploymentOptionProperty<object> PopulateFilesOnFileGroups { get; set; }

        public DeploymentOptionProperty<object> NoAlterStatementsToChangeClrTypes { get; set; }

        public DeploymentOptionProperty<object> IncludeTransactionalScripts { get; set; }

        public DeploymentOptionProperty<object> IncludeCompositeObjects { get; set; }

        public DeploymentOptionProperty<object> AllowUnsafeRowLevelSecurityDataMovement { get; set; }

        public DeploymentOptionProperty<object> IgnoreWithNocheckOnCheckConstraints { get; set; }

        public DeploymentOptionProperty<object> IgnoreFillFactor { get; set; }

        public DeploymentOptionProperty<object> IgnoreFileSize { get; set; }

        public DeploymentOptionProperty<object> IgnoreFilegroupPlacement { get; set; }

        public DeploymentOptionProperty<object> DoNotAlterReplicatedObjects { get; set; }

        public DeploymentOptionProperty<object> DoNotAlterChangeDataCaptureObjects { get; set; }

        public DeploymentOptionProperty<object> DisableAndReenableDdlTriggers { get; set; }

        public DeploymentOptionProperty<object> DeployDatabaseInSingleUserMode { get; set; }

        public DeploymentOptionProperty<object> CreateNewDatabase { get; set; }

        public DeploymentOptionProperty<object> CompareUsingTargetCollation { get; set; }

        public DeploymentOptionProperty<object> CommentOutSetVarDeclarations { get; set; }

        public DeploymentOptionProperty<object> CommandTimeout { get; set; } = new DeploymentOptionProperty<object>(120);

        public DeploymentOptionProperty<object> LongRunningCommandTimeout { get; set; } = new DeploymentOptionProperty<object>(0);

        public DeploymentOptionProperty<object> DatabaseLockTimeout { get; set; } = new DeploymentOptionProperty<object>(60);

        public DeploymentOptionProperty<object> BlockWhenDriftDetected { get; set; }

        public DeploymentOptionProperty<object> BlockOnPossibleDataLoss { get; set; }

        public DeploymentOptionProperty<object> BackupDatabaseBeforeChanges { get; set; }

        public DeploymentOptionProperty<object> AllowIncompatiblePlatform { get; set; }

        public DeploymentOptionProperty<object> AllowDropBlockingAssemblies { get; set; }

        public DeploymentOptionProperty<object> AdditionalDeploymentContributorArguments { get; set; } = new DeploymentOptionProperty<object>(null);

        public DeploymentOptionProperty<object> AdditionalDeploymentContributors { get; set; } = new DeploymentOptionProperty<object>(null);

        public DeploymentOptionProperty<object> DropConstraintsNotInSource { get; set; }

        public DeploymentOptionProperty<object> DropDmlTriggersNotInSource { get; set; }

        public DeploymentOptionProperty<object> DropExtendedPropertiesNotInSource { get; set; }

        public DeploymentOptionProperty<object> DropIndexesNotInSource { get; set; }

        public DeploymentOptionProperty<object> IgnoreFileAndLogFilePath { get; set; }

        public DeploymentOptionProperty<object> IgnoreExtendedProperties { get; set; }

        public DeploymentOptionProperty<object> IgnoreDmlTriggerState { get; set; }

        public DeploymentOptionProperty<object> IgnoreDmlTriggerOrder { get; set; }

        public DeploymentOptionProperty<object> IgnoreDefaultSchema { get; set; }

        public DeploymentOptionProperty<object> IgnoreDdlTriggerState { get; set; }

        public DeploymentOptionProperty<object> IgnoreDdlTriggerOrder { get; set; }

        public DeploymentOptionProperty<object> IgnoreCryptographicProviderFilePath { get; set; }

        public DeploymentOptionProperty<object> VerifyDeployment { get; set; }

        public DeploymentOptionProperty<object> IgnoreComments { get; set; }

        public DeploymentOptionProperty<object> IgnoreColumnCollation { get; set; }

        public DeploymentOptionProperty<object> IgnoreAuthorizer { get; set; }

        public DeploymentOptionProperty<object> IgnoreAnsiNulls { get; set; }

        public DeploymentOptionProperty<object> GenerateSmartDefaults { get; set; }

        public DeploymentOptionProperty<object> DropStatisticsNotInSource { get; set; }

        public DeploymentOptionProperty<object> DropRoleMembersNotInSource { get; set; }

        public DeploymentOptionProperty<object> DropPermissionsNotInSource { get; set; }

        public DeploymentOptionProperty<object> DropObjectsNotInSource { get; set; }

        public DeploymentOptionProperty<object> IgnoreColumnOrder { get; set; }

        public DeploymentOptionProperty<object> IgnoreTablePartitionOptions { get; set; } // DW Specific

        public DeploymentOptionProperty<object> AdditionalDeploymentContributorPaths { get; set; } = new DeploymentOptionProperty<object>(null);

        public DeploymentOptionProperty<ObjectType[]> DoNotDropObjectTypes { get; set; } = new DeploymentOptionProperty<ObjectType[]>(null);

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

        public DeploymentOptionProperty<object> AllowExternalLibraryPaths { get; set; }

        public DeploymentOptionProperty<object> AllowExternalLanguagePaths { get; set; }

        public DeploymentOptionProperty<object> DoNotEvaluateSqlCmdVariables { get; set; }

        public DeploymentOptionProperty<object> DisableParallelismForEnablingIndexes { get; set; }

        public DeploymentOptionProperty<object> DoNotDropWorkloadClassifiers { get; set; }

        public DeploymentOptionProperty<object> DisableIndexesForDataPhase { get; set; }

        public DeploymentOptionProperty<object> DoNotDropDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionProperty<object> HashObjectNamesInLogs { get; set; }

        public DeploymentOptionProperty<object> IgnoreWorkloadClassifiers { get; set; }

        public DeploymentOptionProperty<object> IgnoreDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionProperty<object> IsAlwaysEncryptedParameterizationEnabled { get; set; }

        public DeploymentOptionProperty<object> PreserveIdentityLastValues { get; set; }

        public DeploymentOptionProperty<object> RestoreSequenceCurrentValue { get; set; }

        public DeploymentOptionProperty<object> RebuildIndexesOfflineForDataPhase { get; set; }

        private Dictionary<string, string> _displayNameMapDict;

        #endregion

        /// <summary>
        /// Mapping the DisplayName to the dac deploy option
        /// Adding new properties here would give easy handling of new option to all extensions
        /// </summary>
        private void SetDisplayNameForOption()
        {
            #region Display Name and Dac Options Mapping
            DacDeployOptions d = new DacDeployOptions();
            _displayNameMapDict = new Dictionary<string, string>();

            // Ex: displayNameMapDict["DacFx Option Name"] = "ADS UI Display Name"
            _displayNameMapDict[nameof(d.AdditionalDeploymentContributorArguments)] = "Additional Deployment Contributor Arguments";
            _displayNameMapDict[nameof(d.AdditionalDeploymentContributorPaths)] = "Additional Deployment Contributor Paths";
            _displayNameMapDict[nameof(d.AdditionalDeploymentContributors)] = "Additional Deployment Contributors";
            _displayNameMapDict[nameof(d.AllowDropBlockingAssemblies)] = "Allow Drop Blocking Assemblies";
            _displayNameMapDict[nameof(d.AllowExternalLanguagePaths)] = "Allow External Language Paths";
            _displayNameMapDict[nameof(d.AllowExternalLibraryPaths)] = "Allow External Library Paths";
            _displayNameMapDict[nameof(d.AllowIncompatiblePlatform)] = "Allow Incompatible Platform";
            _displayNameMapDict[nameof(d.AllowUnsafeRowLevelSecurityDataMovement)] = "Allow Unsafe RowLevel Security Data Movement";
            _displayNameMapDict[nameof(d.AzureSharedAccessSignatureToken)] = "Azure Shared Access Signature Token";
            _displayNameMapDict[nameof(d.AzureStorageBlobEndpoint)] = "Azure Storage Blob Endpoint";
            _displayNameMapDict[nameof(d.AzureStorageContainer)] = "Azure Storage Container";
            _displayNameMapDict[nameof(d.AzureStorageKey)] = "Azure Storage Key";
            _displayNameMapDict[nameof(d.AzureStorageRootPath)] = "Azure Storage Root Path";
            _displayNameMapDict[nameof(d.BackupDatabaseBeforeChanges)] = "Backup Database Before Changes";
            _displayNameMapDict[nameof(d.BlockOnPossibleDataLoss)] = "Block On Possible Data Loss";
            _displayNameMapDict[nameof(d.BlockWhenDriftDetected)] = "Block When Drift Detected";
            _displayNameMapDict[nameof(d.CommandTimeout)] = "Command Timeout";
            _displayNameMapDict[nameof(d.CommentOutSetVarDeclarations)] = "Comment Out SetVar Declarations";
            _displayNameMapDict[nameof(d.CompareUsingTargetCollation)] = "Compare Using Target Collation";
            _displayNameMapDict[nameof(d.CreateNewDatabase)] = "Create New Database";
            _displayNameMapDict[nameof(d.DatabaseLockTimeout)] = "Database Lock Timeout";
            _displayNameMapDict[nameof(d.DatabaseSpecification)] = "Database Specification";
            _displayNameMapDict[nameof(d.DataOperationStateProvider)] = "Data Operation State Provider";
            _displayNameMapDict[nameof(d.DeployDatabaseInSingleUserMode)] = "Deploy Database In Single User Mode";
            _displayNameMapDict[nameof(d.DisableAndReenableDdlTriggers)] = "Disable And Reenable Ddl Triggers";
            _displayNameMapDict[nameof(d.DisableIndexesForDataPhase)] = "Disable Indexes For Data Phase";
            _displayNameMapDict[nameof(d.DisableParallelismForEnablingIndexes)] = "Disable Parallelism For Enabling Indexes";
            _displayNameMapDict[nameof(d.DoNotAlterChangeDataCaptureObjects)] = "Do Not Alter Change Data Capture Objects";
            _displayNameMapDict[nameof(d.DoNotAlterReplicatedObjects)] = "Do Not Alter Replicated Objects";
            _displayNameMapDict[nameof(d.DoNotDropDatabaseWorkloadGroups)] = "Do Not Drop Database Workload Groups";
            _displayNameMapDict[nameof(d.DoNotDropObjectTypes)] = "Do Not Drop Object Types";
            _displayNameMapDict[nameof(d.DoNotDropWorkloadClassifiers)] = "Do Not Drop Workload Classifiers";
            _displayNameMapDict[nameof(d.DoNotEvaluateSqlCmdVariables)] = "Do Not Evaluate Sql Cmd Variables";
            _displayNameMapDict[nameof(d.DropConstraintsNotInSource)] = "Drop Constraints Not In Source";
            _displayNameMapDict[nameof(d.DropDmlTriggersNotInSource)] = "Drop Dml Triggers Not In Source";
            _displayNameMapDict[nameof(d.DropExtendedPropertiesNotInSource)] = "Drop Extended Properties Not In Source";
            _displayNameMapDict[nameof(d.DropIndexesNotInSource)] = "Drop Indexes NotInSource";
            _displayNameMapDict[nameof(d.DropObjectsNotInSource)] = "Drop Objects NotInSource";
            _displayNameMapDict[nameof(d.DropPermissionsNotInSource)] = "Drop Permissions Not In Source";
            _displayNameMapDict[nameof(d.DropRoleMembersNotInSource)] = "Drop Role Members Not In Source";
            _displayNameMapDict[nameof(d.DropStatisticsNotInSource)] = "Drop Statistics Not In Source";
            _displayNameMapDict[nameof(d.EnclaveAttestationProtocol)] = "Enclave Attestation Protocol";
            _displayNameMapDict[nameof(d.EnclaveAttestationUrl)] = "Enclave Attestation Url";
            _displayNameMapDict[nameof(d.ExcludeObjectTypes)] = "Exclude Object Types";
            _displayNameMapDict[nameof(d.GenerateSmartDefaults)] = "Generate Smart Defaults";
            _displayNameMapDict[nameof(d.HashObjectNamesInLogs)] = "Hash Object Names In Logs";
            _displayNameMapDict[nameof(d.IgnoreAnsiNulls)] = "Ignore Ansi Nulls";
            _displayNameMapDict[nameof(d.IgnoreAuthorizer)] = "Ignore Authorizer";
            _displayNameMapDict[nameof(d.IgnoreColumnCollation)] = "Ignore Column Collation";
            _displayNameMapDict[nameof(d.IgnoreColumnOrder)] = "Ignore Column Order";
            _displayNameMapDict[nameof(d.IgnoreComments)] = "Ignore Comments";
            _displayNameMapDict[nameof(d.IgnoreCryptographicProviderFilePath)] = "Ignore Cryptographic Provider File Path";
            _displayNameMapDict[nameof(d.IgnoreDatabaseWorkloadGroups)] = "Ignore Database Workload Groups";
            _displayNameMapDict[nameof(d.IgnoreDdlTriggerOrder)] = "Ignore Ddl Trigger Order";
            _displayNameMapDict[nameof(d.IgnoreDdlTriggerState)] = "Ignore Ddl Trigger State";
            _displayNameMapDict[nameof(d.IgnoreDefaultSchema)] = "Ignore Default Schema";
            _displayNameMapDict[nameof(d.IgnoreDmlTriggerOrder)] = "Ignore Dml Trigger Order";
            _displayNameMapDict[nameof(d.IgnoreDmlTriggerState)] = "Ignore Dml Trigger State";
            _displayNameMapDict[nameof(d.IgnoreExtendedProperties)] = "Ignore Extended Properties";
            _displayNameMapDict[nameof(d.IgnoreFileAndLogFilePath)] = "Ignore File And Log File Path";
            _displayNameMapDict[nameof(d.IgnoreFileSize)] = "Ignore File Size";
            _displayNameMapDict[nameof(d.IgnoreFilegroupPlacement)] = "Ignore File group Placement";
            _displayNameMapDict[nameof(d.IgnoreFillFactor)] = "Ignore Fill Factor";
            _displayNameMapDict[nameof(d.IgnoreFullTextCatalogFilePath)] = "Ignore Full Text Catalog File Path";
            _displayNameMapDict[nameof(d.IgnoreIdentitySeed)] = "Ignore Identity Seed";
            _displayNameMapDict[nameof(d.IgnoreIncrement)] = "Ignore Increment";
            _displayNameMapDict[nameof(d.IgnoreIndexOptions)] = "Ignore Index Options";
            _displayNameMapDict[nameof(d.IgnoreIndexPadding)] = "Ignore Index Padding";
            _displayNameMapDict[nameof(d.IgnoreKeywordCasing)] = "Ignore Keyword Casing";
            _displayNameMapDict[nameof(d.IgnoreLockHintsOnIndexes)] = "IgnoreLock Hints On Indexes";
            _displayNameMapDict[nameof(d.IgnoreLoginSids)] = "IgnoreLogin Sids";
            _displayNameMapDict[nameof(d.IgnoreNotForReplication)] = "IgnoreNotForReplication";
            _displayNameMapDict[nameof(d.IgnoreObjectPlacementOnPartitionScheme)] = "Ignore Object Placement On Partition Scheme";
            _displayNameMapDict[nameof(d.IgnorePartitionSchemes)] = "Ignore Partition Schemes";
            _displayNameMapDict[nameof(d.IgnorePermissions)] = "Ignore Permissions";
            _displayNameMapDict[nameof(d.IgnoreQuotedIdentifiers)] = "Ignore Quoted Identifiers";
            _displayNameMapDict[nameof(d.IgnoreRoleMembership)] = "Ignore Role Membership";
            _displayNameMapDict[nameof(d.IgnoreRouteLifetime)] = "Ignore Route Lifetime";
            _displayNameMapDict[nameof(d.IgnoreSemicolonBetweenStatements)] = "Ignore Semicolon Between Statements";
            _displayNameMapDict[nameof(d.IgnoreTableOptions)] = "Ignore Table Options";
            _displayNameMapDict[nameof(d.IgnoreTablePartitionOptions)] = "Ignore Table Partition Options";
            _displayNameMapDict[nameof(d.IgnoreUserSettingsObjects)] = "Ignore User Settings Objects";
            _displayNameMapDict[nameof(d.IgnoreWhitespace)] = "Ignore Whitespace";
            _displayNameMapDict[nameof(d.IgnoreWithNocheckOnCheckConstraints)] = "Ignore With Nocheck On Check Constraints";
            _displayNameMapDict[nameof(d.IgnoreWithNocheckOnForeignKeys)] = "Ignore With Nocheck On Foreign Keys";
            _displayNameMapDict[nameof(d.IgnoreWorkloadClassifiers)] = "Ignore Workload Classifiers";
            _displayNameMapDict[nameof(d.IncludeCompositeObjects)] = "Include Composite Objects";
            _displayNameMapDict[nameof(d.IncludeTransactionalScripts)] = "Include Transactional Scripts";
            _displayNameMapDict[nameof(d.IsAlwaysEncryptedParameterizationEnabled)] = "Is Always Encrypted Parameterization Enabled";
            _displayNameMapDict[nameof(d.LongRunningCommandTimeout)] = "Long Running Command Timeout";
            _displayNameMapDict[nameof(d.NoAlterStatementsToChangeClrTypes)] = "No Alter Statements To Change ClrTypes";
            _displayNameMapDict[nameof(d.PopulateFilesOnFileGroups)] = "Populate Files On File Groups";
            _displayNameMapDict[nameof(d.PreserveIdentityLastValues)] = "Preserve Identity Last Values";
            _displayNameMapDict[nameof(d.RebuildIndexesOfflineForDataPhase)] = "Rebuild Indexes Offline For Data Phase";
            _displayNameMapDict[nameof(d.RegisterDataTierApplication)] = "Register Data Tier Application";
            _displayNameMapDict[nameof(d.RestoreSequenceCurrentValue)] = "Restore Sequence Current Value";
            _displayNameMapDict[nameof(d.RunDeploymentPlanExecutors)] = "Run Deployment Plan Executors";
            _displayNameMapDict[nameof(d.ScriptDatabaseCollation)] = "Script Database Collation";
            _displayNameMapDict[nameof(d.ScriptDatabaseCompatibility)] = "Script Database Compatibility";
            _displayNameMapDict[nameof(d.ScriptDatabaseOptions)] = "Script Database Options";
            _displayNameMapDict[nameof(d.ScriptDeployStateChecks)] = "Script DeployS tate Checks";
            _displayNameMapDict[nameof(d.ScriptFileSize)] = "Script File Size";
            _displayNameMapDict[nameof(d.ScriptNewConstraintValidation)] = "Script New Constraint Validation";
            _displayNameMapDict[nameof(d.ScriptRefreshModule)] = "Script Refresh Module";
            _displayNameMapDict[nameof(d.SqlCommandVariableValues)] = "Sql Command Variable Values";
            _displayNameMapDict[nameof(d.TreatVerificationErrorsAsWarnings)] = "Treat Verification Errors As Warnings";
            _displayNameMapDict[nameof(d.UnmodifiableObjectWarnings)] = "Unmodifiable Object Warnings";
            _displayNameMapDict[nameof(d.VerifyCollationCompatibility)] = "Verify Collation Compatibility";
            _displayNameMapDict[nameof(d.VerifyDeployment)] = "Verify Deployment";
            #endregion
        }

        public DeploymentOptions()
        {
            DacDeployOptions options = new DacDeployOptions();

            // Setting Display names for all dacDeploy options
            SetDisplayNameForOption();

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
            // Setting Display names for all dacDeploy options
            SetDisplayNameForOption();

            SetOptions(options);
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
            var attribute = prop.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault();
            Type type = typeof(DeploymentOptionProperty<>).MakeGenericType(val.GetType());
            object setProp = Activator.CreateInstance(type, val, attribute.Description, _displayNameMapDict[deployOptionsProp.Name]);
            deployOptionsProp.SetValue(this, setProp);
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
