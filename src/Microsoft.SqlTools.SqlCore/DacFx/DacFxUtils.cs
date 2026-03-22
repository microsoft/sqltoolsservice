//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.SqlCore.DacFx.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.DacFx
{
    /// <summary>
    /// Utility methods for converting DeploymentOptions to DacFx DacDeployOptions.
    /// </summary>
    public static class DacFxUtils
    {
        /// <summary>
        /// Converts DeploymentOptions to DacDeployOptions which can be passed to the DacFx APIs.
        /// </summary>
        public static DacDeployOptions CreateDeploymentOptions(DeploymentOptions? deploymentOptions = null)
        {
            try
            {
                deploymentOptions = deploymentOptions ?? new DeploymentOptions();
                PropertyInfo[] deploymentOptionsProperties = deploymentOptions.GetType().GetProperties();

                DacDeployOptions dacOptions = new DacDeployOptions();
                Type propType = dacOptions.GetType();
                Dictionary<string, DeploymentOptionProperty<bool>> booleanOptionsDictionary = new Dictionary<string, DeploymentOptionProperty<bool>>();

                foreach (PropertyInfo deployOptionsProp in deploymentOptionsProperties)
                {
                    var prop = propType.GetProperty(deployOptionsProp.Name);
                    if (prop != null && deployOptionsProp.Name == nameof(deploymentOptions.ExcludeObjectTypes))
                    {
                        List<ObjectType> finalExcludeObjects = new List<ObjectType> { };
                        var val = deployOptionsProp.GetValue(deploymentOptions);
                        string[]? excludeObjectTypeOptionsArray = (string[]?)val?.GetType()?.GetProperty("Value")?.GetValue(val);

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
                                    Logger.Error(string.Format($"{objectTypeValue} is not part of ObjectTypes enum"));
                                }
                            }
                            prop.SetValue(dacOptions, finalExcludeObjects.ToArray());
                        }
                    }

                    if (deployOptionsProp.Name == nameof(deploymentOptions.BooleanOptionsDictionary))
                    {
                        booleanOptionsDictionary = deploymentOptions.BooleanOptionsDictionary as Dictionary<string, DeploymentOptionProperty<bool>>;
                    }
                }

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
                Logger.Error(string.Format("Schema compare create options model failed: {0}", e.Message));
                throw;
            }
        }
    }
}
