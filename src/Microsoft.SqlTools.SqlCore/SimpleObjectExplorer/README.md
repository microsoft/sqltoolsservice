# Simple Object Explorer

The Simple Object Explorer is a lightweight implementation of SQL server object explorer. It takes in a simple SqlConnection and runs a single query to fetch the object tree. It is designed to be used in scenarios where the entire schema tree is needed in one go.

## Features
- Uses `Micrsosoft.Data.SqlClient` only
- Fetches the entire object tree in one go
- Executes a single query to fetch the object tree
- Supports SQL Server 2008 and above

## Architecture
The object explorer model is defined in [ObjectExplorerModel.cs](./ObjectExplorerModel.xml) Read the up-to-date documentation there to support new object types.

## Usage

The below code snippet shows how to use the Simple Object Explorer to fetch the object tree for a given connection. The returned object is of type [TreeNode](./TreeNode.cs) and represents the Database of the connection passed in. 

```csharp
var connection = new SqlConnection("Data Source=localhost;Integrated Security=true");
var root = await SimpleObjectExplorer.GetObjectExplorerModel(connection);
```

## Limitations
- Does not support server-level objects and connections
- The objects are grouped by schema and does not follow the SSMS style grouping.