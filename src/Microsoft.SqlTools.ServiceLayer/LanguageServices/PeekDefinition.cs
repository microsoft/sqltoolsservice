//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    public class PeekDefinition
    {
        private ConnectionInfo connectionInfo;
        private string tempPath;
        public PeekDefinition(ConnectionInfo connInfo)
        {
            connectionInfo = connInfo;
            tempPath = Path.GetTempPath();
        }

        public Location[] GetTableDefinition(string tableName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                Database database = server.Databases[this.connectionInfo.SqlConnection.Database];
                Table table = database.Tables[tableName];
                string tempFileName = tempPath + tableName + ".sql";

                if (table != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = table.Script();
                        foreach (string script in scripts)
                        {
                            if (script.Contains("CREATE TABLE"))
                            {
                                scriptFile.WriteLine(script);
                            }  
                        }

                        
                    }
                    return GetLocationFromFile(tempFileName);
                }
            }
            return null;
        }

        public Location[] GetViewDefinition(string viewName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                Database database = server.Databases[this.connectionInfo.SqlConnection.Database];
                View view = (schemaName != null) ? database.Views[viewName, schemaName] : database.Views[viewName];
                string tempFileName = tempPath + schemaName + "." + viewName + ".sql";

                if (view != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = view.Script();
                        foreach (string script in scripts)
                        {
                            if (script.Contains("CREATE VIEW"))
                            {
                                scriptFile.WriteLine(script);
                            }  
                        }
                        
                    }
                    return GetLocationFromFile(tempFileName);
                }
            }
            return null;
        }

        public Location[] GetStoredProcedureDefinition(string storedProcedureName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                Database database = server.Databases[this.connectionInfo.SqlConnection.Database];
                StoredProcedure storedProcedure = (schemaName != null) ? database.StoredProcedures[storedProcedureName, schemaName] :
                                                    database.StoredProcedures[storedProcedureName];
                string tempFileName = tempPath + schemaName + "." + storedProcedureName + ".sql";

                if (storedProcedure != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = storedProcedure.Script();
                        foreach (string script in scripts)
                        {
                            if (script.Contains("CREATE PROCEDURE"))
                            {
                                scriptFile.WriteLine(script);
                            }                       
                        }         
                    }
                    return GetLocationFromFile(tempFileName);
                }
            }
            return null;
        }

        public Location[] GetLocationFromFile(string tempFileName)
        {
            Location[] locations = new[] { 
                    new Location {
                        // Uri = "file:///c%3A/Users/shravind/AppData/Local/Temp/script.sql",
                        Uri = new Uri(tempFileName).AbsoluteUri,
                        // TODO: change line range to start of create
                        Range = new Range {
                            Start = new Position{ Line = 2, Character = 1},
                            End = new Position{ Line = 3, Character = 1}
                        }
                    }
                };
                return locations;
        }

    }
}