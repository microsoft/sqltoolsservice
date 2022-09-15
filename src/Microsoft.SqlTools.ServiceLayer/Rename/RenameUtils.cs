//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;

namespace Microsoft.SqlTools.ServiceLayer.Rename
{
    /// <summary>
    /// Helper class for Rename Service
    /// </summary>
    public static class RenameUtils
    {
        private static readonly string UrnPrefix = "Server[@Name='{0}']/Database[@Name='{1}']/Table[@Name='{2}' and @Schema='{3}']";
        private static readonly string RegexMatchPatternValidObjectName = @"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$";
        /// <summary>
        /// Method to validate all the parameters for the renaming operation
        /// </summary>
        /// <param name="requestParams">parameters which should be checked</param>
        public static void Validate(ProcessRenameEditRequestParams requestParams)
        {
            if (requestParams == null)
            {
                throw new ArgumentNullException();
            }
            if (String.IsNullOrEmpty(requestParams.TableInfo.TableName) || String.IsNullOrEmpty(requestParams.TableInfo.Database) || String.IsNullOrEmpty(requestParams.TableInfo.Schema) || String.IsNullOrEmpty(requestParams.TableInfo.OldName) || String.IsNullOrEmpty(requestParams.ChangeInfo.NewName))
            {
                throw new ArgumentException(SR.RenameRequestParametersNotNullOrEmpty);
            }
        }
        /// <summary>
        /// Method to get the sqlobject, which should be renamed
        /// </summary>
        /// <param name="requestParams">parameters which are required for the rename operation</param>
        /// <param name="connection">the sqlconnection on the server to search for the sqlobject</param>
        /// <returns>the sqlobject if implements the interface IRenamable, so they can be renamed</returns>
        public static IRenamable GetSQLRenameObject(ProcessRenameEditRequestParams requestParams, SqlConnection connection)
        {
            ServerConnection serverConnection = new ServerConnection(connection);
            Server server = new Server(serverConnection);

            Database database = new Database(server, requestParams.TableInfo.Database);
            SqlSmoObject dbObject = database.Parent.GetSmoObject(new Urn(GetURNFromDatabaseSqlObjects(requestParams, serverConnection.TrueName)));

            return (IRenamable)dbObject;
        }
        /// <summary>
        /// Method to generate the Uniform Resource Name (URN) of a sqlobject 
        /// </summary>
        /// <param name="requestParams">parameters which are required for the rename operation</param>
        /// <param name="trueNameOfServer">return the name of the server (@@Servername)</param>
        /// <returns>URN string of the sqlobject</returns>
        public static string GetURNFromDatabaseSqlObjects(ProcessRenameEditRequestParams requestParams, string trueNameOfServer)
        {
            String urnTableReplaced = String.Format(RenameUtils.UrnPrefix, trueNameOfServer, requestParams.TableInfo.Database, requestParams.TableInfo.TableName, requestParams.TableInfo.Schema, requestParams.TableInfo.OldName);

            if (requestParams.ChangeInfo.Type == ChangeType.COLUMN)
            {
                return String.Format(urnTableReplaced + "/Column[@Name='{0}']", requestParams.TableInfo.OldName);
            }
            return urnTableReplaced;
        }
    }
}