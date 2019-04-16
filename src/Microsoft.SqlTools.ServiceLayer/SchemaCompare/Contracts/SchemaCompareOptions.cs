//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Class to define schema compare and publish options. 
    /// Keeping the order and defaults same as DacFx
    /// The default values here should also match the default values in ADS UX
    /// </summary>
    public class SchemaCompareOptions
    {
        public bool IgnoreTableOptions { get; set; }

        public bool IgnoreSemicolonBetweenStatements { get; set; } = true;

        public bool IgnoreRouteLifetime { get; set; } = true;

        public bool IgnoreRoleMembership { get; set; }

        public bool IgnoreQuotedIdentifiers { get; set; } = true;

        public bool IgnorePermissions { get; set; }

        public bool IgnorePartitionSchemes { get; set; }

        public bool IgnoreObjectPlacementOnPartitionScheme { get; set; } = true;

        public bool IgnoreNotForReplication { get; set; }

        public bool IgnoreLoginSids { get; set; } = true;

        public bool IgnoreLockHintsOnIndexes { get; set; }

        public bool IgnoreKeywordCasing { get; set; } = true;
        
        public bool IgnoreIndexPadding { get; set; } = true;

        public bool IgnoreIndexOptions { get; set; }

        public bool IgnoreIncrement { get; set; }

        public bool IgnoreIdentitySeed { get; set; }

        public bool IgnoreUserSettingsObjects { get; set; }
        
        public bool IgnoreFullTextCatalogFilePath { get; set; } = true;
        
        public bool IgnoreWhitespace { get; set; } = true;

        public bool IgnoreWithNocheckOnForeignKeys { get; set; }
        
        public bool VerifyCollationCompatibility { get; set; } = true;
        
        public bool UnmodifiableObjectWarnings { get; set; } = true;

        public bool TreatVerificationErrorsAsWarnings { get; set; }
        
        public bool ScriptRefreshModule { get; set; } = true;
        
        public bool ScriptNewConstraintValidation { get; set; } = true;

        public bool ScriptFileSize { get; set; }

        public bool ScriptDeployStateChecks { get; set; }

        public bool ScriptDatabaseOptions { get; set; }

        public bool ScriptDatabaseCompatibility { get; set; }

        public bool ScriptDatabaseCollation { get; set; }

        public bool RunDeploymentPlanExecutors { get; set; }

        public bool RegisterDataTierApplication { get; set; }
        
        public bool PopulateFilesOnFileGroups { get; set; } = true;

        public bool NoAlterStatementsToChangeClrTypes { get; set; }

        public bool IncludeTransactionalScripts { get; set; }

        public bool IncludeCompositeObjects { get; set; }

        public bool AllowUnsafeRowLevelSecurityDataMovement { get; set; }

        public bool IgnoreWithNocheckOnCheckConstraints { get; set; }
        
        public bool IgnoreFillFactor { get; set; } = true;
        
        public bool IgnoreFileSize { get; set; } = true;
        
        public bool IgnoreFilegroupPlacement { get; set; } = true;
        
        public bool DoNotAlterReplicatedObjects { get; set; } = true;
        
        public bool DoNotAlterChangeDataCaptureObjects { get; set; } = true;
        
        public bool DisableAndReenableDdlTriggers { get; set; } = true;

        public bool DeployDatabaseInSingleUserMode { get; set; }

        public bool CreateNewDatabase { get; set; }

        public bool CompareUsingTargetCollation { get; set; }

        public bool CommentOutSetVarDeclarations { get; set; }
        
        public int CommandTimeout { get; set; } = 120;

        public bool BlockWhenDriftDetected { get; set; }
        
        public bool BlockOnPossibleDataLoss { get; set; } = true;

        public bool BackupDatabaseBeforeChanges { get; set; }

        public bool AllowIncompatiblePlatform { get; set; }

        public bool AllowDropBlockingAssemblies { get; set; }

        public string AdditionalDeploymentContributorArguments { get; set; }

        public string AdditionalDeploymentContributors { get; set; }
        
        public bool DropConstraintsNotInSource { get; set; } = true;
        
        public bool DropDmlTriggersNotInSource { get; set; } = true;
        
        public bool DropExtendedPropertiesNotInSource { get; set; } = true;
        
        public bool DropIndexesNotInSource { get; set; } = true;
        
        public bool IgnoreFileAndLogFilePath { get; set; } = true;

        public bool IgnoreExtendedProperties { get; set; }

        public bool IgnoreDmlTriggerState { get; set; }

        public bool IgnoreDmlTriggerOrder { get; set; }

        public bool IgnoreDefaultSchema { get; set; }

        public bool IgnoreDdlTriggerState { get; set; }

        public bool IgnoreDdlTriggerOrder { get; set; }
        
        public bool IgnoreCryptographicProviderFilePath { get; set; } = true;
        
        public bool VerifyDeployment { get; set; } = true;

        public bool IgnoreComments { get; set; }

        public bool IgnoreColumnCollation { get; set; }

        public bool IgnoreAuthorizer { get; set; }
        
        public bool IgnoreAnsiNulls { get; set; } = true;

        public bool GenerateSmartDefaults { get; set; }
        
        public bool DropStatisticsNotInSource { get; set; } = true;

        public bool DropRoleMembersNotInSource { get; set; }

        public bool DropPermissionsNotInSource { get; set; }
        
        public bool DropObjectsNotInSource { get; set; } = true;

        public bool IgnoreColumnOrder { get; set; }

        public ObjectType[] DoNotDropObjectTypes { get; set; } = null;

        public ObjectType[] ExcludeObjectTypes { get; set; } =
        {
                ObjectType.ServerTriggers,
                ObjectType.Routes,
                ObjectType.LinkedServerLogins,
                ObjectType.Endpoints,
                ObjectType.ErrorMessages,
                ObjectType.Filegroups,
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
    }
}
