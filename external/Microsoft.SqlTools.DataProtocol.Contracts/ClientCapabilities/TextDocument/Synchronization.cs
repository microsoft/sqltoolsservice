//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument
{
    /// <summary>
    /// Defines which synchonization capabilities the client supports
    /// </summary>
    public class SynchronizationCapabilities : DynamicRegistrationCapability
    {
        /// <summary>
        /// Whether the client supports sending "will save" notifications
        /// </summary>
        public bool? WillSave { get; set; }

        /// <summary>
        /// Whether the client supports sending "did save" notifications
        /// </summary>
        public bool? DidSave { get; set; }
    }
}