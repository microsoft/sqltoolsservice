//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress upgrade plan operation
    /// </summary>
    class UpgradePlanOperation : DacFxOperation
    {
        public UpgradePlanParams Parameters { get; }

        public UpgradePlanOperation(UpgradePlanParams parameters, ConnectionInfo connInfo): base(connInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
        }

        public string ExecuteGenerateDeployReport()
        {
            DacPackage dacpac = DacPackage.Load(this.Parameters.PackageFilePath);
            DacServices ds = new DacServices(this.ConnectionString);
            DacDeployOptions options = GetDefaultDeployOptions();
            options.BlockOnPossibleDataLoss = false;
            options.BlockWhenDriftDetected = false;
            string report = ds.GenerateDeployReport(dacpac, this.Parameters.DatabaseName, options, this.CancellationToken);
            return report;
        }

        private static DacDeployOptions GetDefaultDeployOptions()
        {
            DacDeployOptions options = new DacDeployOptions
            {
                AllowDropBlockingAssemblies = true,
                AllowIncompatiblePlatform = true,
                BackupDatabaseBeforeChanges = false,
                BlockOnPossibleDataLoss = true,
                BlockWhenDriftDetected = true,
                CompareUsingTargetCollation = false,
                CommentOutSetVarDeclarations = false,
                CreateNewDatabase = false,
                DeployDatabaseInSingleUserMode = false,
                DisableAndReenableDdlTriggers = true,
                DoNotAlterReplicatedObjects = true,
                DoNotAlterChangeDataCaptureObjects = true,
                DropConstraintsNotInSource = true,
                DropDmlTriggersNotInSource = true,
                DropExtendedPropertiesNotInSource = true,
                DropIndexesNotInSource = true,
                DropObjectsNotInSource = true,
                DropPermissionsNotInSource = true,
                DropRoleMembersNotInSource = true,
                GenerateSmartDefaults = false,
                IgnoreAnsiNulls = false,
                IgnoreAuthorizer = false,
                IgnoreColumnCollation = false,
                IgnoreComments = false,
                IgnoreCryptographicProviderFilePath = true,
                IgnoreDdlTriggerOrder = false,
                IgnoreDdlTriggerState = false,
                IgnoreDefaultSchema = false,
                IgnoreDmlTriggerOrder = false,
                IgnoreDmlTriggerState = false,
                IgnoreExtendedProperties = false,
                IgnoreFileAndLogFilePath = true,
                IgnoreFilegroupPlacement = true,
                IgnoreFileSize = true,
                IgnoreFillFactor = true,
                IgnoreFullTextCatalogFilePath = true,
                IgnoreIdentitySeed = false,
                IgnoreIncrement = false,
                IgnoreIndexOptions = false,
                IgnoreIndexPadding = true,
                IgnoreKeywordCasing = false,
                IgnoreLockHintsOnIndexes = false,
                IgnoreLoginSids = true,
                IgnoreNotForReplication = false,
                IgnoreObjectPlacementOnPartitionScheme = true,
                IgnorePartitionSchemes = false,
                IgnorePermissions = false,
                IgnoreQuotedIdentifiers = false,
                IgnoreRoleMembership = false,
                IgnoreRouteLifetime = true,
                IgnoreSemicolonBetweenStatements = false,
                IgnoreTableOptions = false,
                IgnoreUserSettingsObjects = false,
                IgnoreWhitespace = false,
                IgnoreWithNocheckOnCheckConstraints = false,
                IgnoreWithNocheckOnForeignKeys = false,
                IncludeCompositeObjects = false,
                IncludeTransactionalScripts = false,
                NoAlterStatementsToChangeClrTypes = false,
                PopulateFilesOnFileGroups = true,
                RegisterDataTierApplication = false,
                ScriptDatabaseCollation = false,
                ScriptDatabaseCompatibility = false,
                ScriptDatabaseOptions = true,
                ScriptDeployStateChecks = false,
                ScriptFileSize = false,
                ScriptNewConstraintValidation = true,
                ScriptRefreshModule = true,
                TreatVerificationErrorsAsWarnings = false,
                UnmodifiableObjectWarnings = true,
                VerifyDeployment = true,
                VerifyCollationCompatibility = true
            };

            return options;
        }
    }
}
