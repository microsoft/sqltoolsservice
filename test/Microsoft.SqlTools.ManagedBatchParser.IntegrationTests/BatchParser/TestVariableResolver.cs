//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;

namespace Microsoft.SqlTools.ManagedBatchParser.UnitTests.BatchParser
{
    internal sealed class TestVariableResolver : IVariableResolver
    {
        private StringBuilder outputString;
        private BatchParserSqlCmd batchParserSqlCmd;

        public TestVariableResolver(StringBuilder outputString)
        {
            this.outputString = outputString;
            batchParserSqlCmd = new BatchParserSqlCmd();
        }

        public string GetVariable(PositionStruct pos, string name)
        {
            return batchParserSqlCmd.GetVariable(pos, name);
        }

        public void SetVariable(PositionStruct pos, string name, string value)
        {
            outputString.AppendFormat("Setting variable {0} to [{1}]\n", name, value);
            batchParserSqlCmd.SetVariable(pos, name, value);
        }
    }
}