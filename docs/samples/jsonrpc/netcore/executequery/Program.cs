//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.JsonRpc.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.JsonRpc.ExecuteQuery
{
    internal class Program
    {        
        internal static void Main(string[] args)
        {
            // set SQLTOOLSSERVICE_EXE to location of SQL Tools Service executable
            Environment.SetEnvironmentVariable("SQLTOOLSSERVICE_EXE", @"D:\xplat\sqltoolsservice\src\Microsoft.SqlTools.ServiceLayer\bin\Debug\netcoreapp1.0\win7-x64\Microsoft.SqlTools.ServiceLayer.exe");

            // execute a query against localhost, master, with IntegratedAuth
            ExecuteQuery("SELECT * FROM sys.objects").Wait();
        }

        internal static async Task ExecuteQuery(string query)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (ClientHelper testHelper = new ClientHelper())
            {
                ConnectParams connectParams = new ConnectParams();
                connectParams.Connection = new ConnectionDetails();
                connectParams.Connection.ServerName = "localhost";
                connectParams.Connection.DatabaseName = "master";
                connectParams.Connection.AuthenticationType = "Integrated";

                await testHelper.Connect(queryTempFile.FilePath, connectParams);

                QueryExecuteCompleteParams queryComplete = await testHelper.RunQuery(queryTempFile.FilePath, query);
                if (queryComplete.BatchSummaries != null && queryComplete.BatchSummaries.Length > 0)
                {
                    var batch = queryComplete.BatchSummaries[0];
                    if (batch.ResultSetSummaries != null && batch.ResultSetSummaries.Length > 0)
                    {
                        var resultSet = batch.ResultSetSummaries[0];

                        QueryExecuteSubsetResult querySubset = await testHelper.ExecuteSubset(
                            queryTempFile.FilePath, batch.Id, resultSet.Id, 0, (int)resultSet.RowCount);

                        foreach (var column in resultSet.ColumnInfo)
                        {
                            Console.Write(column.ColumnName + ", ");
                        }

                        foreach (var row in querySubset.ResultSubset.Rows)
                        {
                            for (int i = 0; i < resultSet.ColumnInfo.Length; ++i)
                            {
                                Console.Write(row.GetValue(i) + ", ");
                            }
                        }
                    }                    
                }

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        } 
    }
}
