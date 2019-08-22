//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{

    /// <summary>
    /// Internal class for utilities shared between multiple schema compare operations
    /// </summary>
    internal static class SchemaCompareUtils
    {
        internal static DacDeployOptions CreateSchemaCompareOptions(DeploymentOptions deploymentOptions)
        {
            System.Reflection.PropertyInfo[] deploymentOptionsProperties = deploymentOptions.GetType().GetProperties();

            DacDeployOptions dacOptions = new DacDeployOptions();
            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                var prop = dacOptions.GetType().GetProperty(deployOptionsProp.Name);
                if (prop != null)
                {
                    prop.SetValue(dacOptions, deployOptionsProp.GetValue(deploymentOptions));
                }
            }
            return dacOptions;
        }

        internal static DiffEntry CreateDiffEntry(SchemaDifference difference, DiffEntry parent)
        {
            if (difference == null)
            {
                return null;
            }

            DiffEntry diffEntry = new DiffEntry();
            diffEntry.UpdateAction = difference.UpdateAction;
            diffEntry.DifferenceType = difference.DifferenceType;
            diffEntry.Name = difference.Name;

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
                    string sourceScript;
                    difference.SourceObject.TryGetScript(out sourceScript);
                    diffEntry.SourceScript = FormatScript(sourceScript);
                }
                if (difference.TargetObject != null)
                {
                    string targetScript;
                    difference.TargetObject.TryGetScript(out targetScript);
                    diffEntry.TargetScript = FormatScript(targetScript);
                }
            }

            diffEntry.Children = new List<DiffEntry>();

            foreach (SchemaDifference child in difference.Children)
            {
                diffEntry.Children.Add(CreateDiffEntry(child, diffEntry));
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
                ObjectIdentifier id = new ObjectIdentifier(sourceObj.NameParts);
                SchemaComparisonExcludedObjectId excludedObjId = new SchemaComparisonExcludedObjectId(sourceObj.SqlObjectType, id);
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
                case SchemaCompareEndpointType.Dacpac:
                    {
                        return new SchemaCompareDacpacEndpoint(endpointInfo.PackageFilePath);
                    }
                case SchemaCompareEndpointType.Database:
                    {
                        string connectionString = GetConnectionString(connInfo, endpointInfo.DatabaseName);
                        return connInfo.ConnectionDetails?.AzureAccountToken != null 
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
                script = Regex.Replace(script, " {2,}", " ");
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
