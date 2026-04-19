//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare publish database changes operation.
    ///
    /// Splits the publish into two explicit steps so that DacServices events are accessible:
    ///   1. GenerateScript() — uses SchemaComparisonResult to produce T-SQL (no DB connection)
    ///   2. Execute the script via SqlConnection with GO-batch splitting, forwarding
    ///      step messages to ProgressHandler for display in the host UI.
    /// </summary>
    public class SchemaComparePublishDatabaseChangesOperation : SchemaComparePublishChangesOperation
    {
        private readonly SchemaCompareEndpointInfo _targetEndpointInfo;
        private readonly ISchemaCompareConnectionProvider _connectionProvider;

        public SchemaComparePublishDatabaseChangesParams Parameters { get; }

        public bool PublishSuccess { get; private set; }

        /// <summary>
        /// Optional progress handler. Assign before calling Execute() to receive
        /// step-level messages during publish.
        /// VSCode/ADS wires this to SqlTask.AddMessage(); SSMS wires it to its own UI.
        /// </summary>
        public ISchemaCompareProgressHandler ProgressHandler { get; set; }

        /// <summary>
        /// Creates a new publish-to-database operation.
        /// </summary>
        /// <param name="parameters">Publish parameters (OperationId, TargetDatabaseName, etc.)</param>
        /// <param name="comparisonResult">The result of the preceding schema comparison.</param>
        /// <param name="targetEndpointInfo">
        /// The target database endpoint — used to build the connection string via connectionProvider.
        /// This is the TargetEndpointInfo from the original SchemaCompareParams.
        /// </param>
        /// <param name="connectionProvider">
        /// Connection provider for resolving connection strings and access tokens.
        /// The same provider used by SchemaCompareOperation.
        /// </param>
        public SchemaComparePublishDatabaseChangesOperation(
            SchemaComparePublishDatabaseChangesParams parameters,
            SchemaComparisonResult comparisonResult,
            SchemaCompareEndpointInfo targetEndpointInfo,
            ISchemaCompareConnectionProvider connectionProvider)
            : base(comparisonResult)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Validate.IsNotNull(nameof(targetEndpointInfo), targetEndpointInfo);
            Validate.IsNotNull(nameof(connectionProvider), connectionProvider);

            Parameters = parameters;
            _targetEndpointInfo = targetEndpointInfo;
            _connectionProvider = connectionProvider;
            OperationId = !string.IsNullOrEmpty(parameters.OperationId)
                ? parameters.OperationId
                : Guid.NewGuid().ToString();
        }

        public override void Execute()
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                // ── Step 1: Generate the deployment script via Schema Compare API ──────────
                // SchemaComparisonResult.GenerateScript() does the diff work and produces
                // T-SQL — no database connection is required at this stage.
                Logger.Verbose($"Schema compare publish operation {OperationId}: generating script for '{Parameters.TargetDatabaseName}'.");
                ProgressHandler?.OnProgress($"Generating deployment script for '{Parameters.TargetDatabaseName}'...");

                SchemaCompareScriptGenerationResult scriptResult = ComparisonResult.GenerateScript(
                    Parameters.TargetDatabaseName, CancellationToken);

                if (!scriptResult.Success)
                {
                    ErrorMessage = scriptResult.Message;
                    throw new Exception(ErrorMessage);
                }

                CancellationToken.ThrowIfCancellationRequested();

                // ── Step 2: Execute the generated script via SqlConnection ─────────────────
                // We own the connection — so we control execution and can emit progress.
                // The script is split on GO batch separators (standard SQLCMD behaviour).
                string connectionString = _connectionProvider.GetConnectionString(_targetEndpointInfo);
                string accessToken = _connectionProvider.GetAccessToken(_targetEndpointInfo);

                Logger.Verbose($"Schema compare publish operation {OperationId}: executing script against '{Parameters.TargetDatabaseName}'.");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        connection.AccessToken = accessToken;
                    }

                    connection.Open();

                    List<string> batches = SplitIntoBatches(scriptResult.Script);
                    int batchIndex = 0;
                    int totalBatches = batches.Count;

                    ProgressHandler?.OnProgress($"Applying {totalBatches} batch(es) to '{Parameters.TargetDatabaseName}'...");

                    foreach (string batch in batches)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        if (string.IsNullOrWhiteSpace(batch))
                        {
                            continue;
                        }

                        batchIndex++;
                        ProgressHandler?.OnProgress($"Executing batch {batchIndex} of {totalBatches}...");
                        Logger.Verbose($"Schema compare publish operation {OperationId}: batch {batchIndex}/{totalBatches}.");

                        using (SqlCommand command = new SqlCommand(batch, connection))
                        {
                            command.CommandTimeout = 0; // no timeout — schema changes can be slow
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // Also execute the master script if present (Azure SQL DB scenario)
                if (!string.IsNullOrEmpty(scriptResult.MasterScript))
                {
                    ProgressHandler?.OnProgress("Applying master database script...");
                    Logger.Verbose($"Schema compare publish operation {OperationId}: applying master script.");

                    // For master scripts, connect without a database name
                    SqlConnectionStringBuilder masterBuilder = new SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = "master"
                    };

                    using (SqlConnection masterConnection = new SqlConnection(masterBuilder.ConnectionString))
                    {
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            masterConnection.AccessToken = accessToken;
                        }

                        masterConnection.Open();

                        foreach (string batch in SplitIntoBatches(scriptResult.MasterScript))
                        {
                            CancellationToken.ThrowIfCancellationRequested();

                            if (string.IsNullOrWhiteSpace(batch))
                            {
                                continue;
                            }

                            using (SqlCommand cmd = new SqlCommand(batch, masterConnection))
                            {
                                cmd.CommandTimeout = 0;
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                PublishSuccess = true;
                ProgressHandler?.OnProgress("Publish completed successfully.");
                Logger.Verbose($"Schema compare publish operation {OperationId}: completed successfully.");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operation was cancelled.";
                Logger.Warning($"Schema compare publish operation {OperationId}: cancelled.");
                throw;
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                PublishSuccess = false;
                ProgressHandler?.OnProgress($"Publish failed: {e.Message}", isError: true);
                Logger.Error($"Schema compare publish database changes operation {OperationId} failed with exception {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Splits a T-SQL script into executable batches using GO as the batch separator.
        /// Handles GO on its own line, optionally followed by a count (e.g. GO 2).
        /// </summary>
        private static List<string> SplitIntoBatches(string script)
        {
            List<string> batches = new List<string>();
            if (string.IsNullOrEmpty(script))
            {
                return batches;
            }

            // Match GO on its own line (case-insensitive), optionally with a repeat count
            string[] parts = Regex.Split(script, @"^\s*GO\s*(\d+)?\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (string part in parts)
            {
                // Skip the captured group from the GO repeat count (if any)
                if (Regex.IsMatch(part.Trim(), @"^\d+$"))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(part))
                {
                    batches.Add(part.Trim());
                }
            }

            return batches;
        }
    }
}
