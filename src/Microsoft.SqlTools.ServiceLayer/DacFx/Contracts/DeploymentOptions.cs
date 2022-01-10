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

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Class to define deployment option default value and the description
    /// </summary>
    public class DeploymentOptionProps
    {
        public object value { get; set; }
        public string description { get; set; } = string.Empty;
    }
    /// <summary>
    /// Class to define deployment options. 
    /// Keeping the order and defaults same as DacFx
    /// The default values here should also match the default values in ADS UX
    /// </summary>
    public class DeploymentOptions
    {
        #region Properties

        public DeploymentOptionProps IgnoreTableOptions { get; set; }

        public DeploymentOptionProps IgnoreSemicolonBetweenStatements { get; set; }

        public DeploymentOptionProps IgnoreRouteLifetime { get; set; }

        public DeploymentOptionProps IgnoreRoleMembership { get; set; }

        public DeploymentOptionProps IgnoreQuotedIdentifiers { get; set; }

        public DeploymentOptionProps IgnorePermissions { get; set; }

        public DeploymentOptionProps IgnorePartitionSchemes { get; set; }

        public DeploymentOptionProps IgnoreObjectPlacementOnPartitionScheme { get; set; }

        public DeploymentOptionProps IgnoreNotForReplication { get; set; }

        public DeploymentOptionProps IgnoreLoginSids { get; set; }

        public DeploymentOptionProps IgnoreLockHintsOnIndexes { get; set; }

        public DeploymentOptionProps IgnoreKeywordCasing { get; set; }

        public DeploymentOptionProps IgnoreIndexPadding { get; set; }

        public DeploymentOptionProps IgnoreIndexOptions { get; set; }

        public DeploymentOptionProps IgnoreIncrement { get; set; }

        public DeploymentOptionProps IgnoreIdentitySeed { get; set; }

        public DeploymentOptionProps IgnoreUserSettingsObjects { get; set; }

        public DeploymentOptionProps IgnoreFullTextCatalogFilePath { get; set; }

        public DeploymentOptionProps IgnoreWhitespace { get; set; }

        public DeploymentOptionProps IgnoreWithNocheckOnForeignKeys { get; set; }

        public DeploymentOptionProps VerifyCollationCompatibility { get; set; }

        public DeploymentOptionProps UnmodifiableObjectWarnings { get; set; }

        public DeploymentOptionProps TreatVerificationErrorsAsWarnings { get; set; }

        public DeploymentOptionProps ScriptRefreshModule { get; set; }

        public DeploymentOptionProps ScriptNewConstraintValidation { get; set; }

        public DeploymentOptionProps ScriptFileSize { get; set; }

        public DeploymentOptionProps ScriptDeployStateChecks { get; set; }

        public DeploymentOptionProps ScriptDatabaseOptions { get; set; }

        public DeploymentOptionProps ScriptDatabaseCompatibility { get; set; }

        public DeploymentOptionProps ScriptDatabaseCollation { get; set; }

        public DeploymentOptionProps RunDeploymentPlanExecutors { get; set; }

        public DeploymentOptionProps RegisterDataTierApplication { get; set; }

        public DeploymentOptionProps PopulateFilesOnFileGroups { get; set; }

        public DeploymentOptionProps NoAlterStatementsToChangeClrTypes { get; set; }

        public DeploymentOptionProps IncludeTransactionalScripts { get; set; }

        public DeploymentOptionProps IncludeCompositeObjects { get; set; }

        public DeploymentOptionProps AllowUnsafeRowLevelSecurityDataMovement { get; set; }

        public DeploymentOptionProps IgnoreWithNocheckOnCheckConstraints { get; set; }

        public DeploymentOptionProps IgnoreFillFactor { get; set; }

        public DeploymentOptionProps IgnoreFileSize { get; set; }

        public DeploymentOptionProps IgnoreFilegroupPlacement { get; set; }

        public DeploymentOptionProps DoNotAlterReplicatedObjects { get; set; }

        public DeploymentOptionProps DoNotAlterChangeDataCaptureObjects { get; set; }

        public DeploymentOptionProps DisableAndReenableDdlTriggers { get; set; }

        public DeploymentOptionProps DeployDatabaseInSingleUserMode { get; set; }

        public DeploymentOptionProps CreateNewDatabase { get; set; }

        public DeploymentOptionProps CompareUsingTargetCollation { get; set; }

        public DeploymentOptionProps CommentOutSetVarDeclarations { get; set; }

        public DeploymentOptionProps CommandTimeout { get; set; } = new DeploymentOptionProps{ value = 120, description = string.Empty };

        public DeploymentOptionProps LongRunningCommandTimeout { get; set; } = new DeploymentOptionProps { value = 0, description = string.Empty };

        public DeploymentOptionProps DatabaseLockTimeout { get; set; } = new DeploymentOptionProps { value = 60, description = string.Empty };

        public DeploymentOptionProps BlockWhenDriftDetected { get; set; }

        public DeploymentOptionProps BlockOnPossibleDataLoss { get; set; }

        public DeploymentOptionProps BackupDatabaseBeforeChanges { get; set; }

        public DeploymentOptionProps AllowIncompatiblePlatform { get; set; }

        public DeploymentOptionProps AllowDropBlockingAssemblies { get; set; }

        public DeploymentOptionProps AdditionalDeploymentContributorArguments { get; set; }

        public DeploymentOptionProps AdditionalDeploymentContributors { get; set; }

        public DeploymentOptionProps DropConstraintsNotInSource { get; set; }

        public DeploymentOptionProps DropDmlTriggersNotInSource { get; set; }

        public DeploymentOptionProps DropExtendedPropertiesNotInSource { get; set; }

        public DeploymentOptionProps DropIndexesNotInSource { get; set; }

        public DeploymentOptionProps IgnoreFileAndLogFilePath { get; set; }

        public DeploymentOptionProps IgnoreExtendedProperties { get; set; }

        public DeploymentOptionProps IgnoreDmlTriggerState { get; set; }

        public DeploymentOptionProps IgnoreDmlTriggerOrder { get; set; }

        public DeploymentOptionProps IgnoreDefaultSchema { get; set; }

        public DeploymentOptionProps IgnoreDdlTriggerState { get; set; }

        public DeploymentOptionProps IgnoreDdlTriggerOrder { get; set; }

        public DeploymentOptionProps IgnoreCryptographicProviderFilePath { get; set; }

        public DeploymentOptionProps VerifyDeployment { get; set; }

        public DeploymentOptionProps IgnoreComments { get; set; }

        public DeploymentOptionProps IgnoreColumnCollation { get; set; }

        public DeploymentOptionProps IgnoreAuthorizer { get; set; }

        public DeploymentOptionProps IgnoreAnsiNulls { get; set; }

        public DeploymentOptionProps GenerateSmartDefaults { get; set; }

        public DeploymentOptionProps DropStatisticsNotInSource { get; set; }

        public DeploymentOptionProps DropRoleMembersNotInSource { get; set; }

        public DeploymentOptionProps DropPermissionsNotInSource { get; set; }

        public DeploymentOptionProps DropObjectsNotInSource { get; set; }

        public DeploymentOptionProps IgnoreColumnOrder { get; set; }

        public DeploymentOptionProps IgnoreTablePartitionOptions { get; set; } // DW Specific

        public DeploymentOptionProps AdditionalDeploymentContributorPaths { get; set; } = new DeploymentOptionProps { value = string.Empty, description = string.Empty };

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

        public DeploymentOptionProps AllowExternalLibraryPaths { get; set; }

        public DeploymentOptionProps AllowExternalLanguagePaths { get; set; }

        public DeploymentOptionProps DoNotEvaluateSqlCmdVariables { get; set; }

        public DeploymentOptionProps DisableParallelismForEnablingIndexes { get; set; }

        public DeploymentOptionProps DoNotDropWorkloadClassifiers { get; set; }

        public DeploymentOptionProps DisableIndexesForDataPhase { get; set; }

        public DeploymentOptionProps DoNotDropDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionProps HashObjectNamesInLogs { get; set; }

        public DeploymentOptionProps IgnoreWorkloadClassifiers { get; set; }

        public DeploymentOptionProps IgnoreDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionProps IsAlwaysEncryptedParameterizationEnabled { get; set; }

        public DeploymentOptionProps PreserveIdentityLastValues { get; set; }

        public DeploymentOptionProps RestoreSequenceCurrentValue { get; set; }

        public DeploymentOptionProps RebuildIndexesOfflineForDataPhase { get; set; }
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
                var attribute = prop.GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
                var description = (DescriptionAttribute)attribute;

                // Note that we set excluded object types here since dacfx has this value as null;
                if (prop != null && deployOptionsProp.Name != "ExcludeObjectTypes")
                {
                    if (deployOptionsProp.Name == "DoNotDropObjectTypes")
                    {
                        deployOptionsProp.SetValue(this, prop.GetValue(options));
                    }
                    else
                    {
                        DeploymentOptionProps setProp = new DeploymentOptionProps()
                        {
                            value = prop.GetValue(options),
                            description = description.Description
                        };
                        deployOptionsProp.SetValue(this, setProp);
                    }
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
                var attribute = prop.GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
                var description = (DescriptionAttribute)attribute;

                // Note that we set excluded object types here since dacfx has this value as null;
                if (prop != null)
                {
                    if (deployOptionsProp.Name == "DoNotDropObjectTypes" || deployOptionsProp.Name == "ExcludeObjectTypes")
                    {
                        deployOptionsProp.SetValue(this, prop.GetValue(options));
                    }
                    else
                    {
                        DeploymentOptionProps setProp = new DeploymentOptionProps()
                        {
                            value = prop.GetValue(options),
                            description = description.Description
                        };
                        deployOptionsProp.SetValue(this, setProp);
                    }
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
