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
    internal class PeekDefinition
    {
        private ConnectionInfo connectionInfo;
        private string tempPath;

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
        }

        internal Location[] GetScript(IEnumerable<Declaration> declarationItems, string tokenText, string schemaName)
        {
            foreach (Declaration declarationItem in declarationItems)
            {
                if (declarationItem.Title.Equals(tokenText))
                {
                    DeclarationType type  = declarationItem.Type;
                    // Script object using SMO based on type
                    switch (type)
                    {
                        case DeclarationType.Table:
                            return this.GetTableDefinition(tokenText, schemaName);
                        case DeclarationType.View:
                            return this.GetViewDefinition(tokenText, schemaName);
                        case DeclarationType.StoredProcedure:
                            return this.GetStoredProcedureDefinition(tokenText, schemaName);
                        default:
                            return null;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Script a table using SMO and write to a file.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetTableDefinition(string tableName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                Table table = database.Tables[tableName];
                string tempFileName = (schemaName != null) ? Path.Combine( tempPath, String.Format("{0}.{1}.sql", schemaName, tableName)) 
                                                : Path.Combine( tempPath, String.Format("{0}.sql", tableName)); 

                if (table != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = table.Script();
                        foreach (string script in scripts)
                        {
                            if (script.IndexOf( "CREATE TABLE", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                scriptFile.WriteLine(script);
                            }  
                        }

                        
                    }
                    return GetLocationFromFile(tempFileName, 0);
                }
            }
            return null;
        }

        /// <summary>
        /// Script a view using SMO and write to a file.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="schemaName">Schema name </param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetViewDefinition(string viewName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                View view = (schemaName != null) ? database.Views[viewName, schemaName] : database.Views[viewName];
                string tempFileName = (schemaName != null) ? Path.Combine( tempPath, String.Format("{0}.{1}.sql", schemaName, viewName))
                                                    :  Path.Combine( tempPath, String.Format("{0}.sql", viewName));

                if (view != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = view.Script();
                        foreach (string script in scripts)
                        {
                            if (script.IndexOf( "CREATE VIEW", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                scriptFile.WriteLine(script);
                            }  
                        }
                        
                    }
                    return GetLocationFromFile(tempFileName, 0);
                }
            }
            return null;
        }

        /// <summary>
        /// Script a stored procedure using SMO and write to a file.
        /// </summary>
        /// <param name="storedProcedureName">Stored Procedure name</param>
        /// <param name="schemaName">Schema Name</param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetStoredProcedureDefinition(string storedProcedureName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                StoredProcedure storedProcedure = (schemaName != null) ? database.StoredProcedures[storedProcedureName, schemaName] :
                                                    database.StoredProcedures[storedProcedureName];
                string tempFileName = (schemaName != null) ?  Path.Combine( tempPath, String.Format("{0}.{1}.sql", tempPath, schemaName, storedProcedureName)) 
                                                    : Path.Combine( tempPath, String.Format("{0}.sql", tempPath, storedProcedureName));

                if (storedProcedure != null)
                {
                    int lineNumber = 0;
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = storedProcedure.Script();
                        foreach (string script in scripts)
                        {
                            if (script.IndexOf( "CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                scriptFile.WriteLine(script);
                                lineNumber = GetStartOfCreate(script, "CREATE PROCEDURE");
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
                if ( lines[lineNumber].IndexOf( createString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return lineNumber;
                }
            }
            return 0;
        }

    }
}