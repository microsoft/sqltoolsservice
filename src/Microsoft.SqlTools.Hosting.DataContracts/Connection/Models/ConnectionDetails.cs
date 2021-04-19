using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    public class ConnectionDetails: GeneralRequestDetails
    {
        /// <summary>
        /// Gets or sets the connection password
        /// </summary>
        public string Password 
        {
            get => GetOptionValue<string>("password");
            set => SetOptionValue("password", value);
        }

        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
        public string ServerName
        {
            get => GetOptionValue<string>("server");
            set => SetOptionValue("server", value);
        }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        public string DatabaseName
        {
            get => GetOptionValue<string>("database");
            set => SetOptionValue("database", value);
        }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        public string UserName
        {
            get => GetOptionValue<string>("user");
            set => SetOptionValue("user", value);
        }

        /// <summary>
        /// Gets or sets the authentication to use.
        /// </summary>
        public string AuthenticationType
        {
            get => GetOptionValue<string>("authenticationType");
            set => SetOptionValue("authenticationType", value);
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether SQL Server uses SSL encryption for all data sent between the client and server if the server has a certificate installed.
        /// </summary>
        public bool? Encrypt
        {
            get => GetOptionValue<bool?>("encrypt");
            set => SetOptionValue("encrypt", value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the channel will be encrypted while bypassing walking the certificate chain to validate trust.
        /// </summary>
        public bool? TrustServerCertificate
        {
            get => GetOptionValue<bool?>("trustServerCertificate");
            set => SetOptionValue("trustServerCertificate", value);
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates if security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state.
        /// </summary>
        public bool? PersistSecurityInfo
        {
            get => GetOptionValue<bool?>("persistSecurityInfo");
            set => SetOptionValue("persistSecurityInfo", value);
        }

        /// <summary>
        /// Gets or sets the length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
        /// </summary>
        public int? ConnectTimeout
        {
            get => GetOptionValue<int?>("connectTimeout");
            set => SetOptionValue("connectTimeout", value);
        }

        /// <summary>
        /// The number of reconnections attempted after identifying that there was an idle connection failure.
        /// </summary>
        public int? ConnectRetryCount
        {
            get => GetOptionValue<int?>("connectRetryCount");
            set => SetOptionValue("connectRetryCount", value);
        }

        /// <summary>
        /// Amount of time (in seconds) between each reconnection attempt after identifying that there was an idle connection failure.
        /// </summary>
        public int? ConnectRetryInterval
        {
            get => GetOptionValue<int?>("connectRetryInterval");
            set => SetOptionValue("connectRetryInterval", value);
        }

        /// <summary>
        /// Gets or sets the name of the application associated with the connection string.
        /// </summary>
        public string ApplicationName
        {
            get => GetOptionValue<string>("applicationName");
            set => SetOptionValue("applicationName", value);
        }

        /// <summary>
        /// Gets or sets the name of the workstation connecting to SQL Server.
        /// </summary>
        public string WorkstationId
        {
            get => GetOptionValue<string>("workstationId");
            set => SetOptionValue("workstationId", value);
        }

        /// <summary>
        /// Declares the application workload type when connecting to a database in an SQL Server Availability Group.
        /// </summary>
        public string ApplicationIntent
        {
            get => GetOptionValue<string>("applicationIntent");
            set => SetOptionValue("applicationIntent", value);
        }

        /// <summary>
        /// Gets or sets the SQL Server Language record name.
        /// </summary>
        public string CurrentLanguage
        {
            get => GetOptionValue<string>("currentLanguage");
            set => SetOptionValue("currentLanguage", value);
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether the connection will be pooled or explicitly opened every time that the connection is requested.
        /// </summary>
        public bool? Pooling
        {
            get => GetOptionValue<bool?>("pooling");
            set => SetOptionValue("pooling", value);
        }

        /// <summary>
        /// Gets or sets the maximum number of connections allowed in the connection pool for this specific connection string.
        /// </summary>
        public int? MaxPoolSize
        {
            get => GetOptionValue<int?>("maxPoolSize");
            set => SetOptionValue("maxPoolSize", value);
        }

        /// <summary>
        /// Gets or sets the minimum number of connections allowed in the connection pool for this specific connection string.
        /// </summary>
        public int? MinPoolSize
        {
            get => GetOptionValue<int?>("minPoolSize");
            set => SetOptionValue("minPoolSize", value);
        }

        /// <summary>
        /// Gets or sets the minimum time, in seconds, for the connection to live in the connection pool before being destroyed.
        /// </summary>
        public int? LoadBalanceTimeout
        {
            get => GetOptionValue<int?>("loadBalanceTimeout");
            set => SetOptionValue("loadBalanceTimeout", value);
        }

        /// <summary>
        /// Gets or sets a Boolean value that indicates whether replication is supported using the connection.
        /// </summary>
        public bool? Replication
        {
            get => GetOptionValue<bool?>("replication");
            set => SetOptionValue("replication", value);
        }

        /// <summary>
        /// Gets or sets a string that contains the name of the primary data file. This includes the full path name of an attachable database.
        /// </summary>
        public string AttachDbFilename
        {
            get => GetOptionValue<string>("attachDbFilename");
            set => SetOptionValue("attachDbFilename", value);
        }

        /// <summary>
        /// Gets or sets the name or address of the partner server to connect to if the primary server is down.
        /// </summary>
        public string FailoverPartner
        {
            get => GetOptionValue<string>("failoverPartner");
            set => SetOptionValue("failoverPartner", value);
        }

        /// <summary>
        /// If your application is connecting to an AlwaysOn availability group (AG) on different subnets, setting MultiSubnetFailover=true provides faster detection of and connection to the (currently) active server.
        /// </summary>
        public bool? MultiSubnetFailover
        {
            get => GetOptionValue<bool?>("multiSubnetFailover");
            set => SetOptionValue("multiSubnetFailover", value);
        }

        /// <summary>
        /// When true, an application can maintain multiple active result sets (MARS).
        /// </summary>
        public bool? MultipleActiveResultSets
        {
            get => GetOptionValue<bool?>("multipleActiveResultSets");
            set => SetOptionValue("multipleActiveResultSets", value);
        }

        /// <summary>
        /// Gets or sets the size in bytes of the network packets used to communicate with an instance of SQL Server.
        /// </summary>
        public int? PacketSize
        {
            get => GetOptionValue<int?>("packetSize");
            set => SetOptionValue("packetSize", value);
        }

        /// <summary>
        /// Gets or sets the port to use for the TCP/IP connection
        /// </summary>
        public int? Port
        {
            get => GetOptionValue<int?>("port");
            set => SetOptionValue("port", value);
        }        

        /// <summary>
        /// Gets or sets a string value that indicates the type system the application expects.
        /// </summary>
        public string TypeSystemVersion
        {
            get => GetOptionValue<string>("typeSystemVersion");
            set => SetOptionValue("typeSystemVersion", value);
        }

        /// <summary>
        /// Gets or sets a string value to be used as the connection string. If given, all other options will be ignored.
        /// </summary>
        public string ConnectionString
        {
            get => GetOptionValue<string>("connectionString");
            set => SetOptionValue("connectionString", value);
        }

        /// <summary>
        /// Gets or sets the group ID
        /// </summary>
        public string GroupId 
        {
            get => GetOptionValue<string>("groupId");
            set => SetOptionValue("groupId", value);
        }

        /// <summary>
        /// Gets or sets the database display name
        /// </summary>
        public string DatabaseDisplayName 
        {
            get => GetOptionValue<string>("databaseDisplayName");
            set => SetOptionValue("databaseDisplayName", value);
        }

        public string AccountToken
        {
            get => GetOptionValue<string>("azureAccountToken");
            set => SetOptionValue("azureAccountToken", value);
        }
    }
}