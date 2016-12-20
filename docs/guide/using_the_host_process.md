# Using the JSON-RPC API

The JSON-RPC API provides an host-agnostic interface for
leveraging the core .NET API.

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

General

* :leftwards_arrow_with_hook: [initialize](#initialize) 
* :leftwards_arrow_with_hook: [shutdown](#shutdown) 
* :arrow_right: [exit](#exit) 
* :arrow_right: [$/cancelRequest](#cancelRequest)

Window

* :arrow_left: [window/showMessage](#window_showMessage) 
* :arrow_right_hook: [window/showMessageRequest](#window_showMessageRequest) 
* :arrow_left: [window/logMessage](#window_logMessage) 
* :arrow_left: [telemetry/event](#telemetry_event) 

Workspace

* :arrow_right: [workspace/didChangeConfiguration](#workspace_didChangeConfiguration) 
* :arrow_right: [workspace/didChangeWatchedFiles](#workspace_didChangeWatchedFiles) 
* :leftwards_arrow_with_hook: [workspace/symbol](#workspace_symbol) 

Document

* :arrow_left: [textDocument/publishDiagnostics](#textDocument_publishDiagnostics) 
* :arrow_right: [textDocument/didChange](#textDocument_didChange)  
* :arrow_right: [textDocument/didClose](#textDocument_didClose) 
* :arrow_right: [textDocument/didOpen](#textDocument_didOpen) 
* :arrow_right: [textDocument/didSave](#textDocument_didSave) 
* :leftwards_arrow_with_hook: [textDocument/completion](#textDocument_completion) 
* :leftwards_arrow_with_hook: [completionItem/resolve](#completionItem_resolve) 
* :leftwards_arrow_with_hook: [textDocument/hover](#textDocument_hover) 
* :leftwards_arrow_with_hook: [textDocument/signatureHelp](#textDocument_signatureHelp) 
* :leftwards_arrow_with_hook: [textDocument/references](#textDocument_references) 
* :leftwards_arrow_with_hook: [textDocument/documentHighlight](#textDocument_documentHighlight) 
* :leftwards_arrow_with_hook: [textDocument/documentSymbol](#textDocument_documentSymbol) 
* :leftwards_arrow_with_hook: [textDocument/formatting](#textDocument_formatting) 
* :leftwards_arrow_with_hook: [textDocument/rangeFormatting](#textDocument_rangeFormatting) 
* :leftwards_arrow_with_hook: [textDocument/onTypeFormatting](#textDocument_onTypeFormatting) 
* :leftwards_arrow_with_hook: [textDocument/definition](#textDocument_definition) 
* :leftwards_arrow_with_hook: [textDocument/codeAction](#textDocument_codeAction) 
* :leftwards_arrow_with_hook: [textDocument/codeLens](#textDocument_codeLens) 
* :leftwards_arrow_with_hook: [codeLens/resolve](#codeLens_resolve) 
* :leftwards_arrow_with_hook: [textDocument/documentLink](#textDocument_documentLink) 
* :leftwards_arrow_with_hook: [documentLink/resolve](#documentLink_resolve) 
* :leftwards_arrow_with_hook: [textDocument/rename](#textDocument_rename) 

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

#### <a name="cancelRequest"></a> Cancellation Support

>**New:** The base protocol now offers support for request cancellation. To cancel a request, a notification message with the following properties is sent:
 
_Notification_:
* method: '$/cancelRequest'
* params: `CancelParams` defined as follows:
```typescript
interface CancelParams {
	/**
	 * The request id to cancel.
	 */
	id: number | string;
}
```

A request that got canceled still needs to return from the server and send a response back. It can not be left open / hanging. This is in line with the JSON RPC protocol that requires that every request sends a response back. In addition it allows for returning partial results on cancel.

## Language Server Protocol

The language server protocol defines a set of JSON-RPC request, response and notification messages which are exchanged using the above base protocol. This section starts describing the basic JSON structures used in the protocol. The document uses TypeScript interfaces to describe these. Based on the basic JSON structures, the actual requests with their responses and the notifications are described.

The protocol currently assumes that one server serves one tool. There is currently no support in the protocol to share one server between different tools. Such a sharing would require additional protocol to either lock a document to support concurrent editing.

### Basic JSON Structures

#### URI

URI's are transferred as strings. The URI's format is defined in [http://tools.ietf.org/html/rfc3986](http://tools.ietf.org/html/rfc3986)

```
  foo://example.com:8042/over/there?name=ferret#nose
  \_/   \______________/\_________/ \_________/ \__/
   |           |            |            |        |
scheme     authority       path        query   fragment
   |   _____________________|__
  / \ /                        \
  urn:example:animal:ferret:nose
```

We also maintain a node module to parse a string into `scheme`, `authority`, `path`, `query`, and `fragment` URI components. The GitHub repository is [https://github.com/Microsoft/vscode-uri](https://github.com/Microsoft/vscode-uri) the npm module is [https://www.npmjs.com/package/vscode-uri](https://www.npmjs.com/package/vscode-uri).


#### Position

Position in a text document expressed as zero-based line and character offset. A position is between two characters like an 'insert' cursor in a editor.

```typescript
interface Position {
	/**
	 * Line position in a document (zero-based).
	 */
	line: number;

	/**
	 * Character offset on a line in a document (zero-based).
	 */
	character: number;
}
```
#### Range

A range in a text document expressed as (zero-based) start and end positions. A range is comparable to a selection in an editor. Therefore the end position is exclusive.

```typescript
interface Range {
	/**
	 * The range's start position.
	 */
	start: Position;

	/**
	 * The range's end position.
	 */
	end: Position;
}
```

#### Location

Represents a location inside a resource, such as a line inside a text file.
```typescript
interface Location {
	uri: string;
	range: Range;
}
```











# Request and Response Types

## File Management

### `open`

This request is sent by the editor when the user opens a file with a known PowerShell extension.  It causes
the file to be opened by the language service and parsed for syntax errors.

#### Request

The arguments object specifies the absolute file path to be loaded.

```json
    {
      "seq": 0,
      "type": "request",
      "command": "open",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1"
      }
    }
```

#### Response

No response is needed for this command.

### `close`

This request is sent by the editor when the user closes a file with a known PowerShell extension which was
previously opened in the language service.

#### Request

The arguments object specifies the absolute file path to be closed.

```json
    {
      "seq": 3,
      "type": "request",
      "command": "close",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1"
      }
    }
```

#### Response

No response is needed for this command.

### `change`

This request is sent by the editor when the user changes the contents of a PowerShell file that has previously
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

*NOTE: In the very near future, all file locations will be specified with zero-based coordinates.*

```json
    {
      "seq": 9,
      "type": "request",
      "command": "change",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
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

### `geterr`

This request causes script diagnostics to be performed on a list of script file paths.  The editor
will typically send this request after every successful `change` request (though it is best to throttle
these requests on the editor side so that it doesn't overwhelm the host).  Responses will be sent back
as `syntaxDiag` and `semanticDiag` events.

#### Request

The arguments for this request specify the list of `files` to be analyzed (sorted by those most recently changed)
and a millisecond `delay` value which instructs the language service to wait for some period before performing
diagnostics.  If another `geterr` request is sent before the specified `delay` expires, the original request will
be cancelled server-side and a new delay period will start.

```json
    {
      "seq": 1,
      "type": "request",
      "command": "geterr",
      "arguments": {
        "delay": 750,
        "files": [
          "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1"
        ]
      }
    }
```

## Code Completions

### `completions`

### Request

```json
    {
      "seq": 34,
      "type": "request",
      "command": "completions",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
        "line": 34,
        "offset": 9
      }
    }
```

#### Response

```json
    {
      "request_seq": 34,
      "success": true,
      "command": "completions",
      "message": null,
      "body": [
        {
          "name": "Get-Acl",
          "kind": "method",
          "kindModifiers": null,
          "sortText": null
        },
        {
          "name": "Get-Alias",
          "kind": "method",
          "kindModifiers": null,
          "sortText": null
        },
        {
          "name": "Get-AliasPattern",
          "kind": "method",
          "kindModifiers": null,
          "sortText": null
        },
        {
          "name": "Get-AppLockerFileInformation",
          "kind": "method",
          "kindModifiers": null,
          "sortText": null
        },
        {
          "name": "Get-AppLockerPolicy",
          "kind": "method",
          "kindModifiers": null,
          "sortText": null
        },
        {
          "name": "Get-AppxDefaultVolume",
          "kind": "method",
          "kindModifiers": null,
          "sortText": null
        },

        ... many more completions ...

      ],
      "seq": 0,
      "type": "response"
    }
```

### `completionEntryDetails`

#### Request

```json
    {
      "seq": 37,
      "type": "request",
      "command": "completionEntryDetails",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
        "line": 34,
        "offset": 9,
        "entryNames": [
          "Get-Acl"
        ]
      }
    }
```

#### Response

```json
    {
      "request_seq": 37,
      "success": true,
      "command": "completionEntryDetails",
      "message": null,
      "body": [
        {
          "name": null,
          "kind": null,
          "kindModifiers": null,
          "displayParts": null,
          "documentation": null,
          "docString": null
        }
      ],
      "seq": 0,
      "type": "response"
    }
```

### `signatureHelp`

#### Request

```json
    {
      "seq": 36,
      "type": "request",
      "command": "signatureHelp",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
        "line": 34,
        "offset": 9
      }
    }
```

#### Response

** TODO: This is a bad example, find another**

```json
    {
      "request_seq": 36,
      "success": true,
      "command": "signatureHelp",
      "message": null,
      "body": null,
      "seq": 0,
      "type": "response"
    }
````

## Symbol Operations

### `definition`

#### Request

```json
    {
      "seq": 20,
      "type": "request",
      "command": "definition",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/StopTest.ps1",
        "line": 8,
        "offset": 10
      }
    }
```

#### Response

```json
    {
      "request_seq": 20,
      "success": true,
      "command": "definition",
      "message": null,
      "body": [
        {
          "file": "c:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\Stop-Process2.ps1",
          "start": {
            "line": 11,
            "offset": 10
          },
          "end": {
            "line": 11,
            "offset": 23
          }
        }
      ],
      "seq": 0,
      "type": "response"
    }
```

### `references`

#### Request

```json
    {
      "seq": 2,
      "type": "request",
      "command": "references",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
        "line": 38,
        "offset": 12
      }
    }
```

#### Response

```json
    {
      "request_seq": 2,
      "success": true,
      "command": "references",
      "message": null,
      "body": {
        "refs": [
          {
            "lineText": "\t\t\tforeach ($process in $processes)",
            "isWriteAccess": true,
            "file": "c:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\Stop-Process2.ps1",
            "start": {
              "line": 32,
              "offset": 13
            },
            "end": {
              "line": 32,
              "offset": 21
            }
          }
        ],
        "symbolName": "$process",
        "symbolStartOffest": 690,
        "symbolDisplayString": "$process"
      },
      "seq": 0,
      "type": "response"
    }
```

### `occurrences`

#### Request

```json
    {
      "seq": 53,
      "type": "request",
      "command": "occurrences",
      "arguments": {
        "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
        "line": 32,
        "offset": 17
      }
    }
```

#### Response

```json
    {
      "request_seq": 53,
      "success": true,
      "command": "occurrences",
      "message": null,
      "body": [
        {
          "isWriteAccess": true,
          "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
          "start": {
            "line": 32,
            "offset": 13
          },
          "end": {
            "line": 32,
            "offset": 21
          }
        },
        {
          "isWriteAccess": true,
          "file": "c:/Users/daviwil/.vscode/extensions/vscode-powershell/examples/Stop-Process2.ps1",
          "start": {
            "line": 35,
            "offset": 11
          },
          "end": {
            "line": 35,
            "offset": 19
          }
        },

        ... more occurrences ...

      ],
      "seq": 0,
      "type": "response"
    }
```

## Debugger Operations

### `initialize`

### `launch`

This request is sent by the editor when the user wants to launch a given script file in the
debugger.

#### Request

```json
    {
      "type": "request",
      "seq": 3,
      "command": "launch",
      "arguments": {
        "program": "c:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1",
        "stopOnEntry": false,
        "arguments": null,
        "workingDirectory": "c:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples",
        "runtimeExecutable": null,
        "runtimeArguments": null
      }
    }
```

```json
    {
      "request_seq": 3,
      "success": true,
      "command": "launch",
      "message": null,
      "body": null,
      "seq": 0,
      "type": "response"
    }
```

### `disconnect`

This request is sent by the editor when the user wants to terminate the debugging session before
the script completes.  When this message is received, execution of the script is aborted and the
instance of the host process is aborted.

*NOTE: For now, it is assumed that debugging will be performed in a separate instance of the
 host process.  This will change in the next couple of minor releases.*

#### Request

```json
    {
      "type": "request",
      "seq": 22,
      "command": "disconnect",
      "arguments": {
        "extensionHostData": {
          "restart": false
        }
      }
    }
```

#### Response

```json
    {
      "request_seq": 0,
      "success": false,
      "command": "disconnect",
      "message": null,
      "body": null,
      "seq": 0,
      "type": "response"
    }
```

### `setBreakpoints`

#### Request

```json
    {
      "type": "request",
      "seq": 2,
      "command": "setBreakpoints",
      "arguments": {
        "source": {
          "path": "c:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1"
        },
        "lines": [
          10
        ]
      }
    }
```

#### Response

```json
    {
      "request_seq": 2,
      "success": true,
      "command": "setBreakpoints",
      "message": null,
      "body": {
        "breakpoints": [
          {
            "verified": true,
            "line": 10
          }
        ]
      },
      "seq": 0,
      "type": "response"
    }
```

### `pause`

#### Request

```json
    {
      "type": "request",
      "seq": 4,
      "command": "pause"
    }
```

#### Response

No response needed for this command.  The debugging service will send a `stopped` event
when execution is stopped due to this request.

### `continue`

#### Request

```json
    {
      "type": "request",
      "seq": 9,
      "command": "continue"
    }
```

#### Response

```json
    {
      "request_seq": 9,
      "success": true,
      "command": "continue",
      "message": null,
      "body": null,
      "seq": 0,
      "type": "response"
    }
```

### `next`

#### Request

```json
    {
      "type": "request",
      "seq": 9,
      "command": "next"
    }
```

#### Response

```json
    {
      "request_seq": 9,
      "success": true,
      "command": "next",
      "message": null,
      "body": null,
      "seq": 0,
      "type": "response"
    }
```

### `stepIn`

#### Request

```json
    {
      "type": "request",
      "seq": 13,
      "command": "stepIn"
    }
```

#### Response

```json
    {
      "request_seq": 13,
      "success": true,
      "command": "stepIn",
      "message": null,
      "body": null,
      "seq": 0,
      "type": "response"
    }
```

### `stepOut`

#### Request

```json
    {
      "type": "request",
      "seq": 17,
      "command": "stepOut"
    }
```

#### Response

```json
    {
      "request_seq": 17,
      "success": true,
      "command": "stepOut",
      "message": null,
      "body": null,
      "seq": 0,
      "type": "response"
    }
```

### `threads`

#### Request

```json
    {
      "type": "request",
      "seq": 5,
      "command": "threads"
    }
```

#### Response

```json
    {
      "request_seq": 5,
      "success": true,
      "command": "threads",
      "message": null,
      "body": {
        "threads": [
          {
            "id": 1,
            "name": "Main Thread"
          }
        ]
      },
      "seq": 0,
      "type": "response"
    }
```

### `scopes`

#### Request

```json
    {
      "type": "request",
      "seq": 7,
      "command": "scopes",
      "arguments": {
        "frameId": 1
      }
    }
```

#### Response

```json
    {
      "request_seq": 7,
      "success": true,
      "command": "scopes",
      "message": null,
      "body": {
        "scopes": [
          {
            "name": "Locals",
            "variablesReference": 1,
            "expensive": false
          }
        ]
      },
      "seq": 0,
      "type": "response"
    }
```

### `variables`

#### Request

```json
    {
      "type": "request",
      "seq": 8,
      "command": "variables",
      "arguments": {
        "variablesReference": 1
      }
    }
```

#### Response

```json
    {
      "request_seq": 8,
      "success": true,
      "command": "variables",
      "message": null,
      "body": {
        "variables": [
          {
            "name": "?",
            "value": "True",
            "variablesReference": 0
          },
          {
            "name": "args",
            "value": " ",
            "variablesReference": 11
          },
          {
            "name": "ConsoleFileName",
            "value": "",
            "variablesReference": 0
          },
          {
            "name": "ExecutionContext",
            "value": " ",
            "variablesReference": 13
          },
          {
            "name": "false",
            "value": "False",
            "variablesReference": 0
          },
          {
            "name": "HOME",
            "value": "C:\\Users\\daviwil",
            "variablesReference": 0
          },

          ... more variables ...

        ]
      },
      "seq": 0,
      "type": "response"
    }
```

### `stackTrace`

#### Request

```json
    {
      "type": "request",
      "seq": 6,
      "command": "stackTrace",
      "arguments": {
        "threadId": 1,
        "levels": 20
      }
    }
```

#### Response

```json
    {
      "request_seq": 6,
      "success": true,
      "command": "stackTrace",
      "message": null,
      "body": {
        "stackFrames": [
          {
            "id": 1,
            "name": "Write-Item",
            "source": {
              "name": null,
              "path": "C:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1",
              "sourceReference": null
            },
            "line": 10,
            "column": 9
          },
          {
            "id": 2,
            "name": "Do-Work",
            "source": {
              "name": null,
              "path": "C:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1",
              "sourceReference": null
            },
            "line": 18,
            "column": 5
          },
          {
            "id": 3,
            "name": "<ScriptBlock>",
            "source": {
              "name": null,
              "path": "C:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1",
              "sourceReference": null
            },
            "line": 23,
            "column": 1
          }
        ]
      },
      "seq": 0,
      "type": "response"
    }
```

### `evaluate`

#### Request

```json
    {
      "type": "request",
      "seq": 13,
      "command": "evaluate",
      "arguments": {
        "expression": "i",
        "frameId": 1
      }
    }
```

#### Response

```json
    {
      "request_seq": 13,
      "success": true,
      "command": "evaluate",
      "message": null,
      "body": {
        "result": "2",
        "variablesReference": 0
      },
      "seq": 0,
      "type": "response"
    }
```

# Event Types

## Script Diagnostics

### `syntaxDiag`

```json
    {
      "event": "syntaxDiag",
      "body": {
        "file": "c:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1",
        "diagnostics": [
          {
            "start": {
              "line": 3,
              "offset": 1
            },
            "end": {
              "line": 3,
              "offset": 2
            },
            "text": "Missing closing '}' in statement block or type definition.",
            "severity": 2
          }
        ]
      },
      "seq": 0,
      "type": "event"
    }
```

### `semanticDiag`

```json
    {
      "event": "semanticDiag",
      "body": {
        "file": "c:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1",
        "diagnostics": [
          {
            "start": {
              "line": 14,
              "offset": 1
            },
            "end": {
              "line": 21,
              "offset": 2
            },
            "text": "The cmdlet 'Do-Work' uses an unapproved verb.",
            "severity": 1
          },
          {
            "start": {
              "line": 20,
              "offset": 5
            },
            "end": {
              "line": 20,
              "offset": 23
            },
            "text": "File '' uses Write-Host. This is not recommended because it may not work in some hosts or there may even be no hosts at all. Use Write-Output instead.",
            "severity": 1
          },
          {
            "start": {
              "line": 18,
              "offset": 16
            },
            "end": {
              "line": 18,
              "offset": 26
            },
            "text": "Variable 'workcount' is not initialized. Non-global variables must be initialized. To fix a violation of this rule, please initialize non-global variables.",
            "severity": 1
          }
        ]
      },
      "seq": 0,
      "type": "event"
    }
```

## Language Service Events

### `started`

This message is sent as soon as the language service finishes initializing.  The editor will
wait for this message to be received before it starts sending requests to the host.  This event
has no body and will always be `null`.

```json
    {
      "event": "started",
      "body": null,
      "seq": 0,
      "type": "event"
    }
```

## Debugger Events

### `initialized`

```json
    {
      "event": "initialized",
      "body": null,
      "seq": 0,
      "type": "event"
    }
```

### `stopped`

```json
    {
      "event": "stopped",
      "body": {
        "reason": "breakpoint",
        "threadId": 1,
        "source": {
          "name": null,
          "path": "C:\\Users\\daviwil\\.vscode\\extensions\\vscode-powershell\\examples\\DebugTest.ps1",
          "sourceReference": null
        },
        "line": 10,
        "column": 9,
        "text": null
      },
      "seq": 0,
      "type": "event"
    }
```

### `terminated`

```json
    {
      "event": "terminated",
      "body": null,
      "seq": 0,
      "type": "event"
    }
```

# Host Process Lifecycle

Right now, language and debugging service generally run separately.

## Language Service

`started` event, etc

