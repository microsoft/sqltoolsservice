using System;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Xunit;

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
    }
}
