//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Identifies what kind of binding context backs a SQL file.
    /// </summary>
    public enum BindingContextKind
    {
        /// <summary>
        /// No binding context; IntelliSense is not available for this file.
        /// </summary>
        None,
        /// <summary>
        /// Backed by a live SMO server connection. <c>connInfo</c> is non-null.
        /// </summary>
        LiveConnection,
        /// <summary>
        /// Backed by an offline TSqlModel built from a SQL project. <c>connInfo</c> is always null.
        /// </summary>
        Project
    }

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
        /// Identifies what kind of binding context backs this file.
        /// <see cref="BindingContextKind.LiveConnection"/> — live SMO connection, connInfo is non-null.
        /// <see cref="BindingContextKind.Project"/>        — offline TSqlModel, connInfo is always null.
        /// <see cref="BindingContextKind.None"/>           — no context yet; IntelliSense unavailable.
        /// </summary>
        public BindingContextKind BindingContextKind { get; set; }

        /// <summary>
        /// Gets or sets the binding queue connection context key
        /// </summary>
        public string ConnectionKey { get; set; }

        /// <summary>Logical database name for project-based offline binding (used when connInfo is null).</summary>
        public string ProjectDatabaseName { get; set; }

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
