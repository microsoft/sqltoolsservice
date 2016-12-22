# Design and Implementation

The SQL Tools Service is designed as a stand-alone collection of database management operations.  The service can be integrated into a 
variety SQL data developer and DBA host applications.  For example, the below image illustrates the high-level MSSQL for VS Code 
composition.  The extension's UI components run in the VS Code process.  This code is written in TypeScipt and communicates with 
the service process over a JSON-RPC stdio channel.

The service process consists of SMO SDK for .Net Core and higher-level SQL management components.  The service provides features such as
common language service operations (IntelliSense auto-complete suggestions, peek definition, SQL error diagnostics, quickinfo hovers), connection management,
and query execution.

<img src='../images/hostprocess.png' width='800px' />

The SQL Tools Service is built on top of the SMO SDK for .Net Core and System.Data components built into .Net Core.  This allows the service
to provide rich SQL Server support that runs on Windows, Mac OS, and Linux operating systems.  The service can work with on-prem SQL Server,
SQL DB, and Azure SQL DW instances.

## Language Service
The SQL Tools Service implements the [language service protocol](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md) 
implemented by VS Code.  This includes support for the following operations.

* Autocomplete suggestions
* Peek\Go to definition
* SQL error checking diagnostics
* QuickInfo hover tooltips
* Function signature info

The language service is primarily implemented in @Microsoft.SqlTools.ServiceLayer.LanguageServices
 ([src link](https://github.com/Microsoft/sqltoolsservice/blob/dev/src/Microsoft.SqlTools.ServiceLayer/LanguageServices/LanguageService.cs)).
The language service depends heavily on the Microsoft.SqlServer.SqlParser assembly.

The language service component shares database connections across editor windows.  Operations are routed
through a queue to control concurrency when accessing shared database connection and metadata resources.  
The binding queue is implemented in @Microsoft.SqlTools.ServiceLayer.LanguageServices.ConnectedBindingQueue
 ([src link](https://github.com/Microsoft/sqltoolsservice/blob/dev/src/Microsoft.SqlTools.ServiceLayer/LanguageServices/ConnectedBindingQueue.cs) and 
  [src link](https://github.com/Microsoft/sqltoolsservice/blob/dev/src/Microsoft.SqlTools.ServiceLayer/LanguageServices/BindingQueue.cs)).
  
<img src='../images/connected_bind.png' width='800px' />

## Message Dispatcher

The JSON-RPC mechanism is build on a message dispatcher.  Messages are read from stdio and serialized\deserialized
using JSON.Net.  Message handlers are registered with the dispatcher.  Ass the messages are processed by
the dispatcher queue they are routed to any registered handlers.  The dispatch queue processes messages 
serially.

<img src='../images/msgdispatch.png' width='650px' />

The below sequence diagram show an example of how the language service may process a error checking diagnostics
workflow.  In this example the editor hosts a language service client that responds to user initiated editing events.
The user actions are translated into a sequence of request\response pairs and one-way event notifications. 
Messages can be initiated from either the server or the client.

<img src='../images/msgdispatchexample.png' width='800px' />

## Query Execution
The Query Execution component provides the ability to execute SQL scripts against SQL Server instances.
This functionality builds onto the support in System.Data to provide features batch processing support 
and a file-backed cache for large resultsets.

@Microsoft.SqlTools.ServiceLayer.QueryExecution.QueryExecutionService
([src link](https://github.com/Microsoft/sqltoolsservice/blob/dev/src/Microsoft.SqlTools.ServiceLayer/QueryExecution/QueryExecutionService.cs)) 
is the class that implements the query execution protocol.

