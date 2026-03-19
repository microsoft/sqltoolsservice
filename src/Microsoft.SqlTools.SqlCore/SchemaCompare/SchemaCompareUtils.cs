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
    /// Host-agnostic utility methods shared between multiple schema compare operations.
    /// </summary>
    public static class SchemaCompareUtils
    {
        private static readonly Regex ExcessWhitespaceRegex = new Regex(" {2,}", RegexOptions.Compiled);

        /// <summary>
        /// Creates a DiffEntry from a SchemaDifference.
        /// </summary>
        public static DiffEntry CreateDiffEntry(SchemaDifference difference, DiffEntry parent, SchemaComparisonResult schemaComparisonResult)
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
                if (difference.SourceObject != null)
                {
                    string sourceScript = schemaComparisonResult.GetDiffEntrySourceScript(difference);
                    if (!sourceScript.ToLowerInvariant().StartsWith("alter"))
                    {
                        diffEntry.SourceScript = FormatScript(sourceScript);
                    }
                }
                if (difference.TargetObject != null)
                {
                    string targetScript = schemaComparisonResult.GetDiffEntryTargetScript(difference);
                    if (!targetScript.ToLowerInvariant().StartsWith("alter"))
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

        /// <summary>
        /// Creates a SchemaComparisonExcludedObjectId from a SchemaCompareObjectId.
        /// </summary>
        public static SchemaComparisonExcludedObjectId CreateExcludedObject(SchemaCompareObjectId sourceObj)
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

        /// <summary>
        /// Creates a DacFx SchemaCompareEndpoint from endpoint info using the connection provider.
        /// </summary>
        public static SchemaCompareEndpoint CreateSchemaCompareEndpoint(SchemaCompareEndpointInfo endpointInfo, ISchemaCompareConnectionProvider connectionProvider)
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
                        string accessToken = connectionProvider.GetAccessToken(endpointInfo);

                        return accessToken != null
                            ? new SchemaCompareDatabaseEndpoint(connectionString, new AccessTokenProvider(accessToken))
                            : new SchemaCompareDatabaseEndpoint(connectionString);
                    }
                default:
                    {
                        throw new NotSupportedException($"Endpoint Type {endpointInfo.EndpointType} is not supported");
                    }
            }
        }

        /// <summary>
        /// Removes excess whitespace from a script string.
        /// </summary>
        public static string RemoveExcessWhitespace(string script)
        {
            if (script != null)
            {
                script = script.Trim();
                script = ExcessWhitespaceRegex.Replace(script, " ");
            }
            return script;
        }

        /// <summary>
        /// Formats a script by trimming whitespace and appending GO.
        /// </summary>
        public static string FormatScript(string script)
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
