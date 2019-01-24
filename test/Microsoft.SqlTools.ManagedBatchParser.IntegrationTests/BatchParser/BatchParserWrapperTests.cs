using System;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Xunit;

namespace Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.BatchParser
{
    public class BatchParserWrapperTests
    {
        [Fact]
        public void CheckSimpleSingleSQLBatchStatement()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "select * from sys.objects";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.Equal(1, batches.Count);
                BatchDefinition batch = batches[0];
                Assert.Equal(sqlScript, batch.BatchText);
                Assert.Equal(1, batch.StartLine);
                Assert.Equal(1, batch.StartColumn);
                Assert.Equal(2, batch.EndLine);
                Assert.Equal(sqlScript.Length+1, batch.EndColumn);
                Assert.Equal(1, batch.BatchExecutionCount);
            }
        }

        [Fact]
        public void CheckSQLBatchStatementWithRepeatExecution()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "select * from sys.object" + Environment.NewLine + "GO 2";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.Equal(1, batches.Count);
                BatchDefinition batch = batches[0];
                Assert.Equal(2, batch.BatchExecutionCount);
            }
        }

        [Fact]
        public void CheckComment()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "-- this is a comment --";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.Equal(1, batches.Count);
                BatchDefinition batch = batches[0];
                Assert.Equal(sqlScript, batch.BatchText);
                Assert.Equal(1, batch.StartLine);
                Assert.Equal(1, batch.StartColumn);
                Assert.Equal(2, batch.EndLine);
                Assert.Equal(sqlScript.Length+1, batch.EndColumn);
            }
        }

        [Fact]
        public void CheckNoOps()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "GO";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.Equal(0, batches.Count);
            }
        }
    }
}
