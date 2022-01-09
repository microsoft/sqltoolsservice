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
    public class DeploymentOptionsProps
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

        public DeploymentOptionsProps IgnoreTableOptions { get; set; }

        public DeploymentOptionsProps IgnoreSemicolonBetweenStatements { get; set; }

        public DeploymentOptionsProps IgnoreRouteLifetime { get; set; }

        public DeploymentOptionsProps IgnoreRoleMembership { get; set; }

        public DeploymentOptionsProps IgnoreQuotedIdentifiers { get; set; }

        public DeploymentOptionsProps IgnorePermissions { get; set; }

        public DeploymentOptionsProps IgnorePartitionSchemes { get; set; }

        public DeploymentOptionsProps IgnoreObjectPlacementOnPartitionScheme { get; set; }

        public DeploymentOptionsProps IgnoreNotForReplication { get; set; }

        public DeploymentOptionsProps IgnoreLoginSids { get; set; }

        public DeploymentOptionsProps IgnoreLockHintsOnIndexes { get; set; }

        public DeploymentOptionsProps IgnoreKeywordCasing { get; set; }

        public DeploymentOptionsProps IgnoreIndexPadding { get; set; }

        public DeploymentOptionsProps IgnoreIndexOptions { get; set; }

        public DeploymentOptionsProps IgnoreIncrement { get; set; }

        public DeploymentOptionsProps IgnoreIdentitySeed { get; set; }

        public DeploymentOptionsProps IgnoreUserSettingsObjects { get; set; }

        public DeploymentOptionsProps IgnoreFullTextCatalogFilePath { get; set; }

        public DeploymentOptionsProps IgnoreWhitespace { get; set; }

        public DeploymentOptionsProps IgnoreWithNocheckOnForeignKeys { get; set; }

        public DeploymentOptionsProps VerifyCollationCompatibility { get; set; }

        public DeploymentOptionsProps UnmodifiableObjectWarnings { get; set; }

        public DeploymentOptionsProps TreatVerificationErrorsAsWarnings { get; set; }

        public DeploymentOptionsProps ScriptRefreshModule { get; set; }

        public DeploymentOptionsProps ScriptNewConstraintValidation { get; set; }

        public DeploymentOptionsProps ScriptFileSize { get; set; }

        public DeploymentOptionsProps ScriptDeployStateChecks { get; set; }

        public DeploymentOptionsProps ScriptDatabaseOptions { get; set; }

        public DeploymentOptionsProps ScriptDatabaseCompatibility { get; set; }

        public DeploymentOptionsProps ScriptDatabaseCollation { get; set; }

        public DeploymentOptionsProps RunDeploymentPlanExecutors { get; set; }

        public DeploymentOptionsProps RegisterDataTierApplication { get; set; }

        public DeploymentOptionsProps PopulateFilesOnFileGroups { get; set; }

        public DeploymentOptionsProps NoAlterStatementsToChangeClrTypes { get; set; }

        public DeploymentOptionsProps IncludeTransactionalScripts { get; set; }

        public DeploymentOptionsProps IncludeCompositeObjects { get; set; }

        public DeploymentOptionsProps AllowUnsafeRowLevelSecurityDataMovement { get; set; }

        public DeploymentOptionsProps IgnoreWithNocheckOnCheckConstraints { get; set; }

        public DeploymentOptionsProps IgnoreFillFactor { get; set; }

        public DeploymentOptionsProps IgnoreFileSize { get; set; }

        public DeploymentOptionsProps IgnoreFilegroupPlacement { get; set; }

        public DeploymentOptionsProps DoNotAlterReplicatedObjects { get; set; }

        public DeploymentOptionsProps DoNotAlterChangeDataCaptureObjects { get; set; }

        public DeploymentOptionsProps DisableAndReenableDdlTriggers { get; set; }

        public DeploymentOptionsProps DeployDatabaseInSingleUserMode { get; set; }

        public DeploymentOptionsProps CreateNewDatabase { get; set; }

        public DeploymentOptionsProps CompareUsingTargetCollation { get; set; }

        public DeploymentOptionsProps CommentOutSetVarDeclarations { get; set; }

        public DeploymentOptionsProps CommandTimeout { get; set; } = new DeploymentOptionsProps{ value = 120, description = string.Empty };

        public DeploymentOptionsProps LongRunningCommandTimeout { get; set; } = new DeploymentOptionsProps { value = 0, description = string.Empty };

        public DeploymentOptionsProps DatabaseLockTimeout { get; set; } = new DeploymentOptionsProps { value = 60, description = string.Empty };

        public DeploymentOptionsProps BlockWhenDriftDetected { get; set; }

        public DeploymentOptionsProps BlockOnPossibleDataLoss { get; set; }

        public DeploymentOptionsProps BackupDatabaseBeforeChanges { get; set; }

        public DeploymentOptionsProps AllowIncompatiblePlatform { get; set; }

        public DeploymentOptionsProps AllowDropBlockingAssemblies { get; set; }

        public DeploymentOptionsProps AdditionalDeploymentContributorArguments { get; set; }

        public DeploymentOptionsProps AdditionalDeploymentContributors { get; set; }

        public DeploymentOptionsProps DropConstraintsNotInSource { get; set; }

        public DeploymentOptionsProps DropDmlTriggersNotInSource { get; set; }

        public DeploymentOptionsProps DropExtendedPropertiesNotInSource { get; set; }

        public DeploymentOptionsProps DropIndexesNotInSource { get; set; }

        public DeploymentOptionsProps IgnoreFileAndLogFilePath { get; set; }

        public DeploymentOptionsProps IgnoreExtendedProperties { get; set; }

        public DeploymentOptionsProps IgnoreDmlTriggerState { get; set; }

        public DeploymentOptionsProps IgnoreDmlTriggerOrder { get; set; }

        public DeploymentOptionsProps IgnoreDefaultSchema { get; set; }

        public DeploymentOptionsProps IgnoreDdlTriggerState { get; set; }

        public DeploymentOptionsProps IgnoreDdlTriggerOrder { get; set; }

        public DeploymentOptionsProps IgnoreCryptographicProviderFilePath { get; set; }

        public DeploymentOptionsProps VerifyDeployment { get; set; }

        public DeploymentOptionsProps IgnoreComments { get; set; }

        public DeploymentOptionsProps IgnoreColumnCollation { get; set; }

        public DeploymentOptionsProps IgnoreAuthorizer { get; set; }

        public DeploymentOptionsProps IgnoreAnsiNulls { get; set; }

        public DeploymentOptionsProps GenerateSmartDefaults { get; set; }

        public DeploymentOptionsProps DropStatisticsNotInSource { get; set; }

        public DeploymentOptionsProps DropRoleMembersNotInSource { get; set; }

        public DeploymentOptionsProps DropPermissionsNotInSource { get; set; }

        public DeploymentOptionsProps DropObjectsNotInSource { get; set; }

        public DeploymentOptionsProps IgnoreColumnOrder { get; set; }

        public DeploymentOptionsProps IgnoreTablePartitionOptions { get; set; } // DW Specific

        public DeploymentOptionsProps AdditionalDeploymentContributorPaths { get; set; } = new DeploymentOptionsProps { value = string.Empty, description = string.Empty };

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

        public DeploymentOptionsProps AllowExternalLibraryPaths { get; set; }

        public DeploymentOptionsProps AllowExternalLanguagePaths { get; set; }

        public DeploymentOptionsProps DoNotEvaluateSqlCmdVariables { get; set; }

        public DeploymentOptionsProps DisableParallelismForEnablingIndexes { get; set; }

        public DeploymentOptionsProps DoNotDropWorkloadClassifiers { get; set; }

        public DeploymentOptionsProps DisableIndexesForDataPhase { get; set; }

        public DeploymentOptionsProps DoNotDropDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionsProps HashObjectNamesInLogs { get; set; }

        public DeploymentOptionsProps IgnoreWorkloadClassifiers { get; set; }

        public DeploymentOptionsProps IgnoreDatabaseWorkloadGroups { get; set; }

        public DeploymentOptionsProps IsAlwaysEncryptedParameterizationEnabled { get; set; }

        public DeploymentOptionsProps PreserveIdentityLastValues { get; set; }

        public DeploymentOptionsProps RestoreSequenceCurrentValue { get; set; }

        public DeploymentOptionsProps RebuildIndexesOfflineForDataPhase { get; set; }
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
                        DeploymentOptionsProps setProp = new DeploymentOptionsProps()
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
                        DeploymentOptionsProps setProp = new DeploymentOptionsProps()
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
