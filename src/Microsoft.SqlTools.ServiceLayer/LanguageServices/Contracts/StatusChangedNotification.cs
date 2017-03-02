//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Parameters sent back with an status change event
    /// </summary>
    public class StatusChangeParams
    {
        /// <summary>
        /// URI identifying the text document
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The new status for the document
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// Event sent for language service status change notification
    /// </summary>
    public class StatusChangedNotification
    {
        public static readonly
           EventType<StatusChangeParams> Type =
           EventType<StatusChangeParams>.Create("textDocument/statusChanged");
    }
}
