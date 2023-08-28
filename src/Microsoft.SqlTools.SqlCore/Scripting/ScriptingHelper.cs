//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.Scripting
{
    /// <summary>
    /// Helper class for scripting
    /// </summary>
    public static class ScriptingHelper
    {
        internal static string SelectAllValuesFromTransmissionQueue(Urn urn)
        {
            string script = string.Empty;
            StringBuilder selectQuery = new StringBuilder();

            /*
             SELECT TOP *, casted_message_body =
              CASE MESSAGE_TYPE_NAME WHEN 'X'
                 THEN CAST(MESSAGE_BODY AS NVARCHAR(MAX))
                 ELSE MESSAGE_BODY
              END
              FROM [new].[sys].[transmission_queue]
             */
            selectQuery.Append("SELECT TOP (1000) ");
            selectQuery.Append("*, casted_message_body = \r\nCASE message_type_name WHEN 'X' \r\n  THEN CAST(message_body AS NVARCHAR(MAX)) \r\n  ELSE message_body \r\nEND \r\n");

            // from clause
            selectQuery.Append("FROM ");
            Urn dbUrn = urn;

            // database
            while (dbUrn.Parent != null && dbUrn.Type != "Database")
            {
                dbUrn = dbUrn.Parent;
            }
            selectQuery.AppendFormat("{0}{1}{2}",
                            ScriptingGlobals.LeftDelimiter,
                            QuoteObjectName(dbUrn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                            ScriptingGlobals.RightDelimiter);
            //SYS
            selectQuery.AppendFormat(".{0}sys{1}",
                                     ScriptingGlobals.LeftDelimiter,
                                     ScriptingGlobals.RightDelimiter);
            //TRANSMISSION QUEUE
            selectQuery.AppendFormat(".{0}transmission_queue{1}",
                                      ScriptingGlobals.LeftDelimiter,
                                      ScriptingGlobals.RightDelimiter);

            script = selectQuery.ToString();
            return script;
        }

        internal static string SelectAllValues(Urn urn)
        {
            string script = string.Empty;
            StringBuilder selectQuery = new StringBuilder();
            selectQuery.Append("SELECT TOP (1000) ");
            selectQuery.Append("*, casted_message_body = \r\nCASE message_type_name WHEN 'X' \r\n  THEN CAST(message_body AS NVARCHAR(MAX)) \r\n  ELSE message_body \r\nEND \r\n");

            // from clause
            selectQuery.Append("FROM ");
            Urn dbUrn = urn;

            // database
            while (dbUrn.Parent != null && dbUrn.Type != "Database")
            {
                dbUrn = dbUrn.Parent;
            }
            selectQuery.AppendFormat("{0}{1}{2}",
                ScriptingGlobals.LeftDelimiter,
                QuoteObjectName(dbUrn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                ScriptingGlobals.RightDelimiter);
            // schema
            selectQuery.AppendFormat(".{0}{1}{2}",
                ScriptingGlobals.LeftDelimiter,
                QuoteObjectName(urn.GetAttribute("Schema"), ScriptingGlobals.RightDelimiter),
                ScriptingGlobals.RightDelimiter);
            // object
            selectQuery.AppendFormat(".{0}{1}{2}",
                ScriptingGlobals.LeftDelimiter,
                QuoteObjectName(urn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                ScriptingGlobals.RightDelimiter);

            //Adding no lock in the end.
            selectQuery.AppendFormat(" WITH(NOLOCK)");

            script = selectQuery.ToString();
            return script;
        }

        internal static class ScriptingGlobals
        {
            /// <summary>
            /// Left delimiter for an named object
            /// </summary>
            public const char LeftDelimiter = '[';

            /// <summary>
            /// right delimiter for a named object
            /// </summary>
            public const char RightDelimiter = ']';
        }



        static DataTable GetColumnNames(Server server, Urn urn, bool isDw)
        {
            List<string> filterExpressions = new List<string>();
            if (server.Version.Major >= 10)
            {
                // We don't have to include sparce columns as all the sparce columns data.
                // Can be obtain from column set columns.
                filterExpressions.Add("@IsSparse=0");
            }

            // Check if we're called for EDIT for SQL2016+/Sterling+.
            // We need to omit temporal columns if such are present on this table.
            if (server.Version.Major >= 13 || (DatabaseEngineType.SqlAzureDatabase == server.DatabaseEngineType && server.Version.Major >= 12 && !isDw))
            {
                // We're called in order to generate a list of columns for EDIT TOP N rows.
                // Don't return auto-generated, auto-populated, read-only temporal columns.
                filterExpressions.Add("@GeneratedAlwaysType=0");
            }

            // Check if we're called for EDIT for SQL2022+/Sterling+.
            // We need to omit dropped ledger columns if such are present
            if (server.Version.Major >= 16 || (DatabaseEngineType.SqlAzureDatabase == server.DatabaseEngineType && server.Version.Major >= 12 && !isDw))
            {
                filterExpressions.Add("@IsDroppedLedgerColumn=0");
            }

            // Check if we're called for SQL2017/Sterling+.
            // We need to omit graph internal columns if such are present on this table.
            if (server.Version.Major >= 14 || (DatabaseEngineType.SqlAzureDatabase == server.DatabaseEngineType && !isDw))
            {
                // from Smo.GraphType:
                // 0 = None
                // 1 = GraphId
                // 2 = GraphIdComputed
                // 3 = GraphFromId
                // 4 = GraphFromObjId
                // 5 = GraphFromIdComputed
                // 6 = GraphToId
                // 7 = GraphToObjId
                // 8 = GraphToIdComputed
                //
                // We only want to show types 0, 2, 5, and 8:
                filterExpressions.Add("(@GraphType=0 or @GraphType=2 or @GraphType=5 or @GraphType=8)");
            }

            Request request = new Request();
            // If we have any filters on the columns, add them.
            if (filterExpressions.Count > 0)
            {
                request.Urn = String.Format("{0}/Column[{1}]", urn.ToString(), string.Join(" and ", filterExpressions.ToArray()));
            }
            else
            {
                request.Urn = String.Format("{0}/Column", urn.ToString());
            }

            request.Fields = new String[] { "Name" };

            // get the columns in the order they were created
            OrderBy order = new OrderBy();
            order.Dir = OrderBy.Direction.Asc;
            order.Field = "ID";
            request.OrderByList = new OrderBy[] { order };

            Enumerator en = new Enumerator();

            // perform the query.
            DataTable? dt = null;
            EnumResult result = en.Process(server.ConnectionContext, request);

            if (result.Type == ResultType.DataTable)
            {
                dt = result;
            }
            else
            {
                dt = ((DataSet)result).Tables[0];
            }
            return dt;
        }

        public static string SelectFromTableOrView(Server server, Urn urn, bool isDw)
        {
            DataTable dt = GetColumnNames(server, urn, isDw);
            StringBuilder selectQuery = new StringBuilder();

            // build the first line
            if (dt != null && dt.Rows.Count > 0)
            {

                selectQuery.Append("SELECT TOP (1000) ");

                // first column
                selectQuery.AppendFormat("{0}{1}{2}\r\n",
                                         ScriptingGlobals.LeftDelimiter,
                                         QuoteObjectName(dt.Rows[0][0] as string, ScriptingGlobals.RightDelimiter),
                                         ScriptingGlobals.RightDelimiter);
                // add all other columns on separate lines. Make the names align.
                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    selectQuery.AppendFormat("      ,{0}{1}{2}\r\n",
                                             ScriptingGlobals.LeftDelimiter,
                                             QuoteObjectName(dt.Rows[i][0] as string, ScriptingGlobals.RightDelimiter),
                                             ScriptingGlobals.RightDelimiter);
                }
            }
            else
            {
                selectQuery.Append("SELECT TOP (1000) * ");
            }

            // from clause
            selectQuery.Append("  FROM ");

            if (server.ServerType != DatabaseEngineType.SqlAzureDatabase)
            {
                // Azure doesn't allow qualifying object names with the DB, so only add it on if we're not in Azure database URN
                Urn dbUrn = urn.Parent;
                selectQuery.AppendFormat("{0}{1}{2}.",
                                     ScriptingGlobals.LeftDelimiter,
                                     QuoteObjectName(dbUrn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                                     ScriptingGlobals.RightDelimiter);
            }

            // schema
            selectQuery.AppendFormat("{0}{1}{2}.",
                                     ScriptingGlobals.LeftDelimiter,
                                     QuoteObjectName(urn.GetAttribute("Schema"), ScriptingGlobals.RightDelimiter),
                                     ScriptingGlobals.RightDelimiter);
            // object
            selectQuery.AppendFormat("{0}{1}{2}",
                                     ScriptingGlobals.LeftDelimiter,
                                     QuoteObjectName(urn.GetAttribute("Name"), ScriptingGlobals.RightDelimiter),
                                     ScriptingGlobals.RightDelimiter);

            // In Hekaton M5, if it's a memory optimized table, we need to provide SNAPSHOT hint for SELECT.
            if (urn.Type.Equals("Table") && IsXTPSupportedOnServer(server))
            {
                try
                {
                    Table table = (Table)server.GetSmoObject(urn);
                    table.Refresh();
                    if (table.IsMemoryOptimized)
                    {
                        selectQuery.Append(" WITH (SNAPSHOT)");
                    }
                }
                catch (Exception ex)
                {
                    // log any exceptions determining if InMemory, but don't treat as fatal exception
                    Logger.Error("Could not determine if is InMemory table " + ex.ToString());
                }
            }

            return selectQuery.ToString();
        }

        /// <summary>
        /// Quote the name of a given sql object.
        /// </summary>
        /// <param name="sqlObject">object</param>
        /// <returns>quoted object name</returns>
        internal static string QuoteObjectName(string sqlObject)
        {
            return QuoteObjectName(sqlObject, ']');
        }

        /// <summary>
        /// Quotes the name of a given sql object
        /// </summary>
        /// <param name="sqlObject">object</param>
        /// <param name="quote">quote to use</param>
        /// <returns></returns>
        internal static string QuoteObjectName(string sqlObject, char quote)
        {
            int len = sqlObject.Length;
            StringBuilder result = new StringBuilder(sqlObject.Length);
            for (int i = 0; i < len; i++)
            {
                if (sqlObject[i] == quote)
                {
                    result.Append(quote);
                }
                result.Append(sqlObject[i]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Returns the value whether the server supports XTP or not s
        internal static bool IsXTPSupportedOnServer(Server server)
        {
            bool isXTPSupported = false;
            if (server.ConnectionContext.ExecuteScalar("SELECT SERVERPROPERTY('IsXTPSupported')") != DBNull.Value)
            {
                isXTPSupported = server.IsXTPSupported;
            }
            return isXTPSupported;
        }

    }
}