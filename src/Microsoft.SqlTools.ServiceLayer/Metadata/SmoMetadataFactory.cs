//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    /// <summary>
    /// Interface for a factory that generates metadata for an object to edit
    /// </summary>
    public interface IMetadataFactory
    {
        /// <summary>
        /// Generates a edit-ready metadata object
        /// </summary>
        /// <param name="connection">Connection to use for getting metadata</param>
        /// <param name="objectName">Name of the object to return metadata for</param>
        /// <param name="objectType">Type of the object to return metadata for</param>
        /// <returns>Metadata about the object requested</returns>
        TableMetadata GetObjectMetadata(DbConnection connection, string schemaName, string objectName, string objectType);
    }

    /// <summary>
    /// Factory that generates metadata using a combination of SMO and SqlClient metadata
    /// </summary>
    public class SmoMetadataFactory : IMetadataFactory
    {
        /// <summary>
        /// Generates a edit-ready metadata object using SMO
        /// </summary>
        /// <param name="connection">Connection to use for getting metadata</param>
        /// <param name="objectName">Name of the object to return metadata for</param>
        /// <param name="objectType">Type of the object to return metadata for</param>
        /// <returns>Metadata about the object requested</returns>
        public TableMetadata GetObjectMetadata(DbConnection connection, string schemaName, string objectName, string objectType)
        {
            // Get a connection to the database for SMO purposes
            SqlConnection sqlConn = connection as SqlConnection;
            if (sqlConn == null)
            {
                // It's not actually a SqlConnection, so let's try a reliable SQL connection
                ReliableSqlConnection reliableConn = connection as ReliableSqlConnection;
                if (reliableConn == null)
                {
                    // If we don't have connection we can use with SMO, just give up on using SMO
                    return null;
                }

                // We have a reliable connection, use the underlying connection
                sqlConn = reliableConn.GetUnderlyingConnection();
            }

            // Connect with SMO and get the metadata for the table
            ServerConnection serverConnection;
            if (sqlConn.AccessToken == null)
            {
                serverConnection = new ServerConnection(sqlConn);
            }
            else
            {
                serverConnection = new ServerConnection(sqlConn, new AzureAccessToken(sqlConn.AccessToken));
            }
            Server server = new Server(serverConnection);
            Database database = server.Databases[sqlConn.Database];
            TableViewTableTypeBase smoResult;
            switch (objectType.ToLowerInvariant())
            {
                case "table":                    
                    Table table = string.IsNullOrEmpty(schemaName) ? new Table(database, objectName) : new Table(database, objectName, schemaName);
                    table.Refresh();
                    smoResult = table;
                    break;
                case "view":
                    View view = string.IsNullOrEmpty(schemaName) ? new View(database, objectName) : new View(database, objectName, schemaName);
                    view.Refresh();
                    smoResult = view;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(objectType), SR.EditDataUnsupportedObjectType(objectType));
            }
            if (smoResult == null)
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), SR.EditDataObjectMetadataNotFound);
            }

            // Generate the edit column metadata
            List<ColumnMetadata> editColumns = new List<ColumnMetadata>();
            for (int i = 0; i < smoResult.Columns.Count; i++)
            {
                Column smoColumn = smoResult.Columns[i];

                // The default value may be escaped
                string defaultValue = smoColumn.DefaultConstraint == null
                    ? null
                    : FromSqlScript.UnwrapLiteral(smoColumn.DefaultConstraint.Text);

                ColumnMetadata column = new ColumnMetadata
                {
                    DefaultValue = defaultValue,
                    EscapedName = ToSqlScript.FormatIdentifier(smoColumn.Name),
                    Ordinal = i
                };
                editColumns.Add(column);
            }

            // Only tables can be memory-optimized
            Table smoTable = smoResult as Table;
            bool isMemoryOptimized = smoTable != null && smoTable.IsMemoryOptimized;

            // Escape the parts of the name
            string[] objectNameParts = {smoResult.Schema, smoResult.Name};
            string escapedMultipartName = ToSqlScript.FormatMultipartIdentifier(objectNameParts);

            return new TableMetadata
            {
                Columns = editColumns.ToArray(),
                EscapedMultipartName = escapedMultipartName,
                IsMemoryOptimized = isMemoryOptimized,
            };
        }
    }
}
