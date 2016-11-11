//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.IO;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    public class PeekDefinition
    {
        private ConnectionInfo connectionInfo;
        private string tempPath;
        public PeekDefinition(ConnectionInfo connInfo)
        {
            this.connectionInfo = connInfo;
            tempPath = Path.GetTempPath();

        }
        public string GetTableDefinition(string tableName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                string databaseName = this.connectionInfo.SqlConnection.Database;
                Scripter scripter = new Scripter(server);
                Database database = server.Databases[databaseName];
                Table table = database.Tables[tableName];
                string tempFileName = tempPath + tableName + ".sql";

                if (table != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = table.Script();
                        foreach (string scr in scripts)
                        {
                            scriptFile.WriteLine(scr);
                        }

                        
                    }
                    return tempFileName;
                }
            }
            return null;
        }

        public string GetViewDefinition(string viewName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                string databaseName = this.connectionInfo.SqlConnection.Database;
                Scripter scripter = new Scripter(server);
                Database database = server.Databases[databaseName];
                View view = database.Views[viewName, schemaName];
                string tempFileName = tempPath + schemaName + "." + viewName + ".sql";
                if (view != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = view.Script();
                        foreach (string scr in scripts)
                        {
                            scriptFile.WriteLine(scr);
                        }
                        
                    }
                    return tempFileName;
                }
                else
                {
                    using (StreamWriter testFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        foreach (View vi in database.Views)
                        {
                            StringCollection scripts = vi.Script();
                            testFile.WriteLine(vi.Name);
                            foreach (string scr in scripts)
                            {
                                //testFile.WriteLine(scr);
                            }
                        }

                    }
                    return tempFileName;

                }
            }
            return null;
        }
    }
}