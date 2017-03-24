//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Xunit;
using Moq;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.BatchParser
{
    public class BatchParserSqlCmdTests : IDisposable
    {
        private BatchParserSqlCmd bpcmd;
        private PositionStruct testPOS;
        public BatchParserSqlCmdTests()
        {
            bpcmd = new BatchParserSqlCmd();
            testPOS = new PositionStruct();
            bpcmd.SetVariable(testPOS, "variable1", "test1");
            bpcmd.SetVariable(testPOS, "variable2", "test2");
            bpcmd.SetVariable(testPOS, "variable3", "test3");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (bpcmd != null)
                {
                    bpcmd = null;
                }
            }
        }

        [Fact]
        public void CheckSetVariable()
        {
            Assert.Equal(bpcmd.InternalVariables.Count, 3);
            bpcmd.SetVariable(testPOS, "variable4", "test4");
            bpcmd.SetVariable(testPOS, "variable5", "test5");
            bpcmd.SetVariable(testPOS, "variable6", "test6");
            Assert.Equal(bpcmd.InternalVariables.Count, 6);

            bpcmd.SetVariable(testPOS, "variable1", null);
        }

        [Fact]
        public void CheckSetNullValueVariable()
        {
            bpcmd.SetVariable(testPOS, "variable6", null);
            Assert.Equal(bpcmd.InternalVariables.Count, 5);
        }

        [Fact]
        public void CheckGetVariable()
        {
            string value = bpcmd.GetVariable(testPOS, "variable1");
            Assert.Equal("test1", value);
            value = bpcmd.GetVariable(testPOS, "variable2");
            Assert.Equal("test2", value);
            value = bpcmd.GetVariable(testPOS, "variable3");
            Assert.Equal("test3", value);
        }

        [Fact]
        public void CheckGetNullVariable()
        {
            Assert.Null(bpcmd.GetVariable(testPOS, "variable6"));
        }

        [Fact]
        public void CheckInclude()
        {
            TextReader textReader = null;
            string outString = "out";
            var result = bpcmd.Include(null, out textReader, out outString);
            Assert.Equal(result, BatchParserAction.Abort);

        }

        [Fact]
        public void CheckOnError()
        {
            var errorActionChanged = bpcmd.ErrorActionChanged;
            var action = new OnErrorAction();
            var result = bpcmd.OnError(null, action);
            Assert.Equal(result, BatchParserAction.Continue);
            bpcmd.ErrorActionChanged = null;
            result = bpcmd.OnError(null, action);
            Assert.NotEqual(result, BatchParserAction.Continue);
        }

        [Fact]
        public void CheckConnectionChangedDelegate()
        {
            var initial = bpcmd.ConnectionChanged;
            bpcmd.ConnectionChanged = null;
            Assert.Null(bpcmd.ConnectionChanged);
        }

        [Fact]
        public void CheckVariableSubstitutionDisabled()
        {
            bpcmd.DisableVariableSubstitution();
            bpcmd.SetVariable(testPOS, "variable1", "test");
            var result = bpcmd.GetVariable(testPOS, "variable1");
            Assert.Null(result);
        }

    }
}
