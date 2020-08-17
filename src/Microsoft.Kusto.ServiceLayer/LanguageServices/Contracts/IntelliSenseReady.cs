//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Parameters sent back with an IntelliSense ready event
    /// </summary>
    public class IntelliSenseReadyParams
    {
        /// <summary>
        /// URI identifying the text document
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Event sent when the language service is finished updating after a connection
    /// </summary>
    public class IntelliSenseReadyNotification
    {
        public static readonly
            EventType<IntelliSenseReadyParams> Type =
            EventType<IntelliSenseReadyParams>.Create("kusto/textDocument/intelliSenseReady");
    }
}
