//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.Connection
{
    /// <summary>
    /// Extension methods for the ConnectionDetails contract class
    /// </summary>
    public static class ConnectionDetailsExtensions
    {
        /// <summary>
        /// Create a copy of a connection details object.
        /// </summary>
        public static ConnectionDetails Clone(this ConnectionDetails details)
        {
            return new ConnectionDetails()
            {
                ServerName = details.ServerName,
                DatabaseName = details.DatabaseName,
                UserName = details.UserName,
                Password = details.Password,
                AuthenticationType = details.AuthenticationType,
                Encrypt = details.Encrypt,
                TrustServerCertificate = details.TrustServerCertificate,
                PersistSecurityInfo = details.PersistSecurityInfo,
                ConnectTimeout = details.ConnectTimeout,
                ConnectRetryCount = details.ConnectRetryCount,
                ConnectRetryInterval = details.ConnectRetryInterval,
                ApplicationName = details.ApplicationName,
                WorkstationId = details.WorkstationId,
                ApplicationIntent = details.ApplicationIntent,
                CurrentLanguage = details.CurrentLanguage,
                Pooling = details.Pooling,
                MaxPoolSize = details.MaxPoolSize,
                MinPoolSize = details.MinPoolSize,
                LoadBalanceTimeout = details.LoadBalanceTimeout,
                Replication = details.Replication,
                AttachDbFilename = details.AttachDbFilename,
                FailoverPartner = details.FailoverPartner,
                MultiSubnetFailover = details.MultiSubnetFailover,
                MultipleActiveResultSets = details.MultipleActiveResultSets,
                PacketSize = details.PacketSize,
                TypeSystemVersion = details.TypeSystemVersion,
                ConnectionString = details.ConnectionString,
                Port = details.Port
            };
        }
    }
}
