//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.ServiceHost
{
    /// <summary>
    /// ScriptFile test case
    /// </summary>
    public class SrTests
    {
        /// <summary>
        /// Simple "test" to access string resources
        /// The purpose of this test is for code coverage.  It's probably better to just 
        /// exclude string resources in the code coverage report than maintain this test.
        /// </summary>
        [Fact]
        public void SrStringsTest()
        {
            var culture = SR.Culture;
            SR.Culture = culture;
            Assert.True(SR.Culture == culture);

            var connectionServiceListDbErrorNullOwnerUri = SR.ConnectionServiceListDbErrorNullOwnerUri;
            var connectionParamsValidateNullConnection = SR.ConnectionParamsValidateNullConnection;
            var credentialsServiceInvalidCriticalHandle = SR.CredentialsServiceInvalidCriticalHandle;
            var credentialsServicePasswordLengthExceeded = SR.CredentialsServicePasswordLengthExceeded;
            var credentialsServiceTargetForDelete = SR.CredentialsServiceTargetForDelete;
            var credentialsServiceTargetForLookup = SR.CredentialsServiceTargetForLookup;
            var queryServiceCancelDisposeFailed = SR.QueryServiceCancelDisposeFailed;
            var queryServiceQueryCancelled = SR.QueryServiceQueryCancelled;
            var queryServiceDataReaderByteCountInvalid = SR.QueryServiceDataReaderByteCountInvalid;
            var queryServiceDataReaderCharCountInvalid = SR.QueryServiceDataReaderCharCountInvalid;
            var queryServiceDataReaderXmlCountInvalid = SR.QueryServiceDataReaderXmlCountInvalid;
            var queryServiceFileWrapperReadOnly = SR.QueryServiceFileWrapperReadOnly;
            var queryServiceAffectedOneRow = SR.QueryServiceAffectedOneRow;
            var queryServiceMessageSenderNotSql = SR.QueryServiceMessageSenderNotSql;
            var queryServiceResultSetNotRead = SR.QueryServiceResultSetNotRead;
            var queryServiceResultSetNoColumnSchema = SR.QueryServiceResultSetNoColumnSchema;
            var connectionServiceListDbErrorNotConnected = SR.ConnectionServiceListDbErrorNotConnected("..");
            var connectionServiceConnStringInvalidAuthType = SR.ConnectionServiceConnStringInvalidAuthType("..");
            var connectionServiceConnStringInvalidIntent = SR.ConnectionServiceConnStringInvalidIntent("..");
            var queryServiceAffectedRows = SR.QueryServiceAffectedRows(10);
            var queryServiceErrorFormat = SR.QueryServiceErrorFormat(1, 1, 1, 1, "\n", "..");
            var queryServiceQueryFailed = SR.QueryServiceQueryFailed("..");
            var workspaceServiceBufferPositionOutOfOrder = SR.WorkspaceServiceBufferPositionOutOfOrder(1, 2, 3, 4);
        }
    }
}
