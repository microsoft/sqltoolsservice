﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    public class CompletionServiceTest
    {
        // Disable flaky test (mairvine - 3/15/2018)
        // [Test]
        public void CompletionItemsShouldCreatedUsingSqlParserIfTheProcessDoesNotTimeout()
        {
            ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();
            ScriptDocumentInfo docInfo = CreateScriptDocumentInfo();
            CompletionService completionService = new CompletionService(bindingQueue);
            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, null);
            bool useLowerCaseSuggestions = true;
            CompletionItem[] defaultCompletionList = AutoCompleteHelper.GetDefaultCompletionItems(docInfo, useLowerCaseSuggestions);

            List<Declaration> declarations = new List<Declaration>();

            var sqlParserWrapper = new Mock<ISqlParserWrapper>();
            sqlParserWrapper.Setup(x => x.FindCompletions(docInfo.ScriptParseInfo.ParseResult, docInfo.ParserLine, docInfo.ParserColumn, 
                It.IsAny<IMetadataDisplayInfoProvider>())).Returns(declarations);
            completionService.SqlParserWrapper = sqlParserWrapper.Object;

            AutoCompletionResult result = completionService.CreateCompletions(connectionInfo, docInfo, useLowerCaseSuggestions);
            Assert.NotNull(result);
            var count = result.CompletionItems == null ? 0 : result.CompletionItems.Count();

            Assert.That(count, Is.Not.EqualTo(defaultCompletionList.Count()));
        }

        [Test]
        public void CompletionItemsShouldCreatedUsingDefaultListIfTheSqlParserProcessTimesout()
        {
            ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();
            ScriptDocumentInfo docInfo = CreateScriptDocumentInfo();
            CompletionService completionService = new CompletionService(bindingQueue);
            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, null);
            bool useLowerCaseSuggestions = true;
            List<Declaration> declarations = new List<Declaration>();
            CompletionItem[] defaultCompletionList = AutoCompleteHelper.GetDefaultCompletionItems(docInfo, useLowerCaseSuggestions);

            var sqlParserWrapper = new Mock<ISqlParserWrapper>();
            sqlParserWrapper.Setup(x => x.FindCompletions(docInfo.ScriptParseInfo.ParseResult, docInfo.ParserLine, docInfo.ParserColumn,
                It.IsAny<IMetadataDisplayInfoProvider>())).Callback(() => Thread.Sleep(LanguageService.BindingTimeout + 100)).Returns(declarations);
            completionService.SqlParserWrapper = sqlParserWrapper.Object;

            AutoCompletionResult result = completionService.CreateCompletions(connectionInfo, docInfo, useLowerCaseSuggestions);
            Assert.NotNull(result);
            Assert.AreEqual(result.CompletionItems.Count(), defaultCompletionList.Count());
            Thread.Sleep(3000);
            Assert.True(connectionInfo.IntellisenseMetrics.Quantile.Any());
        }

        private ScriptDocumentInfo CreateScriptDocumentInfo()
        {
            TextDocumentPosition doc = new TextDocumentPosition()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = "script file"
                },
                Position = new Position()
                {
                    Line = 1,
                    Character = 14
                }
            };
            ScriptFile scriptFile = new ScriptFile()
            {
                Contents = "Select * from sys.all_objects"
            };

            ScriptParseInfo scriptParseInfo = new ScriptParseInfo() { IsConnected = true };
            ScriptDocumentInfo docInfo = new ScriptDocumentInfo(doc, scriptFile, scriptParseInfo);

            return docInfo;
        }
    }
}
