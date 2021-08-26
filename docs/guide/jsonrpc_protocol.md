# SQL Tools JSON-RPC Protocol

The SQL Tools JSON-RPC API provides a host-agnostic interface for the SQL Tools Service functionality.
The API provides easily consumable operations that allow simple integration into tools applications.

## Launching the Host Process

From your host process, launch `Microsoft.SqlTools.ServiceLayer(.exe)` using an host-native process
API that allows you to read and write this process' standard in/out streams.  All communication
with the host process occurs via this channel.

It is recommended that the process I/O be dealt with as a byte stream rather than read as a
string since different parts of the message format could be sent with different text encodings
(see next section).

It is expected that an editor will launch one instance of the host process for each SQL
'workspace' that the user has opened.  Generally this would map to a single top-level folder
which contains all of the user's SQL script files for a given project.

## Messages Overview

The SQL Tools Service implements portions of the
[language service protocol](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md)
defined by VS Code.  Some portions of this protocol reference have been duplicated here for
convenience.

It additionally implements several other API to provide database management
services such as connection management or query execution.

This document provides the protocol specification for all the service's JSON-RPC APIs and some useful events.


### Connection Management

* :leftwards_arrow_with_hook: [connection/connect](#connection_connect)
* :leftwards_arrow_with_hook: [connection/cancelconnect](#connect_cancelconnect)
* :arrow_right: [connection/disconnect](#connection_disconnect)
* :arrow_right: [connection/listdatabases](#connection_listdatabases)
* :arrow_right: [connection/changedatabase](#connection_changedatabase)
* :arrow_right: [connection/getconnectionstring](#connection_getconnectionstring)
* :arrow_right: [connection/buildconnectioninfo](#connection_buildconnectioninfo)
* :arrow_left: [connection/connectionchanged](#connection_connectionchanged)
* :arrow_left: [connection/complete](#connection_complete)


### Query Execution
* :leftwards_arrow_with_hook: [query/executeString](#query_executeString)
* :leftwards_arrow_with_hook: [query/executeDocumentSelection](#query_executeDocumentSelection)
* :leftwards_arrow_with_hook: [query/executedocumentstatement](#query_executedocumentstatement)
* :leftwards_arrow_with_hook: [query/subset](#query_subset)
* :leftwards_arrow_with_hook: [query/dispose](#query_dispose)
* :leftwards_arrow_with_hook: [query/cancel](#query_cancel)
* :arrow_right: [query/changeConnectionUri](#query_changeConnectionUri)
* :leftwards_arrow_with_hook: [query/saveCsv](#query_saveCsv)
* :leftwards_arrow_with_hook: [query/saveExcel](#query_saveExcel)
* :leftwards_arrow_with_hook: [query/saveJson](#query_saveJson)
* :leftwards_arrow_with_hook: [query/saveXml](#query_saveXml)
* :leftwards_arrow_with_hook: [query/executionPlan](#query_executionPlan)
* :leftwards_arrow_with_hook: [query/simpleexecute](#query_simpleexecute)
* :leftwards_arrow_with_hook: [query/setexecutionoptions](#query_setexecutionoptions)
* :arrow_left: [query/message](#query_message)
* :arrow_left: [query/complete](#query_complete)


### Language Service Protocol

Documentation for the Language Service Protocol is available at
[language service protocol](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md).
The SQL Tools Service implements the following portion Language Service Protocol.

* :leftwards_arrow_with_hook: [initialize](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#initialize)
* :leftwards_arrow_with_hook: [shutdown](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#shutdown)
* :arrow_right: [exit](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#exit)
* :arrow_right: [workspace/didChangeConfiguration](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#workspace_didChangeConfiguration)
* :arrow_right: [workspace/didChangeWatchedFiles](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#workspace_didChangeWatchedFiles)
* :arrow_left: [textDocument/publishDiagnostics](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_publishDiagnostics)
* :arrow_right: [textDocument/didChange](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_didChange)
* :arrow_right: [textDocument/didClose](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_didClose)
* :arrow_right: [textDocument/didOpen](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_didOpen)
* :arrow_right: [textDocument/didSave](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_didSave)
* :leftwards_arrow_with_hook: [textDocument/completion](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_completion)
* :leftwards_arrow_with_hook: [completionItem/resolve](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#completionItem_resolve)
* :leftwards_arrow_with_hook: [textDocument/hover](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_hover)
* :leftwards_arrow_with_hook: [textDocument/signatureHelp](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_signatureHelp)
* :leftwards_arrow_with_hook: [textDocument/references](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_references)
* :leftwards_arrow_with_hook: [textDocument/definition](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md#textDocument_definition)

### Language Service Protocol Extensions

* :leftwards_arrow_with_hook: [completion/extLoad](#completion_extLoad)

# Message Protocol

A message consists of two parts: a header section and the message body.  For now, there is
only one header, `Content-Length`.  This header, written with ASCII encoding, specifies the
UTF-8 byte length of the message content to follow.  The host process expects that all messages
sent to it will come with an accurate `Content-Length` header so that it knows exactly how many
bytes to read.  Likewise, all messages returned from the host process will be sent in this manner.

## Message Schema

The body of a message is encoded in JSON and conforms to a specific schema.  There are three types of
messages that can be transmitted between editor and host process: `Request`, `Response`, and `Event`.

### Common Fields

The common fields shared between all message types are as follows:

- `seq`: A sequence number that increases with each message sent from the editor to the host.
  Even though this field shows up on all message types, it is generally only used to correlate
  response messages to the initial request that caused it.
- `type`: The type of message being transmitted, either `request`, `response`, or `event`.

### Request Fields

A request gets sent by the editor when a particular type of behavior is needed.
In this case, the `type` field will be set to `request`.

- `command`: The request type.  There are a variety of request types that will be enumerated
  later in this document.
- `arguments`: A JSON object containing arguments for the request, varies per each request `command`.

*NOTE: Some `request` types do not require a matching `response` or may cause `events` to be raised at a later time*

### Response Fields

A response gets sent by the host process when a request completes or fails.  In this case,
the `type`field will be set to `response`.

- `request_seq`: The `seq` number that was included with the original request, used to help
  the editor correlate the response to the original request
- `command`: The name of the request command to which this response relates
- `body`: A JSON object body for the response, varies per each response `command`.
- `success`: A boolean indicating whether the request was successful
- `message`: An optional response message, generally used when `success` is set to `false`.

### Event Fields

An event gets sent by the host process when

- `event`: The name of the event type to which this event relates
- `body`: A JSON object body for the event, varies per each `event` type

## Base Protocol

The base protocol consists of a header and a content part (comparable to HTTP). The header and content part are
separated by a '\r\n'.

### Header Part

The header part consists of header fields. Each header field is comprised of a name and a value,
separated by ': ' (a colon and a space).
Each header field is terminated by '\r\n'.
Considering the last header field and the overall header itself are each terminated with '\r\n',
and that at least one header is mandatory, this means that two '\r\n' sequences always
immediately precede the content part of a message.

Currently the following header fields are supported:

| Header Field Name | Value Type  | Description |
|:------------------|:------------|:------------|
| Content-Length    | number      | The length of the content part in bytes. This header is required. |
| Content-Type      | string      | The mime type of the content part. Defaults to application/vscode-jsonrpc; charset=utf8 |

The header part is encoded using the 'ascii' encoding. This includes the '\r\n' separating the header and content part.

### Content Part

Contains the actual content of the message. The content part of a message uses [JSON-RPC](http://www.jsonrpc.org/) to describe requests, responses and notifications. The content part is encoded using the charset provided in the Content-Type field. It defaults to 'utf8', which is the only encoding supported right now.

### Example:

```
Content-Length: ...\r\n
\r\n
{
	"jsonrpc": "2.0",
	"id": 1,
	"method": "textDocument/didOpen",
	"params": {
		...
	}
}
```
### Base Protocol JSON structures

The following TypeScript definitions describe the base [JSON-RPC protocol](http://www.jsonrpc.org/specification):

#### Abstract Message

A general message as defined by JSON-RPC. The language server protocol always uses "2.0" as the jsonrpc version.

```typescript
interface Message {
	jsonrpc: string;
}
```
#### RequestMessage

A request message to describe a request between the client and the server. Every processed request must send a response back to the sender of the request.

```typescript
interface RequestMessage extends Message {

	/**
	 * The request id.
	 */
	id: number | string;

	/**
	 * The method to be invoked.
	 */
	method: string;

	/**
	 * The method's params.
	 */
	params?: any
}
```

#### Response Message

Response Message sent as a result of a request.

```typescript
interface ResponseMessage extends Message {
	/**
	 * The request id.
	 */
	id: number | string;

	/**
	 * The result of a request. This can be omitted in
	 * the case of an error.
	 */
	result?: any;

	/**
	 * The error object in case a request fails.
	 */
	error?: ResponseError<any>;
}

interface ResponseError<D> {
	/**
	 * A number indicating the error type that occurred.
	 */
	code: number;

	/**
	 * A string providing a short description of the error.
	 */
	message: string;

	/**
	 * A Primitive or Structured value that contains additional
	 * information about the error. Can be omitted.
	 */
	data?: D;
}

export namespace ErrorCodes {
	export const ParseError: number = -32700;
	export const InvalidRequest: number = -32600;
	export const MethodNotFound: number = -32601;
	export const InvalidParams: number = -32602;
	export const InternalError: number = -32603;
	export const serverErrorStart: number = -32099;
	export const serverErrorEnd: number = -32000;
	export const serverNotInitialized: number = -32001;
}
```
#### Notification Message

A notification message. A processed notification message must not send a response back. They work like events.

```typescript
interface NotificationMessage extends Message {
	/**
	 * The method to be invoked.
	 */
	method: string;

	/**
	 * The notification's params.
	 */
	params?: any
}
```

## Example JSON-RPC Message Format

See the [language service protocol](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md)
for more details on the protocol formats for these Language Service events.  Below is an example of the JSON-RPC
message format for the `textDocument/didChange` message.

### `textDocument/didChange`

This request is sent by the editor when the user changes the contents of a SQL file that has previously
been opened in the language service.  Depending on how the request arguments are specified, the file change could
either be an arbitrary-length string insertion, region delete, or region replacement.

It is up to the editor to decide how often to send these requests in response
to the user's typing activity.  The language service can deal with change deltas of any length, so it is really
just a matter of preference how often `change` requests are sent.

#### Request

The arguments for this request specify the absolute path of the `file` being changed as well as the complete details
of the edit that the user performed.  The `line`/`endLine` and `offset`/`endOffset` (column) numbers indicate the
1-based range of the file that is being replaced.  The `insertString` field indicates the string that will be
inserted.  In the specified range.

```json
    {
      "seq": 9,
      "type": "request",
      "command": "change",
      "arguments": {
        "file": "c:/Users/UserName/Documents/test.sql",
        "line": 39,
        "offset": 5,
        "endLine": 39,
        "endOffset": 5,
        "insertString": "Test\r\nchange"
      }
    }
```

#### Response

No response is needed for this command.

# Database Management Protocol

The follow section describes the message protocol format for the common database management
functionality provided by the SQL Tools Service.  The message formats are described as
C# classes.  These classes are packaged inside the common message structures documented above
and serialized to JSON using JSON.Net.

## Connection Management


### <a name="connection_connect"></a>`connection/connect`

Establish a connection to a database server.

#### Request

```csharp
    public class ConnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Contains the required parameters to initialize a connection to a database.
        /// A connection will identified by its server name, database name and user name.
        /// This may be changed in the future to support multiple connections with different
        /// connection properties to the same database.
        /// </summary>
        public ConnectionDetails Connection { get; set; }

        /// <summary>
        /// The type of this connection. By default, this is set to ConnectionType.Default.
        /// </summary>
        public string Type { get; set; } = ConnectionType.Default;

        /// <summary>
        /// The porpose of the connection to keep track of open connections
        /// </summary>
        public string Purpose { get; set; } = ConnectionType.GeneralConnection;
    }
```

#### Request Case
```json
Content-length: {the length of the JSON below}

{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "connection/connect",
    "params": {
        "ownerUri": "{An id or a filename, which is binding with this connection}",
        "connection": {
            "serverName": "{your server name}",
            "userName": "{your user name}",
            "password": "{your password}",
            "databaseName": "{your database name}"
        }
    }
}
```

#### Response

```csharp
    bool
```


### <a name="connect_cancelconnect"></a>`connect/cancelconnect`

Cancel an active connection request.

#### Request

```csharp
    public class CancelConnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The type of connection we are trying to cancel
        /// </summary>
        public string Type { get; set; } = ConnectionType.Default;
    }
```

#### Response

```csharp
    bool
```


### <a name="connection_disconnect"></a>`connection/disconnect`

Disconnect the connection specified in the request.

#### Request

```csharp
    public class DisconnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The type of connection we are disconnecting. If null, we will disconnect all connections.
        /// connections.
        /// </summary>
        public string Type { get; set; }
    }
```

#### Response

```csharp
    bool
```

### <a name="connection_listdatabases"></a>`connection/listdatabases`

Return a list of databases on the server associated with the active connection.

#### Request

```csharp
    public class ListDatabasesParams
    {
        /// <summary>
        /// URI of the owner of the connection requesting the list of databases.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// whether to include the details of the databases.
        /// </summary>
        public bool? IncludeDetails { get; set; }
    }
```

#### Response

```csharp
    public class ListDatabasesResponse
    {
        /// <summary>
        /// Gets or sets the list of database names.
        /// </summary>
        public string[] DatabaseNames { get; set; }
    }
```

### <a name="connection_changedatabase"></a>`connection/changedatabase`

Change the database for a connection.

#### Request
```csharp
    public class ChangeDatabaseParams
    {
        /// <summary>
        /// URI of the owner of the connection requesting the list of databases.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The database to change to
        /// </summary>
        public string NewDatabase { get; set; }
    }
```
#### Response
```csharp
    bool
```

### <a name="connection_getconnectionstring"></a>`connection/getconnectionstring`

Get a connection string for the provided connection.

#### Request
```csharp
    public class GetConnectionStringParams
    {
        /// <summary>
        /// URI of the owner of the connection
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Indicates whether the password should be return in the connection string
        /// </summary>
        public bool IncludePassword { get; set; }
    }
```
#### Response
```csharp
    string
```

### <a name="connection_buildconnectioninfo"></a>`connection/buildconnectioninfo`

Serialize a connection string.

#### Request
```csharp
    string
```
#### Response
```csharp
    ConnectionDetails
```

### <a name="connection_connectionchanged"></a>`connection/connectionchanged`

Connection changed notification.

#### Request

```csharp
    public class ConnectionChangedParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.
        /// </summary>
        public string OwnerUri { get; set; }
        /// <summary>
        /// Contains the high-level properties about the connection, for display to the user.
        /// </summary>
        public ConnectionSummary Connection { get; set; }
    }
```

### <a name="connection_complete"></a>`connection/complete`

Connection complete event.

#### Request

```csharp
    public class ConnectionCompleteParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.
        /// </summary>
        public string OwnerUri { get; set;  }

        /// <summary>
        /// A GUID representing a unique connection ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets any detailed connection error messages.
        /// </summary>
        public string Messages { get; set; }

        /// <summary>
        /// Error message returned from the engine for a connection failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error number returned from the engine for connection failure reason, if any.
        /// </summary>
        public int ErrorNumber { get; set; }

        /// <summary>
        /// Information about the connected server.
        /// </summary>
        public ServerInfo ServerInfo { get; set; }

        /// <summary>
        /// Gets or sets the actual Connection established, including Database Name
        /// </summary>
        public ConnectionSummary ConnectionSummary { get; set; }

        /// <summary>
        /// The type of connection that this notification is for
        /// </summary>
        public string Type { get; set; } = ConnectionType.Default;
    }
```


## Query Execution

### <a name="query/executeString"></a>`query/executeString`

Execute a SQL script.

#### Request
```csharp
    public class ExecuteStringParams : ExecuteRequestParamsBase
    {
        /// <summary>
        /// The query to execute
        /// </summary>
        public string Query { get; set; }
    }
    public abstract class ExecuteRequestParamsBase
    {
        /// <summary>
        /// URI for the editor that is asking for the query execute
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Execution plan options
        /// </summary>
        public ExecutionPlanOptions ExecutionPlanOptions { get; set; }

        /// <summary>
        /// Flag to get full column schema via additional queries.
        /// </summary>
        public bool GetFullColumnSchema { get; set; }
    }
```

#### Response

This response has no message but only the JSON-RPC version and the request-id.

```csharp
    public class ExecuteRequestResult
    {
    }
```

### <a name="query_executeDocumentSelection"></a>`query/executeDocumentSelection`

Execute a selection of a document in the workspace service.

#### Request
```csharp
    public class ExecuteDocumentSelectionParams : ExecuteRequestParamsBase
    {
        /// <summary>
        /// The selection from the document
        /// </summary>
        public SelectionData QuerySelection { get; set; }
    }
```
#### Response

This response has no message but only the JSON-RPC version and the request-id.

```csharp
    public class ExecuteRequestResult
    {
    }
```


### <a name="query_executedocumentstatement"></a>`query/executedocumentstatement`

Execute a selection of a document in the workspace service.

#### Request
```csharp
    public class ExecuteDocumentStatementParams : ExecuteRequestParamsBase
    {
        /// <summary>
        /// Line in the document for the location of the SQL statement
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column in the document for the location of the SQL statement
        /// </summary>
        public int Column { get; set; }
    }
```
#### Response

This response has no message but only the JSON-RPC version and the request-id.

```csharp
    public class ExecuteRequestResult
    {
    }
```


### <a name="query_subset"></a>`query/subset`

Retrieve a subset of a query results.

#### Request

```csharp
    public class SubsetParams
    {
        /// <summary>
        /// URI for the file that owns the query to look up the results for
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

        /// <summary>
        /// Beginning index of the rows to return from the selected resultset. This index will be
        /// included in the results.
        /// </summary>
        public long RowsStartIndex { get; set; }

        /// <summary>
        /// Number of rows to include in the result of this request. If the number of the rows
        /// exceeds the number of rows available after the start index, all available rows after
        /// the start index will be returned.
        /// </summary>
        public int RowsCount { get; set; }
    }
```

#### Request Case

```json
Content-length: {the length of the JSON below}

{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "query/subset",
    "params": {
        "ownerUri": "{the owner uri}",
        "batchIndex": 0,
        "resultSetIndex": 0,
        "rowsStartIndex": 0,
        "rowsCount": 1
    }
}
```

#### Response

```csharp
    public class SubsetResult
    {
        /// <summary>
        /// The requested subset of results. Optional, can be set to null to indicate an error
        /// </summary>
        public ResultSetSubset ResultSubset { get; set; }
    }

    public class ResultSetSubset
    {
        /// <summary>
        /// The number of rows returned from result set, useful for determining if less rows were
        /// returned than requested.
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// 2D array of the cell values requested from result set
        /// </summary>
        public DbCellValue[][] Rows { get; set; }
    }
```


#### Response Case

```json
Content-length: {the length of the JSON below}

{
    "jsonrpc": "2.0",
    "id": "1",
    "result": {
        "resultSubset": {
            "rowCount": 1,
            "rows": [
                [
                    {
                        "displayValue": "Alice",
                        "isNull": false,
                        "invariantCultureDisplayValue": null,
                        "rowId": 0
                    },
                    {
                        "displayValue": "Bob",
                        "isNull": false,
                        "invariantCultureDisplayValue": null,
                        "rowId": 0
                    }
                ]
            ]
        }
    }
}
```


### <a name="query_dispose"></a>`query/dispose`

Dispose the query for the owner uri.

#### Request
```csharp
    public class QueryDisposeParams
    {
        public string OwnerUri { get; set; }
    }
```
#### Response

This response has no message but only the JSON-RPC version and the request-id.

```csharp
    public class QueryDisposeResult
    {
    }
```

### <a name="query_cancel"></a>`query/cancel`

Cancel the query in progress for the owner uri.

#### Request
```csharp
    public class QueryCancelParams
    {
        public string OwnerUri { get; set; }
    }
```
#### Response
```csharp
    public class QueryCancelResult
    {
        /// <summary>
        /// Any error messages that occurred during disposing the result set. Optional, can be set
        /// to null if there were no errors.
        /// </summary>
        public string Messages { get; set; }
    }
```
### <a name="query_changeConnectionUri"></a>`query/changeConnectionUri`

Change the uri associated with a query.

#### Notification
```csharp
    public class QueryChangeConnectionUriParams
    {
        public string NewOwnerUri { get; set; }
        public string OriginalOwnerUri { get; set; 
    }
```

### <a name="query_saveCsv"></a>`query/saveCsv`

Save a resultset as CSV to a file.

#### Request

```csharp
    public class SaveResultsAsCsvRequestParams: SaveResultsRequestParams
    {
        /// <summary>
        /// Include headers of columns in CSV
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// Delimiter for separating data items in CSV
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// either CR, CRLF or LF to seperate rows in CSV
        /// </summary>
        public string LineSeperator { get; set; }

        /// <summary>
        /// Text identifier for alphanumeric columns in CSV
        /// </summary>
        public string TextIdentifier { get; set; }

        /// <summary>
        /// Encoding of the CSV file
        /// </summary>
        public string Encoding { get; set; }
    }

    public class SaveResultsRequestParams
    {
        /// <summary>
        /// The path of the file to save results in
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

        /// <summary>
        /// URI for the editor that called save results
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Start index of the selected rows (inclusive)
        /// </summary>
        public int? RowStartIndex { get; set; }

        /// <summary>
        /// End index of the selected rows (inclusive)
        /// </summary>
        public int? RowEndIndex { get; set; }

        /// <summary>
        /// Start index of the selected columns (inclusive)
        /// </summary>
        /// <returns></returns>
        public int? ColumnStartIndex { get; set; }

        /// <summary>
        /// End index of the selected columns (inclusive)
        /// </summary>
        /// <returns></returns>
        public int? ColumnEndIndex { get; set; }

        /// <summary>
        /// Check if request is a subset of result set or whole result set
        /// </summary>
        /// <returns></returns>
        internal bool IsSaveSelection
        {
            get
            {
                return ColumnStartIndex.HasValue && ColumnEndIndex.HasValue
                       && RowStartIndex.HasValue && RowEndIndex.HasValue;
            }
        }
    }
```

#### Response

```csharp
    public class SaveResultRequestResult
    {
        /// <summary>
        /// Error messages for saving to file.
        /// </summary>
        public string Messages { get; set; }
    }
```

### <a name="query_saveExcel"></a>`query/saveExcel`

Save a resultset to a file in Excel format.

#### Request

```csharp
    public class SaveResultsAsExcelRequestParams : SaveResultsRequestParams
    {
        /// <summary>
        /// Include headers of columns in Excel
        /// </summary>
        public bool IncludeHeaders { get; set; }
    }
```
#### Response

```csharp
    public class SaveResultRequestResult
    {
        /// <summary>
        /// Error messages for saving to file.
        /// </summary>
        public string Messages { get; set; }
    }
```




### <a name="query_saveJson"></a>`query/saveJson`

Save a resultset as JSON to a file.

#### Request

```csharp
    public class SaveResultsAsJsonRequestParams: SaveResultsRequestParams
    {
    }
```

#### Response

```csharp
    public class SaveResultRequestResult
    {
        /// <summary>
        /// Error messages for saving to file.
        /// </summary>
        public string Messages { get; set; }
    }
```

### <a name="query_saveXml"></a>`query/saveXml`

Save a resultset to a file in XML format.

#### Request
```csharp
    public class SaveResultsAsXmlRequestParams: SaveResultsRequestParams
    {
        /// <summary>
        /// Formatting of the XML file
        /// </summary>
        public bool Formatted { get; set; }

        /// <summary>
        /// Encoding of the XML file
        /// </summary>
        public string Encoding { get; set; }
    }
```
#### Response
```csharp
    public class SaveResultRequestResult
    {
        /// <summary>
        /// Error messages for saving to file.
        /// </summary>
        public string Messages { get; set; }
    }
```

### <a name="query_executionPlan"></a>`query/executionPlan`

Get an execution plan

#### Request
```csharp
    public class QueryExecutionPlanParams
    {
        /// <summary>
        /// URI for the file that owns the query to look up the results for
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

    }
```
#### Response
```csharp
    public class QueryExecutionPlanResult
    {
        /// <summary>
        /// The requested execution plan. Optional, can be set to null to indicate an error
        /// </summary>
        public ExecutionPlan ExecutionPlan { get; set; }
    }
    public class ExecutionPlan
    {
        /// <summary>
        /// The format of the execution plan
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// The execution plan content
        /// </summary>
        public string Content { get; set; }
    }
```


### <a name="query_simpleexecute"></a>`query/simpleexecute`

Execute a string.

#### Request
```csharp
    public class SimpleExecuteParams
    {
        /// <summary>
        /// The string to execute
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// The owneruri to get connection from
        /// </summary>
        public string OwnerUri { get; set; }
    }
```

#### Response
```csharp
    public class SimpleExecuteResult
    {

        /// <summary>
        /// The number of rows that was returned with the resultset
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public DbColumnWrapper[] ColumnInfo { get; set; }

        /// <summary>
        /// 2D array of the cell values requested from result set
        /// </summary>
        public DbCellValue[][] Rows { get; set; }
    }
```

### <a name="query_setexecutionoptions"></a>`query/setexecutionoptions`

Set query execution options.

#### Request
```csharp
    public class QueryExecutionOptionsParams
    {
        public string OwnerUri { get; set; }

        public QueryExecutionSettings Options { get; set; }
    }
```
#### Response
```csharp
    bool
```


### <a name="query_message"></a>`query/message`

Send the query message.

#### Request
```csharp
    public class MessageParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The message that is being returned
        /// </summary>
        public ResultMessage Message { get; set; }
    }
        public class ResultMessage
    {
        /// <summary>
        /// ID of the batch that generated this message. If null, this message
        /// was not generated as part of a batch
        /// </summary>
        public int? BatchId { get; set; }

        /// <summary>
        /// Whether or not this message is an error
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Timestamp of the message
        /// Stored in UTC ISO 8601 format; should be localized before displaying to any user
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Message contents
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Constructor with default "Now" time
        /// </summary>
        public ResultMessage(string message, bool isError, int? batchId)
        {
            BatchId = batchId;
            IsError = isError;
            Time = DateTime.Now.ToString("o");
            Message = message;
        }

        /// <summary>
        /// Default constructor, used for deserializing JSON RPC only
        /// </summary>
        public ResultMessage()
        {
        }
        public override string ToString() => $"Message on Batch Id:'{BatchId}', IsError:'{IsError}', Message:'{Message}'";
    }
```

#### Request Case

```json
Content-length: {the length of the JSON below}

{
    "jsonrpc": "2.0",
    "method": "query/message",
    "params": {
        "ownerUri": "{the owner uri}",
        "message": {
            "batchId": 0,
            "isError": false,
            "time": "{timestamp}",
            "message": "(1 rows affected)"
        }
    }
}
```

### <a name="query_complete"></a>`query/complete`

Send the query summaries including the batch summaries and the result set summaries.

#### Request
```csharp
    public class QueryCompleteParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Summaries of the result sets that were returned with the query
        /// </summary>
        public BatchSummary[] BatchSummaries { get; set; }
    }

        public class BatchSummary
    {
        /// <summary>
        /// Localized timestamp for how long it took for the execution to complete
        /// </summary>
        public string ExecutionElapsed { get; set; }

        /// <summary>
        /// Localized timestamp for when the execution completed.
        /// </summary>
        public string ExecutionEnd { get; set; }

        /// <summary>
        /// Localized timestamp for when the execution started.
        /// </summary>
        public string ExecutionStart { get; set; }

        /// <summary>
        /// Whether or not the batch encountered an error that halted execution
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// The ID of the result set within the query results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The selection from the file for this batch
        /// </summary>
        public SelectionData Selection { get; set; }

        /// <summary>
        /// The summaries of the result sets inside the batch
        /// </summary>
        public ResultSetSummary[] ResultSetSummaries { get; set; }

        /// <summary>
        /// The special action of the batch
        /// </summary>
        public SpecialAction SpecialAction { get; set; }

        public override string ToString() => $"Batch Id:'{Id}', Elapsed:'{ExecutionElapsed}', HasError:'{HasError}'";
    }

    public class ResultSetSummary
    {
        /// <summary>
        /// The ID of the result set within the batch results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the batch set within the query
        /// </summary>
        public int BatchId { get; set; }

        /// <summary>
        /// The number of rows that are available for the resultset thus far
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// If true it indicates that all rows have been fetched and the RowCount being sent across is final for this ResultSet
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public DbColumnWrapper[] ColumnInfo { get; set; }

        /// <summary>
        /// The special action definition of the result set
        /// </summary>
        public SpecialAction SpecialAction { get; set; }

        /// <summary>
        /// The visualization options for the client to render charts.
        /// </summary>
        public VisualizationOptions Visualization { get; set; }

        /// <summary>
        /// Returns a string represents the current object.
        /// </summary>
        public override string ToString() => $"Result Summary Id:{Id}, Batch Id:'{BatchId}', RowCount:'{RowCount}', Complete:'{Complete}', SpecialAction:'{SpecialAction}', Visualization:'{Visualization}'";
    }
```


#### Request Case

This case only shows some important attributes in JSON.

```json
Content-length: {the length of the JSON below}

{
    "jsonrpc": "2.0",
    "method": "query/complete",
    "params": {
        "ownerUri": "{the owner uri}",
        "batchSummaries": [
            {
                "executionElapsed": "{time}",
                "executionEnd": "{time stamp}",
                "executionStart": "{time stamp}",
                "hasError": false,
                "id": 0,
                "resultSetSummaries": [
                    {
                        "id": 0,
                        "batchId": 0,
                        "rowCount": 2,
                        "complete": true,
                        "columnInfo": [
                            {
                                "columnName": "id",
                                "columnSize": 100,
                                "dataTypeName": "varchar"
                            },
                            {
                                "columnName": "name",
                                "columnSize": 100,
                                "dataTypeName": "varchar"
                            }
                        ],
                    }
                ],
            }
        ]
    }
}
```

## Language Service Protocol Extensions

### <a name="completion_extload"></a>`completion/extLoad`

Load a completion extension.

#### Request

```csharp
    public class CompletionExtensionParams
    {
        /// <summary>
        /// Absolute path for the assembly containing the completion extension
        /// </summary>
        public string AssemblyPath { get; set; }

        /// <summary>
        /// The type name for the completion extension
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Property bag for initializing the completion extension
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }
    }
```

#### Response

```csharp
    bool
```