//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{

    /// <summary>
    /// Parameters for the Language Flavor Change notification.
    /// </summary>
    public class LanguageFlavorChangeParams
    {
        /// <summary>
        /// A URI identifying the affected resource         
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The primary language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// The specific language flavor that is being set
        /// </summary>
        public string Flavor { get; set; }
    }

    /// <summary>
    /// Defines an event that is sent from the client to notify that
    /// the client is exiting and the server should as well.
    /// </summary>
    public class LanguageFlavorChangeNotification
    {
        public static readonly
            EventType<LanguageFlavorChangeParams> Type =
            EventType<LanguageFlavorChangeParams>.Create("connection/languageflavorchanged");
    }
}