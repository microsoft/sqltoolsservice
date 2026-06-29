//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Base type carrying the connection identity, details, and intellisense metrics required by the
    /// language service binding queue and completion service. The hosting service layer derives from
    /// this type to add transport/SQL-specific state.
    /// </summary>
    public abstract class ConnectionInfoBase
    {
        /// <summary>
        /// Initializes the shared connection state.
        /// </summary>
        protected ConnectionInfoBase(string ownerUri, ConnectionDetails connectionDetails)
        {
            OwnerUri = ownerUri;
            ConnectionDetails = connectionDetails;
            IntellisenseMetrics = new InteractionMetrics<double>(new int[] { 50, 100, 200, 500, 1000, 2000 });
        }

        /// <summary>
        /// URI identifying the owner/user of the connection. Could be a file, service, resource, etc.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Properties used for creating/opening the SQL connection.
        /// </summary>
        public ConnectionDetails ConnectionDetails { get; set; }

        /// <summary>
        /// Intellisense interaction metrics for the connection.
        /// </summary>
        public InteractionMetrics<double> IntellisenseMetrics { get; private set; }

        /// <summary>
        /// Returns true if the connection is to a cloud (Azure) instance.
        /// </summary>
        public abstract bool IsCloud { get; set; }

        /// <summary>
        /// Tries to get the open <see cref="DbConnection"/> associated with the given connection type.
        /// </summary>
        /// <param name="connectionType">The connection type (e.g. "Query", "Default").</param>
        /// <param name="connection">The located connection, or <c>null</c> if none.</param>
        /// <returns>True if a connection of the given type was found; false otherwise.</returns>
        public abstract bool TryGetConnection(string connectionType, out DbConnection connection);

        /// <summary>
        /// Gets a unique key describing this connection, used to look up the binding context.
        /// </summary>
        public string ConnectionContextKey => GetConnectionContextKey(this.ConnectionDetails);

        /// <summary>
        /// Generate a unique key based on the ConnectionDetails object
        /// </summary>
        public static string GetConnectionContextKey(ConnectionDetails details)
        {
            string key = string.Format("{0}_{1}_{2}_{3}",
                details.ServerName ?? "NULL",
                details.DatabaseName ?? "NULL",
                details.UserName ?? "NULL",
                details.AuthenticationType ?? "NULL"
            );

            if (!string.IsNullOrEmpty(details.Id))
            {
                key += "_" + details.Id;
            }

            if (!string.IsNullOrEmpty(details.DatabaseDisplayName))
            {
                key += "_" + details.DatabaseDisplayName;
            }

            if (!string.IsNullOrEmpty(details.GroupId))
            {
                key += "_" + details.GroupId;
            }

            if (!string.IsNullOrEmpty(details.ConnectionName))
            {
                key += "_" + details.ConnectionName;
            }

            // Additional properties that are used to distinguish the connection (besides password)
            // These are so that multiple connections can connect to the same target, with different settings.
            foreach (KeyValuePair<string, object> entry in details.Options.OrderBy(entry => entry.Key))
            {
                // Filter out properties we already have or don't want (password)
                if (
                    // Exclude properties that are already used above
                    entry.Key != "server" &&
                    entry.Key != "database" &&
                    entry.Key != "user" &&
                    entry.Key != "authenticationType" &&
                    entry.Key != "databaseDisplayName" &&
                    // Exclude strictly-organizational properties that have no bearing on the connection
                    entry.Key != "connectionName" &&
                    entry.Key != "groupId" &&
                    // Exclude secrets/credentials that should never be logged or stored in plaintext
                    entry.Key != "password" &&
                    entry.Key != "azureAccountToken")
                {
                    // Boolean values are explicitly labeled true or false instead of undefined.
                    if (entry.Value is bool v)
                    {
                        if (v)
                        {
                            key += "_" + entry.Key + ":true";
                        }
                        else
                        {
                            key += "_" + entry.Key + ":false";
                        }
                    }
                    else if (!string.IsNullOrEmpty(entry.Value as String))
                    {
                        key += "_" + entry.Key + ":" + entry.Value;
                    }
                }
            }

#pragma warning disable SYSLIB0013 // we don't want to escape the ":" characters in our key-value options pairs because it's more readable
            return Uri.EscapeUriString(key);
#pragma warning restore SYSLIB0013
        }
    }
}
