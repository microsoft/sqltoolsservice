//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
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

        [Fact]
        public void SrStringsTestWithEnLocalization()
        {
            string locale = "en";
            var args = new string[] { "--locale " + locale };
            CommandOptions options = new CommandOptions(args);
            Assert.Equal(SR.Culture.Name, options.Locale);
            Assert.Equal(options.Locale, locale);

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.Equal(TestLocalizationConstant, "EN_LOCALIZATION");
        }

        [Fact]
        public void SrStringsTestWithEsLocalization()
        {
            string locale = "es";
            var args = new string[] { "--locale " + locale };
            CommandOptions options = new CommandOptions(args);
            Assert.Equal(SR.Culture.Name, options.Locale);
            Assert.Equal(options.Locale, locale);

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.Equal(TestLocalizationConstant, "ES_LOCALIZATION");
        }

        [Fact]
        public void SrStringsTestWithNullLocalization()
        {
            SR.Culture = null;
            var args = new string[] { "" };
            CommandOptions options = new CommandOptions(args);
            Assert.Null(SR.Culture);
            Assert.Equal(options.Locale, "");

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.Equal(TestLocalizationConstant, "EN_LOCALIZATION");
        }
    }
}
