//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using NUnit.Framework;

namespace Microsoft.SqlTools.ManagedBatchParser.UnitTests.BatchParser
{
    [TestFixture]
    public class BatchParserWrapperTests
    {
        [Test]
        public void CheckSimpleSingleSQLBatchStatement()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "select * from sys.objects";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.AreEqual(1, batches.Count);
                BatchDefinition batch = batches[0];
                Assert.AreEqual(sqlScript, batch.BatchText);
                Assert.AreEqual(1, batch.StartLine);
                Assert.AreEqual(1, batch.StartColumn);
                Assert.AreEqual(2, batch.EndLine);
                Assert.AreEqual(sqlScript.Length + 1, batch.EndColumn);
                Assert.AreEqual(1, batch.BatchExecutionCount);
            }
        }

        [Test]
        public void CheckSimpleMultipleQLBatchStatement()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = @"SELECT 'FirstLine';
                    GO
                    SELECT 'MiddleLine_1';
                    GO
                    SELECT 'MiddleLine_1'
                    GO
                    SELECT 'LastLine'";
                var batches = parserWrapper.GetBatches(sqlScript);
                // Each select statement is one batch , so we are expecting 4 batches.
                Assert.AreEqual(4, batches.Count);
            }
        }

        [Test]
        public void CheckSQLBatchStatementWithRepeatExecution()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "select * from sys.object" + Environment.NewLine + "GO 2";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.AreEqual(1, batches.Count);
                BatchDefinition batch = batches[0];
                Assert.AreEqual(2, batch.BatchExecutionCount);
            }
        }

        [Test]
        public void CheckComment()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "-- this is a comment --";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.AreEqual(1, batches.Count);
                BatchDefinition batch = batches[0];
                Assert.AreEqual(sqlScript, batch.BatchText);
                Assert.AreEqual(1, batch.StartLine);
                Assert.AreEqual(1, batch.StartColumn);
                Assert.AreEqual(2, batch.EndLine);
                Assert.AreEqual(sqlScript.Length + 1, batch.EndColumn);
            }
        }

        [Test]
        public void CheckNoOps()
        {
            using (BatchParserWrapper parserWrapper = new BatchParserWrapper())
            {
                string sqlScript = "GO";
                var batches = parserWrapper.GetBatches(sqlScript);
                Assert.AreEqual(0, batches.Count);
            }
        }
    }
}