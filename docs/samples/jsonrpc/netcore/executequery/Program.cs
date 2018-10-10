//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.JsonRpc.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;

namespace Microsoft.SqlTools.JsonRpc.ExecuteQuery
{
    /// <summary>
    /// Simple JSON-RPC API sample to connect to a database, execute a query, and print the results
    /// </summary>
    internal class Program
    {        
        internal static void Main(string[] args)
        {
            // set SQLTOOLSSERVICE_EXE to location of SQL Tools Service executable
            Environment.SetEnvironmentVariable("SQLTOOLSSERVICE_EXE", @"MicrosoftSqlToolsServiceLayer.exe");

            // execute a query against localhost, master, with IntegratedAuth
            ExecuteQuery("SELECT * FROM sys.objects").Wait();

        }
        
        internal static async Task ExecuteQuery(string query)
        {    
            // create a temporary "workspace" file
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            // create the client helper which wraps the client driver objects
            using (ClientHelper testHelper = new ClientHelper())
            {
                // connnection details
                ConnectParams connectParams = new ConnectParams();
                connectParams.Connection = new ConnectionDetails();
                connectParams.Connection.ServerName = "localhost";
                connectParams.Connection.DatabaseName = "master";
                connectParams.Connection.AuthenticationType = "Integrated";

                // connect to the database
                await testHelper.Connect(queryTempFile.FilePath, connectParams);

                // execute the query
                QueryCompleteParams queryComplete = 
                    await testHelper.RunQuery(queryTempFile.FilePath, query);

                if (queryComplete.BatchSummaries != null && queryComplete.BatchSummaries.Length > 0)
                {
                    var batch = queryComplete.BatchSummaries[0];
                    if (batch.ResultSetSummaries != null && batch.ResultSetSummaries.Length > 0)
                    {
                        var resultSet = batch.ResultSetSummaries[0];

                        // retrive the results
                        SubsetResult querySubset = await testHelper.ExecuteSubset(
                            queryTempFile.FilePath, batch.Id, 
                            resultSet.Id, 0, (int)resultSet.RowCount);

                        // print the header
                        foreach (var column in resultSet.ColumnInfo)
                        {
                            Console.Write(column.ColumnName + ", ");
                        }
                        Console.Write(Environment.NewLine);

                        // print the rows
                        foreach (var row in querySubset.ResultSubset.Rows)
                        {
                            for (int i = 0; i < resultSet.ColumnInfo.Length; ++i)
                            {
                                Console.Write(row[i].DisplayValue + ", ");
                            }
                            Console.Write(Environment.NewLine);
                        }
                    }                    
                }

                // close database connection
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }        
    }
}
