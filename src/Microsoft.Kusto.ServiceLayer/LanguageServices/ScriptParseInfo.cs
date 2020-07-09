//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Kusto.Language;
using Kusto.Language.Editor;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Data Source specific class for storing cached metadata regarding a parsed KQL file.
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
        /// Gets or sets the previous Kusto diagnostics result. TODOKusto: Check exact usage.
        /// </summary>
        public IReadOnlyList<Diagnostic> ParseResult { get; set; }
        
        /// <summary>
        /// Gets or sets the current autocomplete suggestion list retrieved from the Kusto language library.
        /// So that other details like documentation can be later retrieved in ResolveCompletionItem.
        /// </summary>
        public IEnumerable<CompletionItem > CurrentSuggestions { get; set; }
    }
}
