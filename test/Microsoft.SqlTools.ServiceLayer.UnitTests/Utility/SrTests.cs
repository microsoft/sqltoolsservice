//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using Xunit;

using ServiceLayerSr = Microsoft.SqlTools.ServiceLayer.SR;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public class SrTests
    {
        /// <summary>
        /// Add and remove and item in a LongList
        /// </summary>
        [Fact]
        public void SrPropertiesTest()
        {
            ServiceLayerSr.Culture = CultureInfo.CurrentCulture;

            // Assert all needed constants exist 
            Assert.NotNull(ServiceLayerSr.QueryServiceSubsetBatchNotCompleted);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperWriteOnly);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperNotInitialized);
            Assert.NotNull(ServiceLayerSr.QueryServiceColumnNull);
            Assert.NotNull(ServiceLayerSr.Culture);
            Assert.NotNull(ServiceLayerSr.BatchParser_CircularReference);
            Assert.NotNull(ServiceLayerSr.BatchParser_CommentNotTerminated);
            Assert.NotNull(ServiceLayerSr.BatchParser_IncorrectSyntax);
            Assert.NotNull(ServiceLayerSr.BatchParser_StringNotTerminated);
            Assert.NotNull(ServiceLayerSr.BatchParser_VariableNotDefined);
            Assert.NotNull(ServiceLayerSr.BatchParserWrapperExecutionEngineBatchCancelling);
            Assert.NotNull(ServiceLayerSr.BatchParserWrapperExecutionEngineBatchMessage);
            Assert.NotNull(ServiceLayerSr.BatchParserWrapperExecutionEngineBatchResultSetFinished);
            Assert.NotNull(ServiceLayerSr.BatchParserWrapperExecutionEngineBatchResultSetProcessing);
            Assert.NotNull(ServiceLayerSr.BatchParserWrapperExecutionEngineError);
            Assert.NotNull(ServiceLayerSr.BatchParserWrapperExecutionError);
            Assert.NotNull(ServiceLayerSr.ConnectionParamsValidateNullConnection);
            Assert.NotNull(ServiceLayerSr.ConnectionParamsValidateNullOwnerUri);
            Assert.NotNull(ServiceLayerSr.ConnectionParamsValidateNullServerName);
            Assert.NotNull(ServiceLayerSr.ConnectionParamsValidateNullSqlAuth(""));
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnectErrorNullParams);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnectionCanceled);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnStringInvalidAuthType(""));
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnStringInvalidIntent(""));
            Assert.NotNull(ServiceLayerSr.ConnectionServiceDbErrorDefaultNotConnected(""));
            Assert.NotNull(ServiceLayerSr.ConnectionServiceListDbErrorNotConnected(""));
            Assert.NotNull(ServiceLayerSr.ConnectionServiceListDbErrorNullOwnerUri);
            Assert.NotNull(ServiceLayerSr.EE_BatchError_Exception);
            Assert.NotNull(ServiceLayerSr.EE_BatchExecutionError_Halting);
            Assert.NotNull(ServiceLayerSr.EE_BatchExecutionError_Ignoring);
            Assert.NotNull(ServiceLayerSr.EE_BatchExecutionInfo_RowsAffected);
            Assert.NotNull(ServiceLayerSr.EE_BatchSqlMessageNoLineInfo);
            Assert.NotNull(ServiceLayerSr.EE_BatchSqlMessageNoProcedureInfo);
            Assert.NotNull(ServiceLayerSr.EE_BatchSqlMessageWithProcedureInfo);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionError_CommandNotSupported);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionError_VariableNotFound);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionInfo_FinalizingLoop);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionInfo_InitializingLoop);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionInfo_QueryCancelledbyUser);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionNotYetCompleteError);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_Error);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_FatalError);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_ParsingSyntax);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_Warning);
            Assert.NotNull(ServiceLayerSr.ErrorEmptyStringReplacement);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionAzureError(""));
            Assert.NotNull(ServiceLayerSr.PeekDefinitionDatabaseError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionError(""));
            Assert.NotNull(ServiceLayerSr.PeekDefinitionNoResultsError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionNotConnectedError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionTimedoutError);
            Assert.NotNull(ServiceLayerSr.QueryServiceAffectedOneRow);
            Assert.NotNull(ServiceLayerSr.QueryServiceAffectedRows(0));
            Assert.NotNull(ServiceLayerSr.QueryServiceCancelAlreadyCompleted);
            Assert.NotNull(ServiceLayerSr.QueryServiceCancelDisposeFailed);
            Assert.NotNull(ServiceLayerSr.QueryServiceColumnNull);
            Assert.NotNull(ServiceLayerSr.QueryServiceCompletedSuccessfully);
            Assert.NotNull(ServiceLayerSr.QueryServiceDataReaderByteCountInvalid);
            Assert.NotNull(ServiceLayerSr.QueryServiceDataReaderCharCountInvalid);
            Assert.NotNull(ServiceLayerSr.QueryServiceDataReaderXmlCountInvalid);
            Assert.NotNull(ServiceLayerSr.QueryServiceErrorFormat(0,0,0,0,"",""));
            Assert.NotNull(ServiceLayerSr.QueryServiceExecutionPlanNotFound);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperNotInitialized);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperReadOnly);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperWriteOnly);
            Assert.NotNull(ServiceLayerSr.QueryServiceMessageSenderNotSql);
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryCancelled);
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryFailed(""));
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryInProgress);
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryInvalidOwnerUri);
            Assert.NotNull(ServiceLayerSr.QueryServiceRequestsNoQuery);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetNoColumnSchema);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetNotRead);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetRowCountOutOfRange);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetStartRowOutOfRange);
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsFail("",""));
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsInProgress);
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsMiscStartingError);
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsResultSetNotComplete);
            Assert.NotNull(ServiceLayerSr.QueryServiceSubsetBatchNotCompleted);
            Assert.NotNull(ServiceLayerSr.QueryServiceSubsetBatchOutOfRange);
            Assert.NotNull(ServiceLayerSr.QueryServiceSubsetResultSetOutOfRange);
            Assert.NotNull(ServiceLayerSr.TestLocalizationConstant);
            Assert.NotNull(ServiceLayerSr.TroubleshootingAssistanceMessage);
            Assert.NotNull(ServiceLayerSr.WorkspaceServiceBufferPositionOutOfOrder(0,0,0,0));
            Assert.NotNull(ServiceLayerSr.WorkspaceServicePositionColumnOutOfRange(0));
            Assert.NotNull(ServiceLayerSr.WorkspaceServicePositionLineOutOfRange);
        }
    }
}
