//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// The model for deserializing settings.json
    /// </summary>
    public class ConnectionSetting
    {
        [JsonProperty("mssql.connections")]
        public List<InstanceInfo> Connections { get; set; }

        public InstanceInfo GetConnectionProfile(string profileName, string serverName)
        {
            if (!string.IsNullOrEmpty(profileName) && Connections != null)
            {
                var byProfileName = Connections.FirstOrDefault(x => x.ProfileName == profileName);
                if (byProfileName != null)
                {
                    return byProfileName;
                }
            }
            return Connections.FirstOrDefault(x => x.ServerName == serverName);
        }
    }

    /// <summary>
    /// The model to de-serializing the connections inside settings.json
    /// </summary>
    public class InstanceInfo
    {
        public InstanceInfo(string versionKey)
        {
            ConnectTimeout = 15;
            VersionKey = versionKey;
        }

        [JsonProperty("server")]
        public string ServerName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Database { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string User { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ProfileName { get; set; }

        public TestServerType ServerType { get; set; }

        public AuthenticationType AuthenticationType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RemoteSharePath { get; set; }

        public int ConnectTimeout { get; set; }

        public string VersionKey { get; set; }

        [JsonIgnore]
        public string ConnectTimeoutAsString
        {
            get { return ConnectTimeout.ToString(); }
            set
            {
                int temp;
                if (int.TryParse(value, out temp))
                {
                    this.ConnectTimeout = temp;
                }
                else
                {
                    this.ConnectTimeout = 15;
                }
            }
        }

        [JsonIgnore]
        public string MachineName
        {
            get
            {
                string serverName = ServerName;
                int index = ServerName.IndexOf('\\');
                if (index > 0)
                {
                    serverName = ServerName.Substring(0, index);
                }
                if (StringComparer.OrdinalIgnoreCase.Compare("(local)", serverName) == 0
                    || StringComparer.OrdinalIgnoreCase.Compare(".", serverName) == 0)
                {
                    serverName = Environment.MachineName;
                }
                return serverName;
            }
        }

        [JsonIgnore]
        public string InstanceName
        {
            get
            {
                string name = null;
                int index = ServerName.IndexOf('\\');
                if (index > 0)
                {
                    name = ServerName.Substring(index + 1);
                }
                return name;
            }
        }

    }
}
