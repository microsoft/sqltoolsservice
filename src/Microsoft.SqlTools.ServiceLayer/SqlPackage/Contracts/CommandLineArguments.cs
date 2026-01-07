//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.Tools.Schema.CommandLineTool;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts
{
    /// <summary>
    /// Command-line arguments for SqlPackage operations containing source/target paths, connection strings, etc.
    /// These arguments are copied from the DacFx/Source/SqlPackage/CommandLineArguments.cs
    /// </summary>
    public class SqlPackageCommandLineArguments
    {
        /// <summary>
        /// Action to perform: Publish, Extract, Script, Export, or Import
        /// </summary>
        public CommandLineToolAction Action { get; set; }

        /// <summary>
        /// Suppress all output except errors
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Enable diagnostics logging
        /// </summary>
        public bool Diagnostics { get; set; }

        /// <summary>
        /// Maximum number of parallel operations (default: 8)
        /// </summary>
        public int MaxParallelism { get; set; }

        /// <summary>
        /// Path to diagnostics log file
        /// </summary>
        public string DiagnosticsFile { get; set; }

        /// <summary>
        /// Path to diagnostics package file
        /// </summary>
        public string DiagnosticsPackageFile { get; set; }

        /// <summary>
        /// Diagnostics logging level
        /// </summary>
        public CommandLineDiagnosticsLevel DiagnosticsLevel { get; set; }

        /// <summary>
        /// Overwrite existing files without prompting
        /// </summary>
        public bool OverwriteFiles { get; set; }

        // Source connection parameters

        /// <summary>
        /// Source server name
        /// </summary>
        public string SourceServerName { get; set; }

        /// <summary>
        /// Source database name
        /// </summary>
        public string SourceDatabaseName { get; set; }

        /// <summary>
        /// Source user name for SQL authentication
        /// </summary>
        public string SourceUser { get; set; }

        /// <summary>
        /// Source password for SQL authentication
        /// </summary>
        public string SourcePassword { get; set; }

        /// <summary>
        /// Source connection timeout in seconds
        /// </summary>
        public int SourceTimeout { get; set; }

        /// <summary>
        /// Source connection encryption option
        /// </summary>
        public EncryptOption SourceEncryptConnection { get; set; }

        /// <summary>
        /// Source host name in certificate for TLS/SSL
        /// </summary>
        public string SourceHostNameInCertificate { get; set; }

        /// <summary>
        /// Trust source server certificate without validation
        /// </summary>
        public bool SourceTrustServerCertificate { get; set; }

        /// <summary>
        /// Source connection string
        /// </summary>
        public string SourceConnectionString { get; set; }

        /// <summary>
        /// Source file path (for Publish, Script, Import operations)
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Model file path for TSqlModel-based operations
        /// </summary>
        public string ModelFilePath { get; set; }

        // Target connection parameters

        /// <summary>
        /// Target server name
        /// </summary>
        public string TargetServerName { get; set; }

        /// <summary>
        /// Target database name
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Target user name for SQL authentication
        /// </summary>
        public string TargetUser { get; set; }

        /// <summary>
        /// Target password for SQL authentication
        /// </summary>
        public string TargetPassword { get; set; }

        /// <summary>
        /// Target connection timeout in seconds
        /// </summary>
        public int TargetTimeout { get; set; }

        /// <summary>
        /// Target connection encryption option
        /// </summary>
        public EncryptOption TargetEncryptConnection { get; set; }

        /// <summary>
        /// Target host name in certificate for TLS/SSL
        /// </summary>
        public string TargetHostNameInCertificate { get; set; }

        /// <summary>
        /// Trust target server certificate without validation
        /// </summary>
        public bool TargetTrustServerCertificate { get; set; }

        /// <summary>
        /// Target connection string
        /// </summary>
        public string TargetConnectionString { get; set; }

        /// <summary>
        /// Target file path (for Extract, Export operations)
        /// </summary>
        public string TargetFile { get; set; }

        // Additional operation parameters

        /// <summary>
        /// Additional deployment properties as /p:PropertyName=Value
        /// </summary>
        public string[] Properties { get; set; }

        /// <summary>
        /// SQLCMD variables as /v:VariableName=Value
        /// </summary>
        public string[] Variables { get; set; }

        /// <summary>
        /// Profile file path (for loading saved deployment profiles)
        /// </summary>
        public string Profile { get; set; }

        /// <summary>
        /// Output path for Script operation
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Path to save the deployment script
        /// </summary>
        public string DeployScriptPath { get; set; }

        /// <summary>
        /// Path to save the deployment report
        /// </summary>
        public string DeployReportPath { get; set; }

        /// <summary>
        /// Reference paths for resolving external dependencies
        /// </summary>
        public string[] ReferencePaths { get; set; }

        // Azure and authentication parameters

        /// <summary>
        /// Azure Key Vault authentication method
        /// </summary>
        public Microsoft.SqlServer.Dac.KeyVault.KeyVaultAuthType AzureKeyVaultAuthMethod { get; set; }

        /// <summary>
        /// Thread maximum stack size
        /// </summary>
        public int ThreadMaxStackSize { get; set; }

        /// <summary>
        /// Access token for Azure AD authentication
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Use universal authentication (Azure AD)
        /// </summary>
        public bool UniversalAuthentication { get; set; }

        /// <summary>
        /// Azure AD tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Azure cloud configuration (e.g., AzurePublic, AzureChina)
        /// </summary>
        public string AzureCloudConfig { get; set; }

        /// <summary>
        /// Client ID for service principal authentication
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret for service principal authentication
        /// </summary>
        public string Secret { get; set; }
    }
}
