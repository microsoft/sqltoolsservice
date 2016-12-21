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

<img src='../images/connected_bind.png' width='800px' />

## Message Dispatcher

<img src='../images/msgdispatch.png' width='650px' />

<img src='../images/msgdispatchexample.png' width='800px' />

