//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SmoMetadataProvider;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class for storing cached metadata regarding a parsed SQL file
    /// </summary>
    internal class ScriptParseInfo
    {
        /// <summary>
        /// Gets or sets the SMO binder for schema-aware intellisense
        /// </summary>
        public IBinder Binder { get; set; }

        /// <summary>
        /// Gets or sets the previous SQL parse result
        /// </summary>
        public ParseResult ParseResult { get; set; }

        /// <summary>
        /// Gets or set the SMO metadata provider that's bound to the current connection
        /// </summary>
        /// <returns></returns>
        public SmoMetadataProvider MetadataProvider { get; set; }

        /// <summary>
        /// Gets or sets the SMO metadata display info provider
        /// </summary>
        /// <returns></returns>
        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }
    }
}
