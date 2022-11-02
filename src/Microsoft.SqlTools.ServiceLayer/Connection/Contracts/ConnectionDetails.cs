//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
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
        /// Gets or sets a value that specifies that Always Encrypted functionality is enabled in a connection.
        /// </summary>
        public string ColumnEncryptionSetting
        {
            get
            {
                return GetOptionValue<string>("columnEncryptionSetting");
            }

            set
            {
                SetOptionValue("columnEncryptionSetting", value);
            }
        }

        /// <summary>
        /// Gets or sets a value for Attestation Protocol.
        /// </summary>
        public string EnclaveAttestationProtocol
        {
            get
            {
                return GetOptionValue<string>("attestationProtocol");
            }

            set
            {
                SetOptionValue("attestationProtocol", value);
            }
        }

        /// <summary>
        /// Gets or sets the enclave attestation Url to be used with enclave based Always Encrypted.
        /// </summary>
        public string EnclaveAttestationUrl
        {
            get
            {
                return GetOptionValue<string>("enclaveAttestationUrl");
            }

            set
            {
                SetOptionValue("enclaveAttestationUrl", value);
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="string"/> value that indicates encryption mode that SQL Server should use to perform SSL encryption for all the data sent between the client and server. Supported values are: Optional, Mandatory, Strict, True, False, Yes and No.
        /// Boolean 'true' and 'false' will also continue to be supported for backwards compatibility.
        /// </summary>
        public string? Encrypt
        {
            get
            {
                string? value = GetOptionValue<string?>("encrypt");
                if (string.IsNullOrEmpty(value))
                {
                    // Accept boolean values for backwards compatibility.
                    value = GetOptionValue<bool?>("encrypt")?.ToString();
                }
                return value;
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
        /// Gets or sets a value that indicates the host name in the certificate to be used for certificate validation when encryption is enabled.
        /// </summary>
        public string HostNameInCertificate
        {
            get
            {
                return GetOptionValue<string>("hostNameInCertificate");
            }

            set
            {
                SetOptionValue("hostNameInCertificate", value);
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

        public int? ExpiresOn
        {
            get
            {
                return GetOptionValue<int?>("expiresOn");
            }
            set
            {
                SetOptionValue("expiresOn", value);
            }
        }

        /// <summary>
        /// Compares all SQL Server Connection properties to be able to identify differences in current instance and provided instance appropriately.
        /// </summary>
        /// <param name="other">Instance to compare with.</param>
        /// <returns>True if comparison yeilds no differences, otherwise false.</returns>
        public bool IsComparableTo(ConnectionDetails other)
            => other != null
            && string.Equals(ApplicationIntent, other.ApplicationIntent, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(ApplicationName, other.ApplicationName, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(AttachDbFilename, other.AttachDbFilename, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(AuthenticationType, other.AuthenticationType, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(AzureAccountToken, other.AzureAccountToken, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(ColumnEncryptionSetting, other.ColumnEncryptionSetting, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(ConnectionString, other.ConnectionString, System.StringComparison.InvariantCultureIgnoreCase)
            && ConnectRetryCount == other.ConnectRetryCount
            && ConnectRetryInterval == other.ConnectRetryInterval
            && ConnectTimeout == other.ConnectTimeout
            && string.Equals(CurrentLanguage, other.CurrentLanguage, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(DatabaseDisplayName, other.DatabaseDisplayName, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(DatabaseName, other.DatabaseName, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(EnclaveAttestationProtocol, other.EnclaveAttestationProtocol, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(EnclaveAttestationUrl, other.EnclaveAttestationUrl, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(Encrypt, other.Encrypt, System.StringComparison.InvariantCultureIgnoreCase)
            && ExpiresOn == other.ExpiresOn
            && string.Equals(FailoverPartner, other.FailoverPartner, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(HostNameInCertificate, other.HostNameInCertificate, System.StringComparison.InvariantCultureIgnoreCase)
            && LoadBalanceTimeout == other.LoadBalanceTimeout
            && MaxPoolSize == other.MaxPoolSize
            && MinPoolSize == other.MinPoolSize
            && MultipleActiveResultSets == other.MultipleActiveResultSets
            && MultiSubnetFailover == other.MultiSubnetFailover
            && PacketSize == other.PacketSize
            && string.Equals(Password, other.Password, System.StringComparison.InvariantCultureIgnoreCase)
            && PersistSecurityInfo == other.PersistSecurityInfo
            && Pooling == other.Pooling
            && Port == other.Port
            && Replication == other.Replication
            && string.Equals(ServerName, other.ServerName, System.StringComparison.InvariantCultureIgnoreCase)
            && TrustServerCertificate == other.TrustServerCertificate
            && string.Equals(TypeSystemVersion, other.TypeSystemVersion, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(UserName, other.UserName, System.StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(WorkstationId, other.WorkstationId, System.StringComparison.InvariantCultureIgnoreCase);
    }
}
