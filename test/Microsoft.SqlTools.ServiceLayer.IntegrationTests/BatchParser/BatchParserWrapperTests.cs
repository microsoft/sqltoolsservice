using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.BatchParser
{
    public class BatchParserWrapperTests
    {
        private BatchParserWrapper bpw;
        public BatchParserWrapperTests()
        {
            bpw = new BatchParserWrapper();
        }

        [Fact]
        public void CheckSimpleSingleSQLBatchStatement()
        {
            string sqlScript = "select * from sys.objects";
            var batches = bpw.GetBatches(sqlScript);
            Assert.Equal(1, batches.Count);
            BatchDefinition batch = batches[0];
            Assert.Equal(sqlScript, batch.BatchText);
            Assert.Equal(1, batch.StartLine);
            Assert.Equal(1, batch.StartColumn);
            Assert.Equal(2, batch.EndLine);
            Assert.Equal(sqlScript.Length, batch.EndColumn);
        }

        [Fact]
        public void CheckComment()
        {
            string sqlScript = "-- this is a comment --";
            var batches = bpw.GetBatches(sqlScript);
            Assert.Equal(1, batches.Count);
            BatchDefinition batch = batches[0];
            Assert.Equal(sqlScript, batch.BatchText);
            Assert.Equal(1, batch.StartLine);
            Assert.Equal(1, batch.StartColumn);
            Assert.Equal(2, batch.EndLine);
            Assert.Equal(sqlScript.Length, batch.EndColumn);
        }

        [Fact]
        public void CheckNoOps()
        {
            string sqlScript = "GO";
            var batches = bpw.GetBatches(sqlScript);
            Assert.Equal(0, batches.Count);
        }
    }
}
