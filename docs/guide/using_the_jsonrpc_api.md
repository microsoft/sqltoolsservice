# Using the SQL Tools JSON-RPC API
The SQL Tools JSON-RPC API is the best way to consume the services
functionality in SQL tools.  The JSON-RPC API available through stdio
of the SQL Tools Service process.

> NOTE: The [API Reference](../api/index.md) is the best starting point for working directly with
> the .NET API.

## Download SQL Tools Service binaries

To get started using the SQL Tools Service you'll need to install the service binaries.
Download the SQL Tools Service binaries from the 
[sqltoolsservice release page](https://github.com/Microsoft/sqltoolsservice/releases).  

Daily development builds will end with "-alpha".  Release builds will end with " Release".
For example, here is the [0.2.0 release](https://github.com/Microsoft/sqltoolsservice/releases/tag/v0.2.0).
It is also possible to build the SQL Tools Service directly from source.

## Query Execution Example using JSON-RPC API

An example of using the JSON RPC API from a .Net Core console application is available at docs/samples/jsonrpc/netcore.  
The following snippet provides a basic example of how to connect to a database and execute a query.

See the [full source code](https://github.com/Microsoft/sqltoolsservice/blob/dev/docs/samples/jsonrpc/netcore/executequery/Program.cs)
for this sample.

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
