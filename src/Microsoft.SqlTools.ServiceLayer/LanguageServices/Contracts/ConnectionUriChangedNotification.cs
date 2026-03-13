//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Parameters for the connection/uriChanged notification.
    /// Sent by the client when a document is saved (untitled → file path) or renamed,
    /// so that sqltoolsservice can atomically transfer all per-URI language-service state.
    /// </summary>
    public class ConnectionUriChangedParams
    {
        /// <summary>
        /// The old URI of the document (e.g. "untitled:Untitled-1" or "file:///old.sql").
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The new URI of the document (e.g. "file:///path/to/test.sql").
        /// </summary>
        public string NewOwnerUri { get; set; }
    }

    /// <summary>
    /// Notification sent when a document's URI changes due to a save or rename operation.
    /// Allows sqltoolsservice to rebind IntelliSense/language-service state from the old
    /// URI to the new URI without a full disconnect + reconnect cycle.
    /// </summary>
    public class ConnectionUriChangedNotification
    {
        public static readonly EventType<ConnectionUriChangedParams> Type =
            EventType<ConnectionUriChangedParams>.Create("connection/uriChanged");
    }
}
