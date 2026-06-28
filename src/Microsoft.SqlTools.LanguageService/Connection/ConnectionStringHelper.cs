//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;
using static Microsoft.SqlTools.Utility.SqlConstants;

namespace Microsoft.SqlTools.LanguageService.Connection
{
    /// <summary>
    /// Builds SQL connection strings from <see cref="ConnectionDetails"/> instances.
    /// </summary>
    /// <remarks>
    /// This logic lives in the language service library so that components such as the scripter
    /// can build connection strings without taking a dependency on the service layer. The runtime
    /// configuration values (Sql Authentication Provider and whether to disable connection pooling)
    /// are passed in by callers so that the library does not need to reference the service layer.
    /// </remarks>
    public static class ConnectionStringHelper
    {
        /// <summary>
        /// Build a connection string from a connection details instance
        /// </summary>
        /// <param name="connectionDetails">Connection details</param>
        /// <param name="enableSqlAuthenticationProvider">Whether the configured 'Sql Authentication Provider' for 'Active Directory Interactive' authentication mode should be used when the user chooses 'Azure MFA'.</param>
        /// <param name="disablePooling">Whether to disable connection pooling for the built connection string.</param>
        public static string BuildConnectionString(ConnectionDetails connectionDetails, bool enableSqlAuthenticationProvider, bool disablePooling)
        {
            return CreateConnectionStringBuilder(connectionDetails, enableSqlAuthenticationProvider, disablePooling).ToString();
        }

        /// <summary>
        /// Build a connection string builder from a connection details instance
        /// </summary>
        /// <param name="connectionDetails">Connection details</param>
        /// <param name="enableSqlAuthenticationProvider">Whether the configured 'Sql Authentication Provider' for 'Active Directory Interactive' authentication mode should be used when the user chooses 'Azure MFA'.</param>
        /// <param name="disablePooling">Whether to disable connection pooling for the built connection string.</param>
        public static SqlConnectionStringBuilder CreateConnectionStringBuilder(ConnectionDetails connectionDetails, bool enableSqlAuthenticationProvider, bool disablePooling)
        {
            SqlConnectionStringBuilder connectionBuilder;

            // If connectionDetails has a connection string already, use it to initialize the connection builder, then override any provided options.
            // Otherwise use the server name, username, and password from the connection details.
            if (!string.IsNullOrEmpty(connectionDetails.ConnectionString))
            {
                connectionBuilder = new SqlConnectionStringBuilder(connectionDetails.ConnectionString);
            }
            else
            {
                // add alternate port to data source property if provided
                string dataSource = !connectionDetails.Port.HasValue
                    ? connectionDetails.ServerName
                    : $"{connectionDetails.ServerName},{connectionDetails.Port.Value}";

                connectionBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = dataSource
                };
            }

