//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class for storing cached metadata regarding a parsed SQL file
    /// </summary>
    public class ScriptParseInfo
    {
        private object buildingMetadataLock = new object();

        /// <summary>
        /// Event which tells if MetadataProvider is built fully or not
        /// </summary>
        public object BuildingMetadataLock
        { 
            get { return this.buildingMetadataLock; }
        }

        /// <summary>
        /// Gets or sets a flag determining is the LanguageService is connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the binding queue connection context key
        /// </summary>
        public string ConnectionKey { get; set; }

        /// <summary>
        /// Gets or sets the previous SQL parse result
        /// </summary>
        public ParseResult ParseResult { get; set; }
        
        /// <summary>
        /// Gets or sets the current autocomplete suggestion list
        /// </summary>
        public IEnumerable<Declaration> CurrentSuggestions { get; set; }
    }
}
