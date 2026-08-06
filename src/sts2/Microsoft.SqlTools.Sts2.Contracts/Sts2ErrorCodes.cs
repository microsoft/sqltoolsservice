//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Sts2.Contracts
{
    /// <summary>
    /// Stable STS2 error identities carried in <c>error.data.code</c> (SPEC §7.6).
    /// JSON-RPC <c>error.code</c> stays numeric; these strings are the contract.
    /// Removing or changing a value is a SPEC-CHANGE.
    /// </summary>
    public static class Sts2ErrorCodes
    {
        /// <summary>Connection failed during authentication.</summary>
        public const string ConnectionFailedAuth = "Sts2.ConnectionFailed.Auth";

        /// <summary>Connection failed at the network layer.</summary>
        public const string ConnectionFailedNetwork = "Sts2.ConnectionFailed.Network";

        /// <summary>Connection attempt timed out.</summary>
        public const string ConnectionFailedTimeout = "Sts2.ConnectionFailed.Timeout";

        /// <summary>The server reported a query error.</summary>
        public const string QueryFailedServer = "Sts2.QueryFailed.Server";

        /// <summary>The transport failed while a query was streaming.</summary>
        public const string QueryFailedTransport = "Sts2.QueryFailed.Transport";

        /// <summary>The operation was canceled by a journaled cancel message.</summary>
        public const string Canceled = "Sts2.Canceled";

        /// <summary>The target resource is busy (for example a second active query on one connection).</summary>
        public const string Busy = "Sts2.Busy";

        /// <summary>The request was structurally invalid or carried an unsupported mustUnderstand_ field.</summary>
        public const string InvalidRequest = "Sts2.InvalidRequest";

        /// <summary>The referenced id is unknown.</summary>
        public const string NotFound = "Sts2.NotFound";

        /// <summary>STS2 is dead or not running; legacy traffic is unaffected.</summary>
        public const string Unavailable = "Sts2.Unavailable";

        /// <summary>An unexpected internal failure; details live in the journal, never the wire.</summary>
        public const string Internal = "Sts2.Internal";
    }
}
