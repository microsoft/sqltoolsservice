//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
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
        private ConnectionInfo connectionInfo;
        private string tempPath;
        
        // Dictionary that holds the script getter for each type
        private Dictionary<DeclarationType, Func<string, string, StringCollection>> sqlScriptGetters =
            new Dictionary<DeclarationType, Func<string, string, StringCollection>>();

        //Dictionary that holds the object name(as appears on the TSQL create statement)
        private Dictionary<DeclarationType, string> sqlObjectTypes = new Dictionary<DeclarationType, string>();

        private Database database 
        {
            get
            {
                Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                return server.Databases[this.connectionInfo.SqlConnection.Database];
            }
        }
        
        internal PeekDefinition(ConnectionInfo connInfo)
        {
            connectionInfo = connInfo;
            DirectoryInfo tempScriptDirectory = Directory.CreateDirectory( Path.GetTempPath()+ "mssql_definition");
            tempPath = tempScriptDirectory.FullName;
            Initialize();
        }

        private void Initialize()
        {
            //Add script getters for each sql object

            //Add tables to supported types
            sqlScriptGetters.Add(DeclarationType.Table, GetTableScripts);
            sqlObjectTypes.Add(DeclarationType.Table, "Table");

            //Add views to supported types
            sqlScriptGetters.Add(DeclarationType.View, GetViewScripts);
            sqlObjectTypes.Add(DeclarationType.View, "View");

            //Add stored procedures to supported types
            sqlScriptGetters.Add(DeclarationType.StoredProcedure, GetStoredProcedureScripts);
            sqlObjectTypes.Add(DeclarationType.StoredProcedure, "Procedure");

        }

        /// <summary>
        /// Get the script of the selected token based on the type of the token
        /// </summary>
        /// <param name="declarationItems"></param>
        /// <param name="tokenText"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        internal Location[] GetScript(IEnumerable<Declaration> declarationItems, string tokenText, string schemaName)
        {
            foreach (Declaration declarationItem in declarationItems)
            {
                if (declarationItem.Title.Equals(tokenText))
                {
                    // Script object using SMO based on type
                    DeclarationType type  = declarationItem.Type;
                    if (sqlScriptGetters.ContainsKey(type))
                    {
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
        /// Script a table using SMO
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="schemaName">Schema name</param>
        /// <returns>String collection of scripts</returns>
        internal  StringCollection GetTableScripts(string tableName, string schemaName)
        {
            return (schemaName != null) ? database.Tables[tableName, schemaName]?.Script()
                    : database.Tables[tableName].Script();
        }

        /// <summary>
        /// Script a view using SMO
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="schemaName">Schema name </param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetViewScripts(string viewName, string schemaName)
        {
            return (schemaName != null) ? database.Views[viewName, schemaName]?.Script()
                    : database.Views[viewName]?.Script();
        }

        /// <summary>
        /// Script a stored procedure using SMO
        /// </summary>
        /// <param name="storedProcedureName">Stored Procedure name</param>
        /// <param name="schemaName">Schema Name</param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetStoredProcedureScripts(string viewName, string schemaName)
        {
            return (schemaName != null) ? database.StoredProcedures[viewName, schemaName]?.Script()
                    : database.StoredProcedures[viewName]?.Script();
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
                Func<string, string, StringCollection> sqlScriptGetter, 
                string objectName, 
                string schemaName, 
                string objectType) 
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                StringCollection scripts = sqlScriptGetter(objectName, schemaName);
                string tempFileName = (schemaName != null) ?  Path.Combine( tempPath, String.Format("{0}.{1}.sql", schemaName, objectName)) 
                                                    : Path.Combine( tempPath, String.Format("{0}.sql", objectName));

                if (scripts != null)
                {
                    int lineNumber = 0;
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        
                        foreach (string script in scripts)
                        {
                            if (script.IndexOf(String.Format("CREATE {0}", objectType), StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                scriptFile.WriteLine(script);
                                lineNumber = GetStartOfCreate(script, String.Format("CREATE {0}", objectType));
                            }                       
                        }         
                    }
                    return GetLocationFromFile(tempFileName, lineNumber);
                }
            }
            return null;
        }

        /// <summary>
        /// Convert a file to a location array containing a location object as expected by the extension
        /// </summary>
        private Location[] GetLocationFromFile(string tempFileName, int lineNumber)
        {
            Location[] locations = new[] { 
                    new Location {
                        Uri = new Uri(tempFileName).AbsoluteUri,
                        Range = new Range {
                            Start = new Position { Line = lineNumber, Character = 1},
                            End = new Position { Line = lineNumber+1, Character = 1}
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

    }
}