            // Check for any optional parameters
            if (!string.IsNullOrEmpty(connectionDetails.DatabaseName))
            {
                connectionBuilder.InitialCatalog = connectionDetails.DatabaseName;
            }
            if (!string.IsNullOrEmpty(connectionDetails.AuthenticationType))
            {
                switch (connectionDetails.AuthenticationType)
                {
                    case Integrated:
                        connectionBuilder.IntegratedSecurity = true;
                        break;
                    case SqlLogin:
                        // Don't erase username from connection string.
                        if (string.IsNullOrEmpty(connectionBuilder.UserID))
                        {
                            connectionBuilder.UserID = connectionDetails.UserName;
                        }
                        // Don't erase password from connection string.
                        if (string.IsNullOrEmpty(connectionBuilder.Password))
                        {
                            connectionBuilder.Password = string.IsNullOrEmpty(connectionDetails.Password)
                            ? string.Empty // Support empty password for accounts without password
                            : connectionDetails.Password;
                        }
                        connectionBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
                        break;
                    case AzureMFA:
                        if (enableSqlAuthenticationProvider)
                        {
                            if (string.IsNullOrEmpty(connectionBuilder.UserID))
                            {
                                connectionBuilder.UserID = connectionDetails.UserName;
                            }
                            // Only delegate to SqlAuthenticationProvider (MSAL) when no token
                            // has been pre-acquired by the client (e.g., VS Code sessions).
                            // When a token is already provided, keep Authentication as
                            // NotSpecified so ReliableSqlConnection can inject it via AccessToken.
                            if (string.IsNullOrEmpty(connectionDetails.AzureAccountToken))
                            {
                                connectionDetails.AuthenticationType = ActiveDirectoryInteractive;
                                connectionBuilder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
                            }
                            else
                            {
                                // To work with the provided token, UserID must be unset and the auth method must be NotSpecified.
                                connectionBuilder.UserID = "";
                                connectionBuilder.Authentication = SqlAuthenticationMethod.NotSpecified;
                            }
                        }
                        break;
                    case ActiveDirectoryInteractive:
                        if (string.IsNullOrEmpty(connectionDetails.AzureAccountToken))
                        {
                            // Don't erase username from connection string.
                            if (string.IsNullOrEmpty(connectionBuilder.UserID))
                            {
                                connectionBuilder.UserID = connectionDetails.UserName;
                            }
                            connectionBuilder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
                        }
                        else
                        {
                            // Cannot set UserID when a token has been pre-acquired.
                            // Do not mutate connectionDetails.UserName — only clear the builder field.
                            connectionBuilder.UserID = "";
                            // Explicitly clear Authentication in case the builder was initialized
                            // from a connection string that already includes an Authentication value.
                            connectionBuilder.Authentication = SqlAuthenticationMethod.NotSpecified;
                        }
                        break;
                    case ActiveDirectoryPassword:
                        // Don't erase username from connection string.
                        if (string.IsNullOrEmpty(connectionBuilder.UserID))
                        {
                            connectionBuilder.UserID = connectionDetails.UserName;
                        }
                        // Don't erase password from connection string.
                        if (string.IsNullOrEmpty(connectionBuilder.Password))
                        {
                            connectionBuilder.Password = connectionDetails.Password;
                        }
                        connectionBuilder.Authentication = SqlAuthenticationMethod.ActiveDirectoryPassword;
                        break;
                    case ActiveDirectoryDefault:
                        // UserID is optional, but if provided it can be used by the driver
                        // as the client ID for a user-assigned managed identity.
                        if (string.IsNullOrEmpty(connectionBuilder.UserID) && !string.IsNullOrEmpty(connectionDetails.UserName))
                        {
                            connectionBuilder.UserID = connectionDetails.UserName;
                        }
                        connectionBuilder.Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault;
                        break;
                    case ActiveDirectoryServicePrincipal:
                        // UserID is the Application (Client) ID; Password is the client secret.
                        // SqlClient performs the OAuth client-credentials flow natively.
                        if (string.IsNullOrEmpty(connectionBuilder.UserID))
                        {
                            connectionBuilder.UserID = connectionDetails.UserName;
                        }
                        if (string.IsNullOrEmpty(connectionBuilder.Password))
                        {
                            connectionBuilder.Password = connectionDetails.Password;
                        }
                        connectionBuilder.Authentication = SqlAuthenticationMethod.ActiveDirectoryServicePrincipal;
                        break;
                    default:
                        throw new ArgumentException(SR.ConnectionServiceConnStringInvalidAuthType(connectionDetails.AuthenticationType));
                }
            }
            if (!string.IsNullOrEmpty(connectionDetails.ColumnEncryptionSetting))
            {
                if (Enum.TryParse<SqlConnectionColumnEncryptionSetting>(connectionDetails.ColumnEncryptionSetting, true, out var value))
                {
                    connectionBuilder.ColumnEncryptionSetting = value;
                }
                else
                {
                    throw new ArgumentException(SR.ConnectionServiceConnStringInvalidColumnEncryptionSetting(connectionDetails.ColumnEncryptionSetting));
                }
            }
            if (!string.IsNullOrEmpty(connectionDetails.SecureEnclaves))
            {
                // Secure Enclaves is not mapped to SqlConnection, it's only used for throwing validation errors
                // when Enclave Attestation Protocol is missing.
                switch (connectionDetails.SecureEnclaves.ToUpper(CultureInfo.InvariantCulture))
                {
                    case "ENABLED":
                        if (string.IsNullOrEmpty(connectionDetails.EnclaveAttestationProtocol))
                        {
                            throw new ArgumentException(SR.ConnectionServiceConnStringMissingAttestationProtocolWithSecureEnclaves);
                        }
                        break;
                    case "DISABLED":
                        break;
                    default:
                        throw new ArgumentException(SR.ConnectionServiceConnStringInvalidSecureEnclaves(connectionDetails.SecureEnclaves));
                }
            }
            if (!string.IsNullOrEmpty(connectionDetails.EnclaveAttestationProtocol))
            {
                if (connectionBuilder.ColumnEncryptionSetting != SqlConnectionColumnEncryptionSetting.Enabled
                    || string.IsNullOrEmpty(connectionDetails.SecureEnclaves) || connectionDetails.SecureEnclaves.ToUpper(CultureInfo.InvariantCulture) == "DISABLED")
                {
                    throw new ArgumentException(SR.ConnectionServiceConnStringInvalidAlwaysEncryptedOptionCombination);
                }

                if (Enum.TryParse<SqlConnectionAttestationProtocol>(connectionDetails.EnclaveAttestationProtocol, true, out var value))
                {
                    connectionBuilder.AttestationProtocol = value;
                }
                else
                {
                    throw new ArgumentException(SR.ConnectionServiceConnStringInvalidEnclaveAttestationProtocol(connectionDetails.EnclaveAttestationProtocol));
                }
            }
            if (!string.IsNullOrEmpty(connectionDetails.EnclaveAttestationUrl))
            {
                if (connectionBuilder.ColumnEncryptionSetting != SqlConnectionColumnEncryptionSetting.Enabled
                    || string.IsNullOrEmpty(connectionDetails.SecureEnclaves) || connectionDetails.SecureEnclaves.ToUpper(CultureInfo.InvariantCulture) == "DISABLED")
                {
                    throw new ArgumentException(SR.ConnectionServiceConnStringInvalidAlwaysEncryptedOptionCombination);
                }

                if (connectionBuilder.AttestationProtocol == SqlConnectionAttestationProtocol.None)
                {
                    throw new ArgumentException(SR.ConnectionServiceConnStringInvalidAttestationProtocolNoneWithUrl);
                }

                connectionBuilder.EnclaveAttestationUrl = connectionDetails.EnclaveAttestationUrl;
            }
            else if (connectionBuilder.AttestationProtocol == SqlConnectionAttestationProtocol.AAS
                || connectionBuilder.AttestationProtocol == SqlConnectionAttestationProtocol.HGS)
            {
                throw new ArgumentException(SR.ConnectionServiceConnStringMissingAttestationUrlWithAttestationProtocol);
            }

