//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.EditData.Contracts
{
    public class EditSessionReadyParams
    {
        /// <summary>
        /// URI for the editor
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Message to explain why a session failed. Should only be set when <see cref="Success"/>
        /// is <c>false</c>.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Whether or not the session is ready
        /// </summary>
        public bool Success { get; set; }
    }

    public class EditSessionReadyEvent
    {
        public static readonly
            EventType<EditSessionReadyParams> Type =
            EventType<EditSessionReadyParams>.Create("edit/sessionReady");
    }
}
