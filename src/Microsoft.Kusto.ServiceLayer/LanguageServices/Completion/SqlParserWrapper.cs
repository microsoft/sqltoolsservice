//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Completion
{
    /// <summary>
    /// SqlParserWrapper interface
    /// </summary>
    public interface ISqlParserWrapper
    {
        IEnumerable<Declaration> FindCompletions(ParseResult parseResult, int line, int col, IMetadataDisplayInfoProvider displayInfoProvider);
    }

    /// <summary>
    /// A wrapper class around SQL parser methods to make the operations testable
    /// </summary>
    public class SqlParserWrapper : ISqlParserWrapper
    {
        /// <summary>
        /// Creates completion list given SQL script info
        /// </summary>
        public IEnumerable<Declaration> FindCompletions(ParseResult parseResult, int line, int col, IMetadataDisplayInfoProvider displayInfoProvider)
        {
            return Resolver.FindCompletions(parseResult, line, col, displayInfoProvider);
        }
    }
}
