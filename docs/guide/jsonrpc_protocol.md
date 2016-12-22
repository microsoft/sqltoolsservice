# SQL Tools JSON-RPC Protocol

The SQL Tools JSON-RPC API provides an host-agnostic interface for the SQL Tools Service functionality.
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

## Messages overview

The SQL Tools Service implements portions of the 
[language service protocol](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md)
defined by VS Code.  Some portions of this protocol reference have been duplicated here for 
convenience.  

It additionally implements several other API to provide database management
services such as connection management or query execution.

This document provides the protocol specification for all the service's JSON-RPC APIs.

### Connection Management

* :leftwards_arrow_with_hook: [connection/connect](#connect_connect)
* :leftwards_arrow_with_hook: [connection/cancelconnect](#connect_cancelconnect)
* :arrow_right: [connection/connectionchanged](#connection_connectionchanged)
* :arrow_right: [connection/complete](#connection_complete)
* :arrow_right: [connection/disconnect](#connection_disconnect)

### Query Execution
* :leftwards_arrow_with_hook: [query/execute](#query_execute)

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
  reponse messages to the initial request that caused it.
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

## Example Request and Response Message

See the [language service protocol](https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md) 
for more details on the protocol formats for these Langauge Service events.  Below is an example of the JSON-RPC
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
functionaltiy provided by the SQL Tools Service.

## Connection Management

### <a name="connection_connect"></a>`connection/connect`

Establish a connection to a database server.

#### Request

```typescript
    public class ConnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set;  }
        /// <summary>
        /// Contains the required parameters to initialize a connection to a database.
        /// A connection will identified by its server name, database name and user name.
        /// This may be changed in the future to support multiple connections with different 
        /// connection properties to the same database.
        /// </summary>
        public ConnectionDetails Connection { get; set; }
    }
```

#### Response

```typescript
    bool
```

### <a name="connect_cancelconnect"></a>`connect/cancelconnect`

Cancel an active connection request.

#### Request

```typescript
    public class CancelConnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set;  }
    }
```

#### Response

```typescript
    bool
```

### <a name="connection_connectionchanged"></a>`connection/connectionchanged`

Connection changed notification

#### Request

```typescript
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

```typescript
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
    }
```

### <a name="connection_disconnect"></a>`connection/disconnect`

Disconnect the connection specified in the request.

#### Request

```typescript
    public class DisconnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set; }
    }
```

#### Response

```typescript
    bool
```

### <a name="connection_listdatabases"></a>`connection/listdatabases`

Return a list of databases on the server associated with the active connection.

#### Request

```typescript
    public class ListDatabasesParams
    {
        /// <summary>
        /// URI of the owner of the connection requesting the list of databases.
        /// </summary>
        public string OwnerUri { get; set; }
    }
```

#### Response

```typescript
    public class ListDatabasesResponse
    {
        /// <summary>
        /// Gets or sets the list of database names.
        /// </summary>
        public string[] DatabaseNames { get; set; }
    }
```

## Query Execution

### <a name="query_execute"></a>`query/execute`

Execute a SQL script.

#### Request

```typescript
    public class QueryExecuteParams
    {
        /// <summary>
        /// The selection from the document
        /// </summary>
        public SelectionData QuerySelection { get; set; }

        /// <summary>
        /// URI for the editor that is asking for the query execute
        /// </summary>
        public string OwnerUri { get; set; }
    }
```

#### Response

```typescript
    public class QueryExecuteResult
    {
        /// <summary>
        /// Informational messages from the query runner. Optional, can be set to null.
        /// </summary>
        public string Messages { get; set; }
    }
```