            if (!string.IsNullOrEmpty(connectionDetails.Encrypt))
            {
                connectionBuilder.Encrypt = connectionDetails.Encrypt.ToLowerInvariant() switch
                {
                    "optional" or "false" or "no" => SqlConnectionEncryptOption.Optional,
                    "mandatory" or "true" or "yes" => SqlConnectionEncryptOption.Mandatory,
                    "strict" => SqlConnectionEncryptOption.Strict,
                    _ => throw new ArgumentException(SR.ConnectionServiceConnStringInvalidEncryptOption(connectionDetails.Encrypt))
                };
            }

            if (connectionDetails.TrustServerCertificate.HasValue)
            {
                connectionBuilder.TrustServerCertificate = connectionDetails.TrustServerCertificate.Value;
            }
            if (!string.IsNullOrEmpty(connectionDetails.HostNameInCertificate))
            {
                connectionBuilder.HostNameInCertificate = connectionDetails.HostNameInCertificate;
            }
            if (connectionDetails.PersistSecurityInfo.HasValue)
            {
                connectionBuilder.PersistSecurityInfo = connectionDetails.PersistSecurityInfo.Value;
            }
            if (connectionDetails.ConnectTimeout.HasValue)
            {
                connectionBuilder.ConnectTimeout = connectionDetails.ConnectTimeout.Value;
            }
            if (connectionDetails.CommandTimeout.HasValue)
            {
                connectionBuilder.CommandTimeout = connectionDetails.CommandTimeout.Value;
            }
            if (connectionDetails.ConnectRetryCount.HasValue)
            {
                connectionBuilder.ConnectRetryCount = connectionDetails.ConnectRetryCount.Value;
            }
            if (connectionDetails.ConnectRetryInterval.HasValue)
            {
                connectionBuilder.ConnectRetryInterval = connectionDetails.ConnectRetryInterval.Value;
            }
            if (!string.IsNullOrEmpty(connectionDetails.ApplicationName))
            {
                connectionBuilder.ApplicationName = connectionDetails.ApplicationName;
            }
            if (!string.IsNullOrEmpty(connectionDetails.WorkstationId))
            {
                connectionBuilder.WorkstationID = connectionDetails.WorkstationId;
            }
            if (!string.IsNullOrEmpty(connectionDetails.ApplicationIntent))
            {
                if (Enum.TryParse<ApplicationIntent>(connectionDetails.ApplicationIntent, true, out ApplicationIntent value))
                {
                    connectionBuilder.ApplicationIntent = value;
                }
                else
                {
                    throw new ArgumentException(SR.ConnectionServiceConnStringInvalidIntent(connectionDetails.ApplicationIntent));
                }
            }
            if (!string.IsNullOrEmpty(connectionDetails.CurrentLanguage))
            {
                connectionBuilder.CurrentLanguage = connectionDetails.CurrentLanguage;
            }
            if (connectionDetails.Pooling.HasValue)
            {
                connectionBuilder.Pooling = connectionDetails.Pooling.Value;
            }
            if (connectionDetails.MaxPoolSize.HasValue)
            {
                connectionBuilder.MaxPoolSize = connectionDetails.MaxPoolSize.Value;
            }
            if (connectionDetails.MinPoolSize.HasValue)
            {
                connectionBuilder.MinPoolSize = connectionDetails.MinPoolSize.Value;
            }
            if (connectionDetails.LoadBalanceTimeout.HasValue)
            {
                connectionBuilder.LoadBalanceTimeout = connectionDetails.LoadBalanceTimeout.Value;
            }
            if (connectionDetails.Replication.HasValue)
            {
                connectionBuilder.Replication = connectionDetails.Replication.Value;
            }
            if (!string.IsNullOrEmpty(connectionDetails.AttachDbFilename))
            {
                connectionBuilder.AttachDBFilename = connectionDetails.AttachDbFilename;
            }
            if (!string.IsNullOrEmpty(connectionDetails.FailoverPartner))
            {
                connectionBuilder.FailoverPartner = connectionDetails.FailoverPartner;
            }
            if (connectionDetails.MultiSubnetFailover.HasValue)
            {
                connectionBuilder.MultiSubnetFailover = connectionDetails.MultiSubnetFailover.Value;
            }
            if (connectionDetails.MultipleActiveResultSets.HasValue)
            {
                connectionBuilder.MultipleActiveResultSets = connectionDetails.MultipleActiveResultSets.Value;
            }
            if (connectionDetails.PacketSize.HasValue)
            {
                connectionBuilder.PacketSize = connectionDetails.PacketSize.Value;
            }
            if (!string.IsNullOrEmpty(connectionDetails.TypeSystemVersion))
            {
                connectionBuilder.TypeSystemVersion = connectionDetails.TypeSystemVersion;
            }
            if (disablePooling)
            {
                connectionBuilder.Pooling = false;
            }

            return connectionBuilder;
        }
    }
}
