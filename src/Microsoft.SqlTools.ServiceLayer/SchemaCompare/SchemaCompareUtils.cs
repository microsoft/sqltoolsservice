//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{

    /// <summary>
    /// Internal class for utilities shared between multiple schema compare operations
    /// </summary>
    internal static class SchemaCompareUtils
    {
        /// <summary>
        /// Converts DeploymentOptions used in STS and ADS to DacDeployOptions which can be passed to the DacFx apis
        /// </summary>
        /// <param name="deploymentOptions"></param>
        /// <returns>DacDeployOptions</returns
        internal static DacDeployOptions CreateSchemaCompareOptions(DeploymentOptions deploymentOptions)
        {
            try
            {
                PropertyInfo[] deploymentOptionsProperties = deploymentOptions.GetType().GetProperties();

                DacDeployOptions dacOptions = new DacDeployOptions();
                Type propType = dacOptions.GetType();
                Dictionary<string, DeploymentOptionProperty<bool>> booleanOptionsDictionary = new Dictionary<string, DeploymentOptionProperty<bool>>();

                foreach (PropertyInfo deployOptionsProp in deploymentOptionsProperties)
                {
                    var prop = propType.GetProperty(deployOptionsProp.Name);
                    // Set the excludeObjectTypes values to the DacDeployOptions
                    if (prop != null && deployOptionsProp.Name == nameof(deploymentOptions.ExcludeObjectTypes))
                    {
                        List<ObjectType> finalExcludeObjects = new List<ObjectType> { };
                        var val = deployOptionsProp.GetValue(deploymentOptions);
                        string[] excludeObjectTypeOptionsArray = (string[])val.GetType().GetProperty("Value").GetValue(val);

                        if (excludeObjectTypeOptionsArray != null)
                        {
                            foreach(string objectTypeValue in excludeObjectTypeOptionsArray)
                            {
                                ObjectType objectTypeName = new ObjectType(); 
                                   
                                if (objectTypeValue != null && Enum.TryParse(objectTypeValue, ignoreCase: true, out objectTypeName))
                                {
                                    finalExcludeObjects.Add(objectTypeName);
                                }
                                else
                                {
                                    Logger.Write(TraceEventType.Error, string.Format($"{objectTypeValue} is not part of ObjectTypes enum"));
                                }
                            }
                            // set final values to excludeObjectType property
                            prop.SetValue(dacOptions, finalExcludeObjects.ToArray());
                        }
                    }

                    // BooleanOptionsDictionary has all the deployment options and is being processed separately in the second iteration by collecting here
                    if (deployOptionsProp.Name == nameof(deploymentOptions.BooleanOptionsDictionary))
                    {
                        booleanOptionsDictionary = deploymentOptions.BooleanOptionsDictionary as Dictionary<string, DeploymentOptionProperty<bool>>;
                    }
                }

                // Iterating through the updated boolean options coming from the booleanOptionsDictionary and assigning them to DacDeployOptions
                foreach (KeyValuePair<string, DeploymentOptionProperty<bool>> deployOptionsProp in booleanOptionsDictionary)
                {
                    var prop = propType.GetProperty(deployOptionsProp.Key);
                    if (prop != null)
                    {
                        var selectedVal = deployOptionsProp.Value.Value;
                        prop.SetValue(dacOptions, selectedVal);
                    }
                }
                return dacOptions;
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("Schema compare create options model failed: {0}", e.Message));
                throw;
            }
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
                case SchemaCompareEndpointType.Project:
                    {
                        return new SchemaCompareProjectEndpoint(endpointInfo.ProjectFilePath, endpointInfo.TargetScripts, endpointInfo.DataSchemaProvider);
                    }
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
