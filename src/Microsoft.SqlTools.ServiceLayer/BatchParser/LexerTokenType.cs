//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public enum LexerTokenType
    {
        None,
        Text,
        TextVerbatim,
        Whitespace,
        NewLine,
        String,
        Eof,
        Error,
        Comment,

        // batch commands
        Go,
        Reset,
        Ed,
        Execute,
        Quit,
        Exit,
        Include,
        Serverlist,
        Setvar,
        List,
        ErrorCommand,
        Out,
        Perftrace,
        Connect,
        OnError,
        Help,
        Xml,
        ListVar,
    }
}
