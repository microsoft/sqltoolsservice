//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Peek Definition/ Go to definition implementation
    /// Script sql objects and write create scripts to file
    /// </summary>
    internal class PeekDefinition
    {
        private ServerConnection serverConnection;

        private Database database;

        private string tempPath;

        internal delegate StringCollection ScriptGetter(string objectName, string schemaName);

        // Dictionary that holds the script getter for each type
        private Dictionary<DeclarationType, ScriptGetter> sqlScriptGetters =
            new Dictionary<DeclarationType, ScriptGetter>();

        // Dictionary that holds the object name (as appears on the TSQL create statement)
        private Dictionary<DeclarationType, string> sqlObjectTypes = new Dictionary<DeclarationType, string>();

        /// <summary>
        /// Initialize a Peek Definition helper object
        /// </summary>
        /// <param name="serverConnection">SMO Server connection</param>
        internal PeekDefinition(ServerConnection serverConnection)
        {
            this.serverConnection = serverConnection;

            DirectoryInfo tempScriptDirectory = Directory.CreateDirectory(Path.GetTempPath() + "mssql_definition");
            this.tempPath = tempScriptDirectory.FullName;
            Initialize();
        }

        private Database Database
        {
            get
            {
                if (this.database == null)
                {
                    if (this.serverConnection != null && !string.IsNullOrEmpty(this.serverConnection.DatabaseName))
                    {
                        try
                        {
                            // Get server object from connection
                            SqlConnection sqlConn = new SqlConnection(this.serverConnection.ConnectionString);                    
                            sqlConn.Open();
                            ServerConnection peekConnection = new ServerConnection(sqlConn);
                            Server server = new Server(peekConnection);                        
                            this.database = new Database(server, peekConnection.DatabaseName);                        
                        }
                        catch(Exception ex)
                        {
                            Logger.Write(LogLevel.Error, "Exception at PeekDefinition Database.get() : " + ex.Message);
                        }   
                    }

                }
                
                return this.database;
            }
        }

        /// <summary>
        /// Add getters for each sql object supported by peek definition
        /// </summary>
        private void Initialize()
        {
            //Add script getters for each sql object

            //Add tables to supported types
            AddSupportedType(DeclarationType.Table, GetTableScripts, "Table");

            //Add views to supported types
            AddSupportedType(DeclarationType.View, GetViewScripts, "view");

            //Add stored procedures to supported types
            AddSupportedType(DeclarationType.StoredProcedure, GetStoredProcedureScripts, "Procedure");
        }

        /// <summary>
        /// Add the given type, scriptgetter and the typeName string to the respective dictionaries
        /// </summary>
        private void AddSupportedType(DeclarationType type, ScriptGetter scriptGetter, string typeName)
        {
            sqlScriptGetters.Add(type, scriptGetter);
            sqlObjectTypes.Add(type, typeName);

        }

        /// <summary>
        /// Convert a file to a location array containing a location object as expected by the extension
        /// </summary>
        internal Location[] GetLocationFromFile(string tempFileName, int lineNumber)
        {
            if (Path.DirectorySeparatorChar.Equals('/'))
            {
                tempFileName = "file:" + tempFileName;                 
            }
            else
            {
                tempFileName = new Uri(tempFileName).AbsoluteUri;
            }
            Location[] locations = new[] {
                    new Location {
                        Uri = tempFileName,
                        Range = new Range {
                            Start = new Position { Line = lineNumber, Character = 1},
                            End = new Position { Line = lineNumber + 1, Character = 1}
                        }
                    }
            };
            return locations;
        }

        /// <summary>
        /// Get line number for the create statement
        /// </summary>
        private int GetStartOfCreate(string script, string createString)
        {
            string[] lines = script.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                if (lines[lineNumber].IndexOf( createString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return lineNumber;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get the script of the selected token based on the type of the token
        /// </summary>
        /// <param name="declarationItems"></param>
        /// <param name="tokenText"></param>
        /// <param name="schemaName"></param>
        /// <returns>Location object of the script file</returns>
        internal Location[] GetScript(IEnumerable<Declaration> declarationItems, string tokenText, string schemaName)
        {
            foreach (Declaration declarationItem in declarationItems)
            {
                if (declarationItem.Title == null)
                {
                    continue;
                }

                if (declarationItem.Title.Equals(tokenText))
                {
                    // Script object using SMO based on type
                    DeclarationType type  = declarationItem.Type;
                    if (sqlScriptGetters.ContainsKey(type) && sqlObjectTypes.ContainsKey(type))
                    {
                        // On *nix and mac systems, the defaultSchema property throws an Exception when accessed.
                        // This workaround ensures that a schema name is present by attempting 
                        // to get the schema name from the declaration item 
                        // If all fails, the default schema name is assumed to be "dbo"
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && string.IsNullOrEmpty(schemaName))
                        {
                            string fullObjectName = declarationItem.DatabaseQualifiedName;
                            schemaName = this.GetSchemaFromDatabaseQualifiedName(fullObjectName, tokenText);
                        }
                        return GetSqlObjectDefinition(
                                    sqlScriptGetters[type],
                                    tokenText,
                                    schemaName,
                                    sqlObjectTypes[type]
                                );
                    }
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Return schema name from the full name of the database. If schema is missing return dbo as schema name.
        /// </summary>
        /// <param name="fullObjectName"> The full database qualified name(database.schema.object)</param>
        /// <param name="objectName"> Object name</param>
        /// <returns>Schema name</returns>
        internal string GetSchemaFromDatabaseQualifiedName(string fullObjectName, string objectName)
        {
            string[] tokens = fullObjectName.Split('.');
            for (int i = tokens.Length - 1; i > 0; i--)
            {
                if(tokens[i].Equals(objectName))
                {
                    return tokens[i-1];
                }
            }
            return "dbo";
        }

        /// <summary>
        /// Script a table using SMO
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="schemaName">Schema name</param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetTableScripts(string tableName, string schemaName)
        {
            try
            {
                Table table = string.IsNullOrEmpty(schemaName)
                    ? new Table(this.Database, tableName)
                    : new Table(this.Database, tableName, schemaName);

                table.Refresh();
                
                return table.Script();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Script a view using SMO
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="schemaName">Schema name </param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetViewScripts(string viewName, string schemaName)
        {
            try
            {
                View view = string.IsNullOrEmpty(schemaName)
                    ? new View(this.Database, viewName)
                    : new View(this.Database, viewName, schemaName);

                view.Refresh();
                
                return view.Script();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Script a stored procedure using SMO
        /// </summary>
        /// <param name="storedProcedureName">Stored Procedure name</param>
        /// <param name="schemaName">Schema Name</param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetStoredProcedureScripts(string sprocName, string schemaName)
        {
            try
            {
                StoredProcedure sproc = string.IsNullOrEmpty(schemaName)
                    ? new StoredProcedure(this.Database, sprocName)
                    : new StoredProcedure(this.Database, sprocName, schemaName);

                sproc.Refresh();
                
                return sproc.Script();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Script a object using SMO and write to a file.
        /// </summary>
        /// <param name="sqlScriptGetter">Function that returns the SMO scripts for an object</param>
        /// <param name="objectName">SQL object name</param>
        /// <param name="schemaName">Schema name or null</param>
        /// <param name="objectType">Type of SQL object</param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetSqlObjectDefinition(
                ScriptGetter sqlScriptGetter,
                string objectName,
                string schemaName,
                string objectType)
        {
                StringCollection scripts = sqlScriptGetter(objectName, schemaName);
                string tempFileName = (schemaName != null) ?  Path.Combine(this.tempPath, string.Format("{0}.{1}.sql", schemaName, objectName))
                                                    : Path.Combine(this.tempPath, string.Format("{0}.sql", objectName));

                if (scripts != null)
                {
                    int lineNumber = 0;
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {

                        foreach (string script in scripts)
                        {
                            string createSyntax = string.Format("CREATE {0}", objectType);
                            if (script.IndexOf(createSyntax, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                scriptFile.WriteLine(script);
                                lineNumber = GetStartOfCreate(script, createSyntax);
                            }
                        }
                    }
                    return GetLocationFromFile(tempFileName, lineNumber);
                }

            return null;
        }
    }
}