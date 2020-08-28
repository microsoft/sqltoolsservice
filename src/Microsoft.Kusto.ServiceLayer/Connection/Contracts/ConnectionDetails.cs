//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Message format for the initial connection request
    /// </summary>
    /// <remarks>
    /// If this contract is ever changed, be sure to update ConnectionDetailsExtensions methods.
    /// </remarks>
    public class ConnectionDetails : GeneralRequestDetails, IConnectionSummary
    {
        public ConnectionDetails() : base()
        {
        }

        /// <summary>
        /// Gets or sets the connection password
        /// </summary>
        public string Password 
        {
            get
            {
                return GetOptionValue<string>("password");
            }
            set
            {
                SetOptionValue("password", value);
            }
        }

        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
        public string ServerName
        {
            get
            {
                return GetOptionValue<string>("server");
            }

            set
            {
                SetOptionValue("server", value);
            }
        }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return GetOptionValue<string>("database");
            }

            set
            {
                SetOptionValue("database", value);
            }
        }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        public string UserName
        {
            get
            {
                return GetOptionValue<string>("user");
            }

            set
            {
                SetOptionValue("user", value);
            }
        }

        /// <summary>
        /// Gets or sets the authentication to use.
        /// </summary>
        public string AuthenticationType
        {
            get
            {
                return GetOptionValue<string>("authenticationType");
            }

            set
            {
                SetOptionValue("authenticationType", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether SQL Server uses SSL encryption for all data sent between the client and server if the server has a certificate installed.
        /// </summary>
        public bool? Encrypt
        {
            get
            {
                return GetOptionValue<bool?>("encrypt");
            }

            set
            {
                SetOptionValue("encrypt", value);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the channel will be encrypted while bypassing walking the certificate chain to validate trust.
        /// </summary>
        public bool? TrustServerCertificate
        {
            get
            {
                return GetOptionValue<bool?>("trustServerCertificate");
            }

            set
            {
                SetOptionValue("trustServerCertificate", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates if security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state.
        /// </summary>
        public bool? PersistSecurityInfo
        {
            get
            {
                return GetOptionValue<bool?>("persistSecurityInfo");
            }

            set
            {
                SetOptionValue("persistSecurityInfo", value);
            }
        }

        /// <summary>
        /// Gets or sets the length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
        /// </summary>
        public int? ConnectTimeout
        {
            get
            {
                return GetOptionValue<int?>("connectTimeout");
            }

            set
            {
                SetOptionValue("connectTimeout", value);
            }
        }

        /// <summary>
        /// The number of reconnections attempted after identifying that there was an idle connection failure.
        /// </summary>
        public int? ConnectRetryCount
        {
            get
            {
                return GetOptionValue<int?>("connectRetryCount");
            }

            set
            {
                SetOptionValue("connectRetryCount", value);
            }
        }

        /// <summary>
        /// Amount of time (in seconds) between each reconnection attempt after identifying that there was an idle connection failure.
        /// </summary>
        public int? ConnectRetryInterval
        {
            get
            {
                return GetOptionValue<int?>("connectRetryInterval");
            }

            set
            {
                SetOptionValue("connectRetryInterval", value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the application associated with the connection string.
        /// </summary>
        public string ApplicationName
        {
            get
            {
                return GetOptionValue<string>("applicationName");
            }

            set
            {
                SetOptionValue("applicationName", value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the workstation connecting to SQL Server.
        /// </summary>
        public string WorkstationId
        {
            get
            {
                return GetOptionValue<string>("workstationId");
            }

            set
            {
                SetOptionValue("workstationId", value);
            }
        }

        /// <summary>
        /// Declares the application workload type when connecting to a database in an SQL Server Availability Group.
        /// </summary>
        public string ApplicationIntent
        {
            get
            {
                return GetOptionValue<string>("applicationIntent");
            }

            set
            {
                SetOptionValue("applicationIntent", value);
            }
        }

        /// <summary>
        /// Gets or sets the SQL Server Language record name.
        /// </summary>
        public string CurrentLanguage
        {
            get
            {
                return GetOptionValue<string>("currentLanguage");
            }

            set
            {
                SetOptionValue("currentLanguage", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether the connection will be pooled or explicitly opened every time that the connection is requested.
        /// </summary>
        public bool? Pooling
        {
            get
            {
                return GetOptionValue<bool?>("pooling");
            }

            set
            {
                SetOptionValue("pooling", value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of connections allowed in the connection pool for this specific connection string.
        /// </summary>
        public int? MaxPoolSize
        {
            get
            {
                return GetOptionValue<int?>("maxPoolSize");
            }

            set
            {
                SetOptionValue("maxPoolSize", value);
            }
        }

        /// <summary>
        /// Gets or sets the minimum number of connections allowed in the connection pool for this specific connection string.
        /// </summary>
        public int? MinPoolSize
        {
            get
            {
                return GetOptionValue<int?>("minPoolSize");
            }

            set
            {
                SetOptionValue("minPoolSize", value);
            }
        }

        /// <summary>
        /// Gets or sets the minimum time, in seconds, for the connection to live in the connection pool before being destroyed.
        /// </summary>
        public int? LoadBalanceTimeout
        {
            get
            {
                return GetOptionValue<int?>("loadBalanceTimeout");
            }

            set
            {
                SetOptionValue("loadBalanceTimeout", value);
            }
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether replication is supported using the connection.
        /// </summary>
        public bool? Replication
        {
            get
            {
                return GetOptionValue<bool?>("replication");
            }

            set
            {
                SetOptionValue("replication", value);
            }
        }

        /// <summary>
        /// Gets or sets a string that contains the name of the primary data file. This includes the full path name of an attachable database.
        /// </summary>
        public string AttachDbFilename
        {
            get
            {
                return GetOptionValue<string>("attachDbFilename");
            }

            set
            {
                SetOptionValue("attachDbFilename", value);
            }
        }

        /// <summary>
        /// Gets or sets the name or address of the partner server to connect to if the primary server is down.
        /// </summary>
        public string FailoverPartner
        {
            get
            {
                return GetOptionValue<string>("failoverPartner");
            }

            set
            {
                SetOptionValue("failoverPartner", value);
            }
        }

        /// <summary>
        /// If your application is connecting to an AlwaysOn availability group (AG) on different subnets, setting MultiSubnetFailover=true provides faster detection of and connection to the (currently) active server.
        /// </summary>
        public bool? MultiSubnetFailover
        {
            get
            {
                return GetOptionValue<bool?>("multiSubnetFailover");
            }

            set
            {
                SetOptionValue("multiSubnetFailover", value);
            }
        }

        /// <summary>
        /// When true, an application can maintain multiple active result sets (MARS).
        /// </summary>
        public bool? MultipleActiveResultSets
        {
            get
            {
                return GetOptionValue<bool?>("multipleActiveResultSets");
            }

            set
            {
                SetOptionValue("multipleActiveResultSets", value);
            }
        }

        /// <summary>
        /// Gets or sets the size in bytes of the network packets used to communicate with an instance of SQL Server.
        /// </summary>
        public int? PacketSize
        {
            get
            {
                return GetOptionValue<int?>("packetSize");
            }

            set
            {
                SetOptionValue("packetSize", value);
            }
        }

        /// <summary>
        /// Gets or sets the port to use for the TCP/IP connection
        /// </summary>
        public int? Port
        {
            get
            {
                return GetOptionValue<int?>("port");
            }

            set
            {
                SetOptionValue("port", value);
            }
        }        

        /// <summary>
        /// Gets or sets a string value that indicates the type system the application expects.
        /// </summary>
        public string TypeSystemVersion
        {
            get
            {
                return GetOptionValue<string>("typeSystemVersion");
            }

            set
            {
                SetOptionValue("typeSystemVersion", value);
            }
        }

        /// <summary>
        /// Gets or sets a string value to be used as the connection string. If given, all other options will be ignored.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return GetOptionValue<string>("connectionString");
            }

            set
            {
                SetOptionValue("connectionString", value);
            }
        }

        /// <summary>
        /// Gets or sets the group ID
        /// </summary>
        public string GroupId 
        {
            get
            {
                return GetOptionValue<string>("groupId");
            }
            set
            {
                SetOptionValue("groupId", value);
            }
        }

        /// <summary>
        /// Gets or sets the database display name
        /// </summary>
        public string DatabaseDisplayName 
        {
            get
            {
                return GetOptionValue<string>("databaseDisplayName");
            }
            set
            {
                SetOptionValue("databaseDisplayName", value);
            }
        }

        public string AzureAccountToken
        {
            get
            {
                return GetOptionValue<string>("azureAccountToken");
            }
            set
            {
                SetOptionValue("azureAccountToken", value);
            }
        }

        public bool IsComparableTo(ConnectionDetails other)
        {
            if (other == null)
            {
                return false;
            }

            if (ServerName != other.ServerName
                || AuthenticationType != other.AuthenticationType
                || UserName != other.UserName
                || AzureAccountToken != other.AzureAccountToken)
            {
                return false;
            }

            // For database name, only compare if neither is empty. This is important
            // Since it allows for handling of connections to the default database, but is
            // not a 100% accurate heuristic.
            if (!string.IsNullOrEmpty(DatabaseName)
                && !string.IsNullOrEmpty(other.DatabaseName)
                && DatabaseName != other.DatabaseName)
            {
                return false;
            }

            return true;
        }
    }
}
