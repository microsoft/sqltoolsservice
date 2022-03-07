//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Kusto;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource.DataSourceIntellisense
{
    public class KustoIntellisenseHelperTests
    {
        [Test]
        public void GetDefaultKeywords_Returns_Keywords()
        {
            var textDocumentPosition = new TextDocumentPosition
            {
                Position = new Position()
            };
            var scriptFile = new ScriptFile("", "", "");
            var scriptParseInfo = new ScriptParseInfo();
            var documentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);
            var position = new Position();

            var completionItems = KustoIntellisenseHelper.GetDefaultKeywords(documentInfo, position);
            Assert.AreEqual(13, completionItems.Length);
        }

        [Test]
        public void GetDefaultDiagnostics_Returns_Diagnostics()
        {
            var parseInfo = new ScriptParseInfo();
            var scriptFile = new ScriptFile("", "", "");
            var queryText = ".show databases";
            var completionItems = KustoIntellisenseHelper.GetDefaultDiagnostics(parseInfo, scriptFile, queryText);
            
            Assert.AreEqual(0, completionItems.Length);
        }
    }
}