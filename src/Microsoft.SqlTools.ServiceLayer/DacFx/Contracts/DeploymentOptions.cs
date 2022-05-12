﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Class to define deployment options. 
    /// Keeping the order and defaults same as DacFx
    /// The default values here should also match the default values in ADS UX
    /// </summary>
    public class DeploymentOptions
    {
        #region Properties

        public bool IgnoreTableOptions { get; set; }

        public bool IgnoreSemicolonBetweenStatements { get; set; }

        public bool IgnoreRouteLifetime { get; set; }

        public bool IgnoreRoleMembership { get; set; }

        public bool IgnoreQuotedIdentifiers { get; set; }

        public bool IgnorePermissions { get; set; }

        public bool IgnorePartitionSchemes { get; set; }

        public bool IgnoreObjectPlacementOnPartitionScheme { get; set; }

        public bool IgnoreNotForReplication { get; set; }

        public bool IgnoreLoginSids { get; set; }

        public bool IgnoreLockHintsOnIndexes { get; set; }

        public bool IgnoreKeywordCasing { get; set; }

        public bool IgnoreIndexPadding { get; set; }

        public bool IgnoreIndexOptions { get; set; }

        public bool IgnoreIncrement { get; set; }

        public bool IgnoreIdentitySeed { get; set; }

        public bool IgnoreUserSettingsObjects { get; set; }

        public bool IgnoreFullTextCatalogFilePath { get; set; }

        public bool IgnoreWhitespace { get; set; }

        public bool IgnoreWithNocheckOnForeignKeys { get; set; }

        public bool VerifyCollationCompatibility { get; set; }

        public bool UnmodifiableObjectWarnings { get; set; }

        public bool TreatVerificationErrorsAsWarnings { get; set; }

        public bool ScriptRefreshModule { get; set; }

        public bool ScriptNewConstraintValidation { get; set; }

        public bool ScriptFileSize { get; set; }

        public bool ScriptDeployStateChecks { get; set; }

        public bool ScriptDatabaseOptions { get; set; }

        public bool ScriptDatabaseCompatibility { get; set; }

        public bool ScriptDatabaseCollation { get; set; }

        public bool RunDeploymentPlanExecutors { get; set; }

        public bool RegisterDataTierApplication { get; set; }

        public bool PopulateFilesOnFileGroups { get; set; }

        public bool NoAlterStatementsToChangeClrTypes { get; set; }

        public bool IncludeTransactionalScripts { get; set; }

        public bool IncludeCompositeObjects { get; set; }

        public bool AllowUnsafeRowLevelSecurityDataMovement { get; set; }

        public bool IgnoreWithNocheckOnCheckConstraints { get; set; }

        public bool IgnoreFillFactor { get; set; }

        public bool IgnoreFileSize { get; set; }

        public bool IgnoreFilegroupPlacement { get; set; }

        public bool DoNotAlterReplicatedObjects { get; set; }

        public bool DoNotAlterChangeDataCaptureObjects { get; set; }

        public bool DisableAndReenableDdlTriggers { get; set; }

        public bool DeployDatabaseInSingleUserMode { get; set; }

        public bool CreateNewDatabase { get; set; }

        public bool CompareUsingTargetCollation { get; set; }

        public bool CommentOutSetVarDeclarations { get; set; }

        public int CommandTimeout { get; set; } = 120;

        public int LongRunningCommandTimeout { get; set; } = 0;

        public int DatabaseLockTimeout { get; set; } = 60;

        public bool BlockWhenDriftDetected { get; set; }

        public bool BlockOnPossibleDataLoss { get; set; }

        public bool BackupDatabaseBeforeChanges { get; set; }

        public bool AllowIncompatiblePlatform { get; set; }

        public bool AllowDropBlockingAssemblies { get; set; }

        public string AdditionalDeploymentContributorArguments { get; set; }

        public string AdditionalDeploymentContributors { get; set; }

        public bool DropConstraintsNotInSource { get; set; }

        public bool DropDmlTriggersNotInSource { get; set; }

        public bool DropExtendedPropertiesNotInSource { get; set; }

        public bool DropIndexesNotInSource { get; set; }

        public bool IgnoreFileAndLogFilePath { get; set; }

        public bool IgnoreExtendedProperties { get; set; }

        public bool IgnoreDmlTriggerState { get; set; }

        public bool IgnoreDmlTriggerOrder { get; set; }

        public bool IgnoreDefaultSchema { get; set; }

        public bool IgnoreDdlTriggerState { get; set; }

        public bool IgnoreDdlTriggerOrder { get; set; }

        public bool IgnoreCryptographicProviderFilePath { get; set; }

        public bool VerifyDeployment { get; set; }

        public bool IgnoreComments { get; set; }

        public bool IgnoreColumnCollation { get; set; }

        public bool IgnoreAuthorizer { get; set; }

        public bool IgnoreAnsiNulls { get; set; }

        public bool GenerateSmartDefaults { get; set; }

        public bool DropStatisticsNotInSource { get; set; }

        public bool DropRoleMembersNotInSource { get; set; }

        public bool DropPermissionsNotInSource { get; set; }

        public bool DropObjectsNotInSource { get; set; }

        public bool IgnoreColumnOrder { get; set; }

        public bool IgnoreTablePartitionOptions { get; set; } // DW Specific

        public string AdditionalDeploymentContributorPaths { get; set; } = string.Empty;

        public ObjectType[] DoNotDropObjectTypes { get; set; } = null;

        public ObjectType[] ExcludeObjectTypes { get; set; } =
        {
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
                ObjectType.AssemblyFiles,
        };

        #endregion

        public DeploymentOptions()
        {
            DacDeployOptions options = new DacDeployOptions();

            // Adding these defaults to ensure behavior similarity with other tools. Dacfx and SSMS import/export wizards use these defaults.
            // Tracking the full fix : https://github.com/microsoft/azuredatastudio/issues/5599
            options.AllowDropBlockingAssemblies = true;
            options.AllowIncompatiblePlatform = true;
            options.DropObjectsNotInSource = true;
            options.DropPermissionsNotInSource = true;
            options.DropRoleMembersNotInSource = true;
            options.IgnoreKeywordCasing = false;
            options.IgnoreSemicolonBetweenStatements = false;

            System.Reflection.PropertyInfo[] deploymentOptionsProperties = this.GetType().GetProperties();

            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                var prop = options.GetType().GetProperty(deployOptionsProp.Name);

                // Note that we set excluded object types here since dacfx has this value as null;
                if (prop != null && deployOptionsProp.Name != "ExcludeObjectTypes")
                {
                    deployOptionsProp.SetValue(this, prop.GetValue(options));
                }
            }
        }

        public DeploymentOptions(DacDeployOptions options)
        {
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

                if (prop != null)
                {
                    deployOptionsProp.SetValue(this, prop.GetValue(options));
                }
            }
        }

        public static DeploymentOptions GetDefaultSchemaCompareOptions()
        {
            return new DeploymentOptions();
        }

        public static DeploymentOptions GetDefaultPublishOptions()
        {
            DeploymentOptions result = new DeploymentOptions();

            result.ExcludeObjectTypes = result.ExcludeObjectTypes.Where(x => x != ObjectType.DatabaseScopedCredentials).ToArray(); // re-include database-scoped credentials

            return result;
        }
    }
}
