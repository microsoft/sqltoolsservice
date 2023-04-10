//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    internal static class DacFxUtils
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
                            foreach (string objectTypeValue in excludeObjectTypeOptionsArray)
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
    }
}
