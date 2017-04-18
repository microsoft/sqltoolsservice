//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using Xunit;

using ServiceLayerSr = Microsoft.SqlTools.ServiceLayer.Localization.sr;
using HostingSr = Microsoft.SqlTools.Hosting.Localization.sr;
using CredentialSr = Microsoft.SqlTools.Credentials.Localization.sr;

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
            Assert.NotNull(SR.QueryServiceSubsetBatchNotCompleted);
            Assert.NotNull(SR.QueryServiceFileWrapperWriteOnly);
            Assert.NotNull(SR.QueryServiceFileWrapperNotInitialized);
            Assert.NotNull(SR.QueryServiceColumnNull);

            var serviceLayerSr = new ServiceLayerSr();
            var hostingSr = new HostingSr();
            var credentialSr = new CredentialSr();
            Assert.NotNull(ServiceLayerSr.ResourceManager);
            ServiceLayerSr.Culture = CultureInfo.CurrentCulture;
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
            Assert.NotNull(ServiceLayerSr.ConnectionParamsValidateNullSqlAuth);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnectErrorNullParams);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnectionCanceled);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnStringInvalidAuthType);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceConnStringInvalidIntent);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceDbErrorDefaultNotConnected);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceListDbErrorNotConnected);
            Assert.NotNull(ServiceLayerSr.ConnectionServiceListDbErrorNullOwnerUri);
            Assert.Null(CredentialSr.CredentialServiceWin32CredentialDisposed);
            Assert.Null(CredentialSr.CredentialsServiceInvalidCriticalHandle);
            Assert.Null(CredentialSr.CredentialsServicePasswordLengthExceeded);
            Assert.Null(CredentialSr.CredentialsServiceTargetForDelete);
            Assert.Null(CredentialSr.CredentialsServiceTargetForLookup);
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
            Assert.NotNull(ServiceLayerSr.EE_ExecutionInfo_InitilizingLoop);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionInfo_QueryCancelledbyUser);
            Assert.NotNull(ServiceLayerSr.EE_ExecutionNotYetCompleteError);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_Error);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_FatalError);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_ParsingSyntax);
            Assert.NotNull(ServiceLayerSr.EE_ScriptError_Warning);
            Assert.NotNull(ServiceLayerSr.ErrorEmptyStringReplacement);
            Assert.Null(ServiceLayerSr.HostingHeaderMissingColon);
            Assert.Null(ServiceLayerSr.HostingHeaderMissingContentLengthHeader);
            Assert.Null(ServiceLayerSr.HostingHeaderMissingContentLengthValue);
            Assert.Null(ServiceLayerSr.HostingUnexpectedEndOfStream);
            Assert.Null(ServiceLayerSr.IncompatibleServiceForExtensionLoader);
            Assert.Null(ServiceLayerSr.MultipleServicesFound);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionAzureError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionDatabaseError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionNoResultsError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionNotConnectedError);
            Assert.NotNull(ServiceLayerSr.PeekDefinitionTimedoutError);
            Assert.NotNull(ServiceLayerSr.QueryServiceAffectedOneRow);
            Assert.NotNull(ServiceLayerSr.QueryServiceAffectedRows);
            Assert.NotNull(ServiceLayerSr.QueryServiceCancelAlreadyCompleted);
            Assert.NotNull(ServiceLayerSr.QueryServiceCancelDisposeFailed);
            Assert.NotNull(ServiceLayerSr.QueryServiceColumnNull);
            Assert.NotNull(ServiceLayerSr.QueryServiceCompletedSuccessfully);
            Assert.NotNull(ServiceLayerSr.QueryServiceDataReaderByteCountInvalid);
            Assert.NotNull(ServiceLayerSr.QueryServiceDataReaderCharCountInvalid);
            Assert.NotNull(ServiceLayerSr.QueryServiceDataReaderXmlCountInvalid);
            Assert.NotNull(ServiceLayerSr.QueryServiceErrorFormat);
            Assert.NotNull(ServiceLayerSr.QueryServiceExecutionPlanNotFound);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperNotInitialized);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperReadOnly);
            Assert.NotNull(ServiceLayerSr.QueryServiceFileWrapperWriteOnly);
            Assert.NotNull(ServiceLayerSr.QueryServiceMessageSenderNotSql);
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryCancelled);
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryFailed);
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryInProgress);
            Assert.NotNull(ServiceLayerSr.QueryServiceQueryInvalidOwnerUri);
            Assert.NotNull(ServiceLayerSr.QueryServiceRequestsNoQuery);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetNoColumnSchema);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetNotRead);
            Assert.Null(ServiceLayerSr.QueryServiceResultSetReaderNull);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetRowCountOutOfRange);
            Assert.NotNull(ServiceLayerSr.QueryServiceResultSetStartRowOutOfRange);
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsFail);
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsInProgress);
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsMiscStartingError);
            Assert.NotNull(ServiceLayerSr.QueryServiceSaveAsResultSetNotComplete);
            Assert.NotNull(ServiceLayerSr.QueryServiceSubsetBatchNotCompleted);
            Assert.NotNull(ServiceLayerSr.QueryServiceSubsetBatchOutOfRange);
            Assert.NotNull(ServiceLayerSr.QueryServiceSubsetResultSetOutOfRange);
            Assert.Null(ServiceLayerSr.ServiceAlreadyRegistered);
            Assert.Null(ServiceLayerSr.ServiceNotFound);
            Assert.Null(ServiceLayerSr.ServiceNotOfExpectedType);
            Assert.Null(ServiceLayerSr.ServiceProviderNotSet);
            Assert.NotNull(ServiceLayerSr.TestLocalizationConstant);
            Assert.NotNull(ServiceLayerSr.TroubleshootingAssistanceMessage);
            Assert.NotNull(ServiceLayerSr.WorkspaceServiceBufferPositionOutOfOrder);
            Assert.NotNull(ServiceLayerSr.WorkspaceServicePositionColumnOutOfRange);
            Assert.NotNull(ServiceLayerSr.WorkspaceServicePositionLineOutOfRange);
        }
    }
}
