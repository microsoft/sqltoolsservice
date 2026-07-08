//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{

    /// <summary>
    /// Internal class for utilities shared between multiple schema compare operations
    /// </summary>
    internal static class SchemaCompareUtils
    {
        private static readonly Regex ExcessWhitespaceRegex = new Regex(" {2,}", RegexOptions.Compiled);

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
                // For platforms that emit standalone constraints that assumption is wrong: the
                // ALTER script is the ONLY place the constraint is defined, so we must keep it.
                // DacFx tells us this per-difference via the public SchemaDifference
                // .IsStandaloneConstraint signal (true only on SqlDwUnified today), so STS no
                // longer needs to match object-type-name suffixes against a platform string.
                bool keepAlterScript = difference.IsStandaloneConstraint;

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
    }
}
