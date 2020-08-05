//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using NUnit.Framework;

namespace Microsoft.SqlTools.ManagedBatchParser.UnitTests.BatchParser
{
    [TestFixture]
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

        [Test]
        public void CheckSetVariable()
        {
            Assert.AreEqual(3, bpcmd.InternalVariables.Count);
            bpcmd.SetVariable(testPOS, "variable4", "test4");
            bpcmd.SetVariable(testPOS, "variable5", "test5");
            bpcmd.SetVariable(testPOS, "variable6", "test6");
            Assert.AreEqual(6, bpcmd.InternalVariables.Count);
        }

        [Test]
        public void CheckSetNullValueVariable()
        {
            Assert.AreEqual(3, bpcmd.InternalVariables.Count);
            bpcmd.SetVariable(testPOS, "variable4", "test4");
            Assert.AreEqual(4, bpcmd.InternalVariables.Count);
            bpcmd.SetVariable(testPOS, "variable4", null);
            Assert.AreEqual(3, bpcmd.InternalVariables.Count);
        }

        [Test]
        public void CheckGetVariable()
        {
            string value = bpcmd.GetVariable(testPOS, "variable1");
            Assert.AreEqual("test1", value);
            value = bpcmd.GetVariable(testPOS, "variable2");
            Assert.AreEqual("test2", value);
            value = bpcmd.GetVariable(testPOS, "variable3");
            Assert.AreEqual("test3", value);
        }

        [Test]
        public void CheckGetNullVariable()
        {
            Assert.Null(bpcmd.GetVariable(testPOS, "variable6"));
        }

        [Test]
        public void CheckInclude()
        {
            TextReader textReader = null;
            string outString = "out";
            var result = bpcmd.Include(null, out textReader, out outString);
            Assert.AreEqual(BatchParserAction.Abort, result);
        }

        [Test]
        public void CheckOnError()
        {
            var errorActionChanged = bpcmd.ErrorActionChanged;
            var action = new OnErrorAction();
            var result = bpcmd.OnError(null, action);
            Assert.AreEqual(BatchParserAction.Continue, result);
        }

        [Test]
        public void CheckConnectionChangedDelegate()
        {
            var initial = bpcmd.ConnectionChanged;
            bpcmd.ConnectionChanged = null;
            Assert.Null(bpcmd.ConnectionChanged);
        }

        [Test]
        public void CheckVariableSubstitutionDisabled()
        {
            bpcmd.DisableVariableSubstitution();
            bpcmd.SetVariable(testPOS, "variable1", "test");
            var result = bpcmd.GetVariable(testPOS, "variable1");
            Assert.Null(result);
        }
    }
}