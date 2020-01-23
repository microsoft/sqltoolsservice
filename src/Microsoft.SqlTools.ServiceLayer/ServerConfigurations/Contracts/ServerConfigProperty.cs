//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts
{
    /// <summary>
    /// A wrapper for SMO config property
    /// </summary>
    public class ServerConfigProperty
    {
        public int Number { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public int ConfigValue { get; set; }

        public int Maximum { get; set; }

        public int Minimum { get; set; }

        public static ServerConfigProperty ToServerConfigProperty(ConfigProperty configProperty)
        {
            if (configProperty != null)
            {
                return new ServerConfigProperty
                {
                    Number = configProperty.Number,
                    DisplayName = configProperty.DisplayName,
                    ConfigValue = configProperty.ConfigValue,
                    Maximum = configProperty.Maximum,
                    Minimum = configProperty.Minimum
                };
            }
            return null;
        }
    }
}
