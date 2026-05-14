//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    internal sealed class NoOpBinder : IBinder
    {
        public static readonly IBinder Instance = new NoOpBinder();

        private NoOpBinder()
        {
        }

        public IServer Bind(IEnumerable<ParseResult> parseResults, string contextDatabaseName, BindMode bindMode)
        {
            return null;
        }
    }
}
