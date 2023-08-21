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
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;
using Microsoft.SqlTools.SqlCore.Connection;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Factory that generates metadata using a combination of SMO and SqlClient metadata
    /// </summary>
    public class SmoEditMetadataFactory : IEditMetadataFactory
    {
        /// <summary>
        /// Generates a edit-ready metadata object using SMO
        /// </summary>
        /// <param name="connection">Connection to use for getting metadata</param>
        /// <param name="objectNamedParts">Split and unwrapped name parts</param>
        /// <param name="objectType">Type of the object to return metadata for</param>
        /// <returns>Metadata about the object requested</returns>
        public EditTableMetadata GetObjectMetadata(DbConnection connection, string[] objectNamedParts, string objectType)
        {
            Validate.IsNotNull(nameof(objectNamedParts), objectNamedParts);
            if (objectNamedParts.Length <= 0)
            {
                throw new ArgumentNullException(nameof(objectNamedParts), SR.EditDataMetadataObjectNameRequired);
            }
            if (objectNamedParts.Length > 2)
            {
                throw new InvalidOperationException(SR.EditDataMetadataTooManyIdentifiers);
            }

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
            Database db = new Database(server, sqlConn.Database);

            TableViewTableTypeBase smoResult;
            switch (objectType.ToLowerInvariant())
            {
                case "table":
                    smoResult = objectNamedParts.Length == 1
                        ? new Table(db, objectNamedParts[0])                        // No schema provided
                        : new Table(db, objectNamedParts[1], objectNamedParts[0]);  // Schema provided
                    break;
                case "view":
                    smoResult = objectNamedParts.Length == 1
                        ? new View(db, objectNamedParts[0])                         // No schema provided
                        : new View(db, objectNamedParts[1], objectNamedParts[0]);   // Schema provided
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(objectType), SR.EditDataUnsupportedObjectType(objectType));
            }

            // A bug in SMO makes it necessary to call refresh to attain certain properties (such as IsMemoryOptimized)
            smoResult.Refresh();
            if (smoResult.State != SqlSmoState.Existing)
            {
                throw new ArgumentOutOfRangeException(nameof(objectNamedParts), SR.EditDataObjectNotFound);
            }

            // Filter out dropped ledger columns from the list of columns to be returned
            // and grab the specific column properties checked below
            smoResult.Columns.ClearAndInitialize("[(@IsDroppedLedgerColumn=0)]", new [] { nameof(Column.DataType), nameof(Column.DefaultConstraintName) });

            // Generate the edit column metadata
            List<EditColumnMetadata> editColumns = new List<EditColumnMetadata>();
            for (int i = 0; i < smoResult.Columns.Count; i++)
            {
                Column smoColumn = smoResult.Columns[i];

                string defaultValue = null;
                try
                {
                    // The default value may be escaped
                    defaultValue = string.IsNullOrEmpty(smoColumn.DefaultConstraintName)
                        ? null
                        : FromSqlScript.UnwrapLiteral(smoColumn.DefaultConstraint.Text);
                }
                catch (PropertyCannotBeRetrievedException)
                {
                    // This exception will be thrown when the user doesn't have view definition privilege,
                    // we can ignore it and use null as the default value; 
                }

                EditColumnMetadata column = new EditColumnMetadata
                {
                    DefaultValue = defaultValue,
                    EscapedName = ToSqlScript.FormatIdentifier(smoColumn.Name),
                    Ordinal = i,
                    IsHierarchyId = smoColumn.DataType.SqlDataType == SqlDataType.HierarchyId,
                };
                editColumns.Add(column);
            }

            // Only tables can be memory-optimized
            Table smoTable = smoResult as Table;
            bool isMemoryOptimized = false;
            // TODO: Remove IsSupported check once SMO fixes broken IsMemoryOptimized scenario (TFS #10871823)
            if (smoTable != null)
            {
                isMemoryOptimized = smoTable.IsSupportedProperty("IsMemoryOptimized") && smoTable.IsMemoryOptimized;
            }

            // Escape the parts of the name
            string[] objectNameParts = { smoResult.Schema, smoResult.Name };
            string escapedMultipartName = ToSqlScript.FormatMultipartIdentifier(objectNameParts);

            return new EditTableMetadata
            {
                Columns = editColumns.ToArray(),
                EscapedMultipartName = escapedMultipartName,
                IsMemoryOptimized = isMemoryOptimized
            };
        }
    }
}
