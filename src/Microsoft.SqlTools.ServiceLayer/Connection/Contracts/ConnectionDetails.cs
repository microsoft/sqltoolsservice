//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Message format for the initial connection request
    /// </summary>
    /// <remarks>
    /// If this contract is ever changed, be sure to update ConnectionDetailsExtensions methods.
    /// </remarks>
    public class ConnectionDetails : ConnectionSummary
    {
        /// <summary>
        /// Gets or Sets the connection options
        /// </summary>
        public Dictionary<string, object> Options { get; set; }

        /// <summary>
        /// Gets or sets the connection password
        /// </summary>
        /// <returns></returns>
        public string Password {
            get
            {
                return getOptionValue<string>("password");
            }
            set
            {
                setOptionValue("password", value);
            }
        }

        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
        public override string ServerName
        {
            get
            {
                return getOptionValue<string>("server");
            }

            set
            {
                setOptionValue("server", value);
            }
        }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        public override string DatabaseName
        {
            get
            {
                return getOptionValue<string>("database");
            }

            set
            {
                setOptionValue("database", value);
            }
        }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        public override string UserName
        {
            get
            {
                return getOptionValue<string>("user");
            }

            set
            {
                setOptionValue("user", value);
            }
        }

        /// <summary>
        /// Gets or sets the authentication to use.
        /// </summary>
        public string AuthenticationType
        {
            get
            {
                return getOptionValue<string>("authenticationType");
            }

            set
            {
                setOptionValue("authenticationType", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether SQL Server uses SSL encryption for all data sent between the client and server if the server has a certificate installed.
        /// </summary>
        public bool? Encrypt
        {
            get
            {
                return getOptionValue<bool?>("encrypt");
            }

            set
            {
                setOptionValue("encrypt", value);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the channel will be encrypted while bypassing walking the certificate chain to validate trust.
        /// </summary>
        public bool? TrustServerCertificate
        {
            get
            {
                return getOptionValue<bool?>("trustServerCertificate");
            }

            set
            {
                setOptionValue("trustServerCertificate", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates if security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state.
        /// </summary>
        public bool? PersistSecurityInfo
        {
            get
            {
                return getOptionValue<bool?>("persistSecurityInfo");
            }

            set
            {
                setOptionValue("persistSecurityInfo", value);
            }
        }

        /// <summary>
        /// Gets or sets the length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
        /// </summary>
        public int? ConnectTimeout
        {
            get
            {
                return getOptionValue<int?>("connectTimeout");
            }

            set
            {
                setOptionValue("connectTimeout", value);
            }
        }

        /// <summary>
        /// The number of reconnections attempted after identifying that there was an idle connection failure.
        /// </summary>
        public int? ConnectRetryCount
        {
            get
            {
                return getOptionValue<int?>("connectRetryCount");
            }

            set
            {
                setOptionValue("connectRetryCount", value);
            }
        }

        /// <summary>
        /// Amount of time (in seconds) between each reconnection attempt after identifying that there was an idle connection failure.
        /// </summary>
        public int? ConnectRetryInterval
        {
            get
            {
                return getOptionValue<int?>("connectRetryInterval");
            }

            set
            {
                setOptionValue("connectRetryInterval", value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the application associated with the connection string.
        /// </summary>
        public string ApplicationName
        {
            get
            {
                return getOptionValue<string>("applicationName");
            }

            set
            {
                setOptionValue("applicationName", value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the workstation connecting to SQL Server.
        /// </summary>
        public string WorkstationId
        {
            get
            {
                return getOptionValue<string>("workstationId");
            }

            set
            {
                setOptionValue("workstationId", value);
            }
        }

        /// <summary>
        /// Declares the application workload type when connecting to a database in an SQL Server Availability Group.
        /// </summary>
        public string ApplicationIntent
        {
            get
            {
                return getOptionValue<string>("applicationIntent");
            }

            set
            {
                setOptionValue("applicationIntent", value);
            }
        }

        /// <summary>
        /// Gets or sets the SQL Server Language record name.
        /// </summary>
        public string CurrentLanguage
        {
            get
            {
                return getOptionValue<string>("currentLanguage");
            }

            set
            {
                setOptionValue("currentLanguage", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether the connection will be pooled or explicitly opened every time that the connection is requested.
        /// </summary>
        public bool? Pooling
        {
            get
            {
                return getOptionValue<bool?>("pooling");
            }

            set
            {
                setOptionValue("pooling", value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of connections allowed in the connection pool for this specific connection string.
        /// </summary>
        public int? MaxPoolSize
        {
            get
            {
                return getOptionValue<int?>("maxPoolSize");
            }

            set
            {
                setOptionValue("maxPoolSize", value);
            }
        }

        /// <summary>
        /// Gets or sets the minimum number of connections allowed in the connection pool for this specific connection string.
        /// </summary>
        public int? MinPoolSize
        {
            get
            {
                return getOptionValue<int?>("minPoolSize");
            }

            set
            {
                setOptionValue("minPoolSize", value);
            }
        }

        /// <summary>
        /// Gets or sets the minimum time, in seconds, for the connection to live in the connection pool before being destroyed.
        /// </summary>
        public int? LoadBalanceTimeout
        {
            get
            {
                return getOptionValue<int?>("loadBalanceTimeout");
            }

            set
            {
                setOptionValue("loadBalanceTimeout", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether replication is supported using the connection.
        /// </summary>
        public bool? Replication
        {
            get
            {
                return getOptionValue<bool?>("replication");
            }

            set
            {
                setOptionValue("replication", value);
            }
        }

        /// <summary>
        /// Gets or sets a string that contains the name of the primary data file. This includes the full path name of an attachable database.
        /// </summary>
        public string AttachDbFilename
        {
            get
            {
                return getOptionValue<string>("attachDbFilename");
            }

            set
            {
                setOptionValue("attachDbFilename", value);
            }
        }

        /// <summary>
        /// Gets or sets the name or address of the partner server to connect to if the primary server is down.
        /// </summary>
        public string FailoverPartner
        {
            get
            {
                return getOptionValue<string>("failoverPartner");
            }

            set
            {
                setOptionValue("failoverPartner", value);
            }
        }

        /// <summary>
        /// If your application is connecting to an AlwaysOn availability group (AG) on different subnets, setting MultiSubnetFailover=true provides faster detection of and connection to the (currently) active server.
        /// </summary>
        public bool? MultiSubnetFailover
        {
            get
            {
                return getOptionValue<bool?>("multiSubnetFailover");
            }

            set
            {
                setOptionValue("multiSubnetFailover", value);
            }
        }

        /// <summary>
        /// When true, an application can maintain multiple active result sets (MARS).
        /// </summary>
        public bool? MultipleActiveResultSets
        {
            get
            {
                return getOptionValue<bool?>("multipleActiveResultSets");
            }

            set
            {
                setOptionValue("multipleActiveResultSets", value);
            }
        }

        /// <summary>
        /// Gets or sets the size in bytes of the network packets used to communicate with an instance of SQL Server.
        /// </summary>
        public int? PacketSize
        {
            get
            {
                return getOptionValue<int?>("packetSize");
            }

            set
            {
                setOptionValue("packetSize", value);
            }
        }

        /// <summary>
        /// Gets or sets a string value that indicates the type system the application expects.
        /// </summary>
        public string TypeSystemVersion
        {
            get
            {
                return getOptionValue<string>("typeSystemVersion");
            }

            set
            {
                setOptionValue("typeSystemVersion", value);
            }
        }

        private T getOptionValue<T>(string name)
        {
            T result = default(T);
            if (Options != null && Options.ContainsKey(name))
            {
                object value = Options[name];
                result = value != null ? (T)value : default(T);
            }
            return result;
        }

        private void setOptionValue<T>(string name, T value)
        {
            Options = Options ?? new Dictionary<string, object>();
            if (Options.ContainsKey(name))
            {
                Options[name] = value;
            }
            else
            {
                Options.Add(name, value);
            }
        }
    }
}
