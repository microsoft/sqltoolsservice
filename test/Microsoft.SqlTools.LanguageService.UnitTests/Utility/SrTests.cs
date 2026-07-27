//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Globalization;
using NUnit.Framework;

using LanguageServiceSr = Microsoft.SqlTools.LanguageService.SR;

namespace Microsoft.SqlTools.LanguageService.UnitTests.Utility
{
    public class SrTests
    {
        [Test]
        public void SrPropertiesTest()
        {
            LanguageServiceSr.Culture = CultureInfo.CurrentCulture;

            Assert.NotNull(LanguageServiceSr.Culture);

            // Workspace Service
            Assert.NotNull(LanguageServiceSr.WorkspaceServiceBufferPositionOutOfOrder(0, 0, 0, 0));
            Assert.NotNull(LanguageServiceSr.WorkspaceServicePositionLineOutOfRange);
            Assert.NotNull(LanguageServiceSr.WorkspaceServicePositionColumnOutOfRange(0));

            // Formatter
            Assert.NotNull(LanguageServiceSr.ErrorEmptyStringReplacement);

            // Completion - Star Expansion
            Assert.NotNull(LanguageServiceSr.StarExpansionLabel(""));
            Assert.NotNull(LanguageServiceSr.StarExpansionDescription("", "", ""));

            // Connection - Connection String Building
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidAuthType(""));
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidColumnEncryptionSetting(""));
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidSecureEnclaves(""));
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidEncryptOption(""));
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidEnclaveAttestationProtocol(""));
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringMissingAttestationProtocolWithSecureEnclaves);
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringMissingAttestationUrlWithAttestationProtocol);
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidAlwaysEncryptedOptionCombination);
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidAttestationProtocolNoneWithUrl);
            Assert.NotNull(LanguageServiceSr.ConnectionServiceConnStringInvalidIntent(""));

            // Scripting - Peek Definition
            Assert.NotNull(LanguageServiceSr.PeekDefinitionAzureError(""));
            Assert.NotNull(LanguageServiceSr.PeekDefinitionError(""));
            Assert.NotNull(LanguageServiceSr.PeekDefinitionNoResultsError);
            Assert.NotNull(LanguageServiceSr.PeekDefinitionDatabaseError);
            Assert.NotNull(LanguageServiceSr.PeekDefinitionTypeNotSupportedError);
            Assert.NotNull(LanguageServiceSr.PeekDefinitionNotConnectedError);
            Assert.NotNull(LanguageServiceSr.PeekDefinitionTimedoutError);

            // AutoParameterization for Always Encrypted
            Assert.NotNull(LanguageServiceSr.ParameterizationDetails("", "", 0, 0, 0, ""));
            Assert.NotNull(LanguageServiceSr.ErrorMessageHeader(0));
            Assert.NotNull(LanguageServiceSr.ErrorMessage("", "", ""));
            Assert.NotNull(LanguageServiceSr.DateTimeErrorMessage("", "", ""));
            Assert.NotNull(LanguageServiceSr.BinaryLiteralPrefixMissingError("", "", ""));
            Assert.NotNull(LanguageServiceSr.ParsingErrorHeader(0, 0));
            Assert.NotNull(LanguageServiceSr.ScriptTooLarge(0, 0));
        }
    }
}
