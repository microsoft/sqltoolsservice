using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public class SqlCmdCommand
    {
        public SqlCmdCommand(LexerTokenType tokenType, string argument)
        {
            this.LexerTokenType = tokenType;
            this.Argument = argument;
        }

        public LexerTokenType LexerTokenType { get; set; }

        public string Argument { get; set; }
    }
}
