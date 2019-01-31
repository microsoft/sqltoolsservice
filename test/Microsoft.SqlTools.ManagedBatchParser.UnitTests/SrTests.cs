//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using Xunit;
using ManagedBatchParserSr = Microsoft.SqlTools.ManagedBatchParser.SR;

namespace Microsoft.SqlTools.ManagedBatchParser.LocalizationTests
{
    public class SrTests
    {
        [Fact]
        public void StringResourcePropertiesTest()
        {
            ManagedBatchParserSr.Culture = CultureInfo.CurrentCulture;
            Assert.NotNull(ManagedBatchParserSr.BatchParser_CircularReference);
            Assert.NotNull(ManagedBatchParserSr.BatchParser_CommentNotTerminated);
            Assert.NotNull(ManagedBatchParserSr.BatchParser_IncorrectSyntax);
            Assert.NotNull(ManagedBatchParserSr.BatchParser_StringNotTerminated);
            Assert.NotNull(ManagedBatchParserSr.BatchParser_VariableNotDefined);
            Assert.NotNull(ManagedBatchParserSr.BatchParserWrapperExecutionEngineBatchCancelling);
            Assert.NotNull(ManagedBatchParserSr.BatchParserWrapperExecutionEngineBatchMessage);
            Assert.NotNull(ManagedBatchParserSr.BatchParserWrapperExecutionEngineBatchResultSetFinished);
            Assert.NotNull(ManagedBatchParserSr.BatchParserWrapperExecutionEngineBatchResultSetProcessing);
            Assert.NotNull(ManagedBatchParserSr.BatchParserWrapperExecutionEngineError);
            Assert.NotNull(ManagedBatchParserSr.EE_BatchError_Exception);
            Assert.NotNull(ManagedBatchParserSr.EE_BatchExecutionError_Halting);
            Assert.NotNull(ManagedBatchParserSr.EE_BatchExecutionError_Ignoring);
            Assert.NotNull(ManagedBatchParserSr.EE_BatchExecutionInfo_RowsAffected);
            Assert.NotNull(ManagedBatchParserSr.EE_BatchSqlMessageNoLineInfo);
            Assert.NotNull(ManagedBatchParserSr.EE_BatchSqlMessageNoProcedureInfo);
            Assert.NotNull(ManagedBatchParserSr.EE_BatchSqlMessageWithProcedureInfo);
            Assert.NotNull(ManagedBatchParserSr.EE_ExecutionError_CommandNotSupported);
            Assert.NotNull(ManagedBatchParserSr.EE_ExecutionError_VariableNotFound);
            Assert.NotNull(ManagedBatchParserSr.EE_ExecutionInfo_FinalizingLoop);
            Assert.NotNull(ManagedBatchParserSr.EE_ExecutionInfo_InitializingLoop);
            Assert.NotNull(ManagedBatchParserSr.EE_ExecutionInfo_QueryCancelledbyUser);
            Assert.NotNull(ManagedBatchParserSr.EE_ExecutionNotYetCompleteError);
            Assert.NotNull(ManagedBatchParserSr.EE_ScriptError_Error);
            Assert.NotNull(ManagedBatchParserSr.EE_ScriptError_FatalError);
            Assert.NotNull(ManagedBatchParserSr.EE_ScriptError_ParsingSyntax);
            Assert.NotNull(ManagedBatchParserSr.EE_ScriptError_Warning);
        }
    }
}