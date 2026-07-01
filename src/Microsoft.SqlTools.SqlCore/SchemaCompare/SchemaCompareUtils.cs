//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{

    /// <summary>
    /// Internal class for utilities shared between multiple schema compare operations
    /// </summary>
    internal static class SchemaCompareUtils
    {
        private static readonly Regex ExcessWhitespaceRegex = new Regex(" {2,}", RegexOptions.Compiled);

        // The DacFx full type-name suffixes for the constraint kinds that, on SqlDwUnified
        // (Fabric Warehouse), are always emitted as standalone "ALTER TABLE ... ADD CONSTRAINT"
        // statements rather than inlined into CREATE TABLE. We need to preserve their child
        // scripts so the diff editor and aggregated generated script include the constraint
        // definitions. Match SchemaComparisonExcludedObjectId.TypeName which is the
        // full .NET type name like "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlPrimaryKeyConstraint".
        private static readonly string[] ConstraintTypeNameSuffixes = new[]
        {
            "PrimaryKeyConstraint",
            "ForeignKeyConstraint",
            "UniqueConstraint",
            "CheckConstraint",
            "DefaultConstraint",
        };

        private const string SqlDwUnifiedPlatformName = "SqlDwUnified";

        internal static DiffEntry CreateDiffEntry(SchemaDifference difference, DiffEntry parent, SchemaComparisonResult schemaComparisonResult)
        {
            if (difference == null)
            {
                return null;
            }

            var diffEntry = new DiffEntry();
            diffEntry.UpdateAction = difference.UpdateAction;
            diffEntry.DifferenceType = difference.DifferenceType;
            diffEntry.Name = difference.Name;
            diffEntry.Included = difference.Included;

            if (difference.SourceObject != null)
            {
                diffEntry.SourceValue = difference.SourceObject.Name.Parts.ToArray();
                var sourceType = new SchemaComparisonExcludedObjectId(difference.SourceObject.ObjectType, difference.SourceObject.Name);
                diffEntry.SourceObjectType = sourceType.TypeName;

            }
            if (difference.TargetObject != null)
            {
                diffEntry.TargetValue = difference.TargetObject.Name.Parts.ToArray();
                var targetType = new SchemaComparisonExcludedObjectId(difference.TargetObject.ObjectType, difference.TargetObject.Name);
                diffEntry.TargetObjectType = targetType.TypeName;
            }

            if (difference.DifferenceType == SchemaDifferenceType.Object)
            {
                // Fabric Warehouse (SqlDwUnified) never inlines PK/FK/UNIQUE/CHECK/DEFAULT
                // constraints into CREATE TABLE — every constraint is emitted as a standalone
                // "ALTER TABLE ... ADD CONSTRAINT ..." script. The legacy filter below skips
                // child scripts that start with "alter" on the assumption that they're
                // duplicates of inline constraints already present in the parent's CREATE.
                // For SqlDwUnified that assumption is wrong: the ALTER script is the ONLY
                // place the constraint is defined, so we must keep it.
                bool keepAlterScript = IsConstraintChildOnSqlDwUnified(diffEntry, schemaComparisonResult);

                // set source and target scripts
                if (difference.SourceObject != null)
                {
                    string sourceScript = schemaComparisonResult.GetDiffEntrySourceScript(difference);

                    // Child scripts that do not use alter need to be added if they are being changed, ex: "EXECUTE sp_addextendedproperty...".
                    // Don't add scripts that start with alter because those are handled by a top level element's create
                    if (keepAlterScript || !sourceScript.ToLowerInvariant().StartsWith("alter"))
                    {
                        diffEntry.SourceScript = FormatScript(sourceScript);
                    }
                }
                if (difference.TargetObject != null)
                {
                    string targetScript = schemaComparisonResult.GetDiffEntryTargetScript(difference);

                    // Child scripts that do not use alter need to be added if they are being changed, ex: "EXECUTE sp_addextendedproperty...".
                    // Don't add scripts that start with alter because those are handled by a top level element's create
                    if (keepAlterScript || !targetScript.ToLowerInvariant().StartsWith("alter"))
                    {
                        diffEntry.TargetScript = FormatScript(targetScript);
                    }
                }
            }

            diffEntry.Children = new List<DiffEntry>();

            foreach (SchemaDifference child in difference.Children)
            {
                diffEntry.Children.Add(CreateDiffEntry(child, diffEntry, schemaComparisonResult));
            }

            return diffEntry;
        }

        private static bool IsConstraintChildOnSqlDwUnified(DiffEntry diffEntry, SchemaComparisonResult schemaComparisonResult)
        {
            string typeName = diffEntry.SourceObjectType ?? diffEntry.TargetObjectType;
            string platform = GetComparisonPlatform(schemaComparisonResult);
            return ShouldPreserveAlterScriptForConstraint(typeName, platform);
        }

        /// <summary>
        /// Pure helper: decides whether a diff-entry's "starts-with-alter" script must be
        /// preserved instead of stripped. Returns true when the given DacFx object type name
        /// is one of the constraint kinds we fold under a parent table
        /// (<see cref="ConstraintTypeNameSuffixes"/>) and the comparison is running under the
        /// Fabric Warehouse DSP (<see cref="SqlDwUnifiedPlatformName"/>). All other inputs
        /// — non-constraint types, non-Fabric platforms, null/empty type or platform —
        /// return false, preserving the legacy strip-on-alter behaviour for SQL Server,
        /// Azure SQL, and Synapse comparisons.
        /// </summary>
        /// <remarks>
        /// Extracted from <see cref="IsConstraintChildOnSqlDwUnified"/> so it can be unit
        /// tested without manufacturing a real <see cref="SchemaComparisonResult"/> graph
        /// (DacFx's comparison types are sealed with no public constructor that yields a
        /// usable platform projection).
        /// </remarks>
        internal static bool ShouldPreserveAlterScriptForConstraint(string objectTypeName, string platform)
        {
            if (string.IsNullOrEmpty(objectTypeName))
            {
                return false;
            }

            bool isConstraint = false;
            foreach (string suffix in ConstraintTypeNameSuffixes)
            {
                if (objectTypeName.EndsWith(suffix, StringComparison.Ordinal))
                {
                    isConstraint = true;
                    break;
                }
            }
            if (!isConstraint)
            {
                return false;
            }

            return string.Equals(platform, SqlDwUnifiedPlatformName, StringComparison.Ordinal);
        }


        internal static SchemaComparisonExcludedObjectId CreateExcludedObject(SchemaCompareObjectId sourceObj)
        {
            try
            {
                if (sourceObj == null || sourceObj.NameParts == null || string.IsNullOrEmpty(sourceObj.SqlObjectType))
                {
                    return null;
                }
                var id = new ObjectIdentifier(sourceObj.NameParts);
                var excludedObjId = new SchemaComparisonExcludedObjectId(sourceObj.SqlObjectType, id);
                return excludedObjId;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        internal static SchemaCompareEndpoint CreateSchemaCompareEndpoint(SchemaCompareEndpointInfo endpointInfo, ISchemaCompareConnectionProvider connectionProvider)
        {
            switch (endpointInfo.EndpointType)
            {
                case SchemaCompareEndpointType.Project:
                    {
                        return endpointInfo?.ExtractTarget != null
                            ? new SchemaCompareProjectEndpoint(endpointInfo.ProjectFilePath, endpointInfo.TargetScripts, endpointInfo.DataSchemaProvider, (DacExtractTarget)endpointInfo?.ExtractTarget)
                            : new SchemaCompareProjectEndpoint(endpointInfo.ProjectFilePath, endpointInfo.TargetScripts, endpointInfo.DataSchemaProvider);
                    }
                case SchemaCompareEndpointType.Dacpac:
                    {
                        return new SchemaCompareDacpacEndpoint(endpointInfo.PackageFilePath);
                    }
                case SchemaCompareEndpointType.Database:
                    {
                        string connectionString = connectionProvider.GetConnectionString(endpointInfo);
                        IUniversalAuthProvider authProvider = connectionProvider.GetAuthProvider(endpointInfo);

                        return authProvider != null
                            ? new SchemaCompareDatabaseEndpoint(connectionString, authProvider)
                            : new SchemaCompareDatabaseEndpoint(connectionString);
                    }
                default:
                    {
                        throw new NotSupportedException(string.Format("Endpoint Type {0} is not supported", endpointInfo.EndpointType));
                    }
            }
        }

        internal static string RemoveExcessWhitespace(string script)
        {
            if (script != null)
            {
                // remove leading and trailing whitespace
                script = script.Trim();
                // replace all multiple spaces with single space
                script = ExcessWhitespaceRegex.Replace(script, " ");
            }
            return script;
        }

        internal static string FormatScript(string script)
        {
            script = RemoveExcessWhitespace(script);
            if (!string.IsNullOrWhiteSpace(script) && !script.Equals("null"))
            {
                script += Environment.NewLine + "GO";
            }
            return script;
        }

        // Cached reflection accessors for SchemaComparisonResult.DataModel.DatabaseSchemaProvider.Platform.
        // Resolved lazily and atomically on first use so we pay the lookup cost once per process.
        private static PropertyInfo s_dataModelProp;
        private static PropertyInfo s_dspProp;
        private static PropertyInfo s_platformProp;
        private static bool s_reflectionInitFailed;
        private static readonly object s_reflectionInitLock = new object();

        // Per-result cache so CreateDiffEntry's recursive walk does not re-reflect for every child.
        // ConditionalWeakTable doesn't allow null values, so we sentinel "unknown" as empty string.
        private static readonly ConditionalWeakTable<SchemaComparisonResult, string> s_platformByResult =
            new ConditionalWeakTable<SchemaComparisonResult, string>();

        /// <summary>
        /// Returns the comparison's DSP platform as a string (e.g. "Sql160", "SqlDwUnified")
        /// by reflecting into the internal <c>SchemaCompareDataModel.DatabaseSchemaProvider</c>
        /// reachable from <see cref="SchemaComparisonResult"/>. Returns <c>null</c> if any step
        /// of the lookup fails or returns null. Result is cached per <c>SchemaComparisonResult</c>
        /// instance so repeated calls are O(1) after the first.
        /// </summary>
        /// <remarks>
        /// This is a reflection workaround for the lack of a public DacFx accessor exposing
        /// the comparison's platform. <c>TSqlModel.Version</c> would be the natural API but
        /// returns <c>Sql150</c> for Fabric Warehouse models (see
        /// <c>InternalModelUtils.CalculateVersionsForPlatform</c>). If DacFx adds a public
        /// <c>SchemaComparisonResult.Platform</c> property in the future, replace this with
        /// the direct call.
        /// </remarks>
        internal static string GetComparisonPlatform(SchemaComparisonResult result)
        {
            if (result == null)
            {
                return null;
            }

            // ConditionalWeakTable is thread-safe, but TryGetValue+Add can still race on the same key.
            // Cache empty string as the sentinel for "unknown" since ConditionalWeakTable does not allow null values.
            string cached = s_platformByResult.GetValue(result, r => TryGetComparisonPlatformCore(r) ?? string.Empty);
            return string.IsNullOrEmpty(cached) ? null : cached;
        }

        private static string TryGetComparisonPlatformCore(SchemaComparisonResult result)
        {
            try
            {
                if (!EnsureReflectionMembers(result.GetType()))
                {
                    return null;
                }

                object dataModel = s_dataModelProp.GetValue(result);
                if (dataModel == null)
                {
                    return null;
                }

                PropertyInfo dspProp = s_dspProp ?? dataModel.GetType().GetProperty(
                    "DatabaseSchemaProvider",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (dspProp == null)
                {
                    return null;
                }
                s_dspProp = dspProp;

                object dsp = dspProp.GetValue(dataModel);
                if (dsp == null)
                {
                    return null;
                }

                PropertyInfo platformProp = s_platformProp ?? dsp.GetType().GetProperty(
                    "Platform",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (platformProp == null)
                {
                    return null;
                }
                s_platformProp = platformProp;

                object platformValue = platformProp.GetValue(dsp);
                return platformValue?.ToString();
            }
            catch (Exception ex)
            {
                // Reflection failures are non-fatal: the platform pill simply won't render
                // and the user keeps their compare results. Log so the failure is diagnosable
                // from the STS log file without surfacing to the UI.
                Logger.Warning(string.Format("Schema compare: failed to detect comparison platform via reflection: {0}", ex));
                return null;
            }
        }

        private static bool EnsureReflectionMembers(Type resultType)
        {
            if (s_dataModelProp != null)
            {
                return true;
            }
            if (s_reflectionInitFailed)
            {
                return false;
            }

            lock (s_reflectionInitLock)
            {
                if (s_dataModelProp != null)
                {
                    return true;
                }
                if (s_reflectionInitFailed)
                {
                    return false;
                }

                PropertyInfo prop = resultType.GetProperty(
                    "DataModel",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null)
                {
                    s_reflectionInitFailed = true;
                    Logger.Warning("Schema compare: SchemaComparisonResult.DataModel property not found via reflection; platform pill will be unavailable.");
                    return false;
                }
                s_dataModelProp = prop;
                return true;
            }
        }
    }
}
