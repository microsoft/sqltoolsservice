//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using static Microsoft.SqlTools.Utility.SqlConstants;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{

    /// <summary>
    /// Internal class for utilities shared between multiple schema compare operations
    /// </summary>
    internal static partial class SchemaCompareUtils
    {
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
                // set source and target scripts
                if (difference.SourceObject != null)
                {
                    string sourceScript = schemaComparisonResult.GetDiffEntrySourceScript(difference);

                    // Child scripts that do not use alter need to be added if they are being changed, ex: "EXECUTE sp_addextendedproperty...".
                    // Don't add scripts that start with alter because those are handled by a top level element's create
                    // ex: if a column changes, then the parent table's script will have the column updated, but GetDiffEntrySourceScript() on the child
                    // will return an alter table statement for updating that column when getting the child script. The child's alter script is unecessary
                    // for displaying the script in schema compare because the comparison displays the create scripts
                    if (!sourceScript.ToLowerInvariant().StartsWith("alter"))
                    {
                        diffEntry.SourceScript = FormatScript(sourceScript);
                    }
                }
                if (difference.TargetObject != null)
                {
                    string targetScript = schemaComparisonResult.GetDiffEntryTargetScript(difference);

                    // Child scripts that do not use alter need to be added if they are being changed, ex: "EXECUTE sp_addextendedproperty...".
                    // Don't add scripts that start with alter because those are handled by a top level element's create
                    // ex: if a column changes, then the parent table's script will have the column updated, but GetDiffEntrySourceScript() on the child
                    // will return an alter table script for updating that column when getting the child script. The child's alter script is unecessary
                    // for displaying the script in schema compare because the comparison displays the create scripts
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

        internal static SchemaCompareEndpoint CreateSchemaCompareEndpoint(SchemaCompareEndpointInfo endpointInfo, ConnectionInfo connInfo)
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
                        string connectionString = GetConnectionString(connInfo, endpointInfo.DatabaseName);

                        // Set Access Token only when authentication mode is not specified.
                        return connInfo.ConnectionDetails?.AzureAccountToken != null && connInfo.ConnectionDetails.AuthenticationType == AzureMFA
                            ? new SchemaCompareDatabaseEndpoint(connectionString, new AccessTokenProvider(connInfo.ConnectionDetails.AzureAccountToken))
                            : new SchemaCompareDatabaseEndpoint(connectionString);
                    }
                default:
                    {
                        throw new NotSupportedException($"Endpoint Type {endpointInfo.EndpointType} is not supported");
                    }
            }
        }

        internal static string GetConnectionString(ConnectionInfo connInfo, string databaseName)
        {
            if (connInfo == null)
            {
                return null;
            }

            connInfo.ConnectionDetails.DatabaseName = databaseName;
            return ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
        }


        internal static string RemoveExcessWhitespace(string script)
        {
            if (script != null)
            {
                // remove leading and trailing whitespace
                script = script.Trim();
                // replace all multiple spaces with single space
                script = GetScriptRegex().Replace(script, " ");
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

        [GeneratedRegex(" {2,}")]
        private static partial Regex GetScriptRegex();
    }
}
