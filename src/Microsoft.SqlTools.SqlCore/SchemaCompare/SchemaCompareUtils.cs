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
                // The per-difference "keep standalone constraint vs strip inline/redundant ALTER"
                // decision now lives entirely in DacFx: GetDiffEntryDisplay{Source,Target}Script
                // returns the script to render for this difference (or null if it is represented
                // inline in its parent), already gated per platform on the
                // DatabaseSchemaProvider.ConstraintsAreStandalone capability. On Fabric Warehouse
                // (SqlDwUnified) this keeps the standalone ALTER TABLE ... ADD CONSTRAINT script;
                // on every other platform it reproduces the legacy strip-on-alter behaviour. STS
                // therefore just renders whatever DacFx returns - no platform, constraint or
                // "starts with alter" logic of its own.
                if (difference.SourceObject != null)
                {
                    string sourceScript = schemaComparisonResult.GetDiffEntryDisplaySourceScript(difference);
                    if (sourceScript != null)
                    {
                        diffEntry.SourceScript = FormatScript(sourceScript);
                    }
                }
                if (difference.TargetObject != null)
                {
                    string targetScript = schemaComparisonResult.GetDiffEntryDisplayTargetScript(difference);
                    if (targetScript != null)
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
