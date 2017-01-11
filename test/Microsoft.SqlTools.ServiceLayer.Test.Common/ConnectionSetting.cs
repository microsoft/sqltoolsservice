//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Globalization;
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
        public List<ConnectionProfile> Connections { get; set; }

        public ConnectionProfile GetConnectionProfile(string profileName, string serverName)
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
    /// The model to deserializing the connections inside settings.json
    /// </summary>
    public class ConnectionProfile
    {
        public const string CRED_PREFIX = "Microsoft.SqlTools";
        public const string CRED_SEPARATOR = "|";
        public const string CRED_SERVER_PREFIX = "server:";
        public const string CRED_DB_PREFIX = "db:";
        public const string CRED_USER_PREFIX = "user:";
        public const string CRED_ITEMTYPE_PREFIX = "itemtype:";

        [JsonProperty("server")]
        public string ServerName { get; set; }
        public string Database { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string ProfileName { get; set; }

        public TestServerType ServerType { get; set; }

        public AuthenticationType AuthenticationType { get; set; }


        public string formatCredentialId(string itemType = "Profile")
        {
            if (!string.IsNullOrEmpty(ServerName))
            {
                List<string> cred = new List<string>();
                cred.Add(CRED_PREFIX);
                AddToList(itemType, CRED_ITEMTYPE_PREFIX, cred);
                AddToList(ServerName, CRED_SERVER_PREFIX, cred);
                AddToList(Database, CRED_DB_PREFIX, cred);
                AddToList(User, CRED_USER_PREFIX, cred);
                return string.Join(CRED_SEPARATOR, cred.ToArray());
            }
            return null;
        }
        private void AddToList(string item, string prefix, List<string> list)
        {
            if (!string.IsNullOrEmpty(item))
            {
                list.Add(string.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, item));
            }
        }
    }
}
