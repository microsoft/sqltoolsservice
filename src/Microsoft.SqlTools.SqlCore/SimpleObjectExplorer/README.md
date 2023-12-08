# Simple Object Explorer

The Simple Object Explorer is a lightweight implementation of the SQL Server Object Explorer. It accepts a SqlConnection and executes a single query to retrieve the entire object tree. It is designed for performance-critical scenarios where the traditional SMO-based object explorer is too slow. Additionally, it is intended for use in scenarios where the entire schema tree is required in one go. Furthermore, it is designed to be employed in situations where not all the sys tables are accessible or present, which are required by SMO to function correctly.

## Features
- Uses `Micrsosoft.Data.SqlClient` only
- Fetches the entire object tree in one go
- Executes a single query to fetch the object tree
- Supports SQL Server 2008 and above

## Architecture
The object explorer model is defined in [ObjectExplorerModel.cs](./ObjectExplorerModel.xml). Read the up-to-date documentation there to support new object types.

## Usage

The below code snippet shows how to use the Simple Object Explorer to fetch the object tree for a given connection. The returned object is of type [TreeNode](./TreeNode.cs) and represents the Database of the connection passed in. 

```csharp
var connection = new SqlConnection("Data Source=localhost;Integrated Security=true");
var root = await SimpleObjectExplorer.GetObjectExplorerModel(connection);
```

## Limitations
- Does not support server-level objects and connections
- The objects are grouped by schema and does not follow the SSMS style grouping.