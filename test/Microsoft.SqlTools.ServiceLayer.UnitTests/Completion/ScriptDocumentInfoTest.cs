//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Completion
{
    [TestFixture]
    public class ScriptDocumentInfoTest
    {
        [Test]
        public void MetricsShouldGetSortedGivenUnSortedArray()
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

            ScriptParseInfo scriptParseInfo = new ScriptParseInfo();
            ScriptDocumentInfo docInfo = new ScriptDocumentInfo(doc, scriptFile, scriptParseInfo);

            Assert.AreEqual(1, docInfo.StartLine);
            Assert.AreEqual(2, docInfo.ParserLine);
            Assert.AreEqual(44, docInfo.StartColumn);
            Assert.AreEqual(14, docInfo.EndColumn);
            Assert.AreEqual(15, docInfo.ParserColumn);
        }
    }
}
