# Using the JSON-RPC API

> NOTE: The [API Reference](../api/index.md) is the best starting point for working directly with
> the .NET API.

An example of using the JSON RPC API from a .Net Core console application is available at docs/samples/jsonrpc/netcore.  
The following snippet provides a basic example of how to connect to a database and execute a query.

```typescript
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
        QueryExecuteCompleteParams queryComplete = 
            await testHelper.RunQuery(queryTempFile.FilePath, query);

        if (queryComplete.BatchSummaries != null && queryComplete.BatchSummaries.Length > 0)
        {
            var batch = queryComplete.BatchSummaries[0];
            if (batch.ResultSetSummaries != null && batch.ResultSetSummaries.Length > 0)
            {
                var resultSet = batch.ResultSetSummaries[0];

                // retrive the results
                QueryExecuteSubsetResult querySubset = await testHelper.ExecuteSubset(
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
                        Console.Write(row.GetValue(i) + ", ");
                    }
                    Console.Write(Environment.NewLine);
                }
            }                    
        }

        // close database connection
        await testHelper.Disconnect(queryTempFile.FilePath);
    }
}
```
