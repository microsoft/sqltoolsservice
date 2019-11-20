//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    /// <summary>
    /// Dynamically resolves the token IDs to match the values in the enum Microsoft.SqlServer.Management.SqlParser.Parser.Tokens.
    /// This way, if the values in the enum change but their names remain the same
    /// (when the Microsoft.SqlServer.Management.SqlParser.Parser.dll adds new tokens to the enum and is rebuilt),
    /// the new values are retreived at runtime without having to rebuild Microsoft.SqlTools.ServiceLayer.Formatter.dll
    /// </summary>
    static class FormatterTokens
    {
        private static int ResolveTokenId(string tokenName)
        {
            EnumConverter converter = new EnumConverter(typeof(Tokens));
            return (int)converter.ConvertFromString(tokenName);
        }

        public static readonly int TOKEN_COMMA = 44;    // not included in Tokens enum for some reason
        public static readonly int TOKEN_FOR = ResolveTokenId("TOKEN_FOR");
        public static readonly int TOKEN_REPLICATION = ResolveTokenId("TOKEN_REPLICATION");
        public static readonly int TOKEN_ID = ResolveTokenId("TOKEN_ID");
        public static readonly int LEX_END_OF_LINE_COMMENT = ResolveTokenId("LEX_END_OF_LINE_COMMENT");
        public static readonly int TOKEN_FROM = ResolveTokenId("TOKEN_FROM");
        public static readonly int TOKEN_SELECT = ResolveTokenId("TOKEN_SELECT");
        public static readonly int TOKEN_TABLE = ResolveTokenId("TOKEN_TABLE");
        public static readonly int TOKEN_USEDB = ResolveTokenId("TOKEN_USEDB");
        public static readonly int TOKEN_NOT = ResolveTokenId("TOKEN_NOT");
        public static readonly int TOKEN_NULL = ResolveTokenId("TOKEN_NULL");
        public static readonly int TOKEN_IDENTITY = ResolveTokenId("TOKEN_IDENTITY");
        public static readonly int TOKEN_ORDER = ResolveTokenId("TOKEN_ORDER");
        public static readonly int TOKEN_BY = ResolveTokenId("TOKEN_BY");
        public static readonly int TOKEN_DESC = ResolveTokenId("TOKEN_DESC");
        public static readonly int TOKEN_ASC = ResolveTokenId("TOKEN_ASC");
        public static readonly int TOKEN_GROUP = ResolveTokenId("TOKEN_GROUP");
        public static readonly int TOKEN_WHERE = ResolveTokenId("TOKEN_WHERE");
        public static readonly int TOKEN_JOIN = ResolveTokenId("TOKEN_JOIN");
        public static readonly int TOKEN_ON = ResolveTokenId("TOKEN_ON");
        public static readonly int TOKEN_UNION = ResolveTokenId("TOKEN_UNION");
        public static readonly int TOKEN_ALL = ResolveTokenId("TOKEN_ALL");
        public static readonly int TOKEN_EXCEPT = ResolveTokenId("TOKEN_EXCEPT");
        public static readonly int TOKEN_INTERSECT = ResolveTokenId("TOKEN_INTERSECT");
        public static readonly int TOKEN_INTO = ResolveTokenId("TOKEN_INTO");
        public static readonly int TOKEN_DEFAULT = ResolveTokenId("TOKEN_DEFAULT");
        public static readonly int TOKEN_WITH = ResolveTokenId("TOKEN_WITH");
        public static readonly int TOKEN_AS = ResolveTokenId("TOKEN_AS");
        public static readonly int TOKEN_IS = ResolveTokenId("TOKEN_IS");
        public static readonly int TOKEN_BEGIN_CS = ResolveTokenId("TOKEN_BEGIN_CS");
        public static readonly int TOKEN_END_CS = ResolveTokenId("TOKEN_END_CS");
        public static readonly int LEX_BATCH_SEPERATOR = ResolveTokenId("LEX_BATCH_SEPERATOR");
        public static readonly int TOKEN_CREATE = ResolveTokenId("TOKEN_CREATE");
        public static readonly int LAST_TOKEN = ResolveTokenId("LAST_TOKEN");

    }

    static class TokenConverter
    {
        public static int ToInt(this Tokens token)
        {
            return (int)token;
        }
    }
}
