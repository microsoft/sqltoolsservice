using System.Linq;
using Kusto.Language.Editor;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource.DataSourceIntellisense
{
    public class KustoIntellisenseHelperTests
    {
        [TestCase(CompletionKind.Syntax, CompletionItemKind.Module)]
        [TestCase(CompletionKind.Column, CompletionItemKind.Field)]
        [TestCase(CompletionKind.Variable, CompletionItemKind.Variable)]
        [TestCase(CompletionKind.Table, CompletionItemKind.File)]
        [TestCase(CompletionKind.Database, CompletionItemKind.Method)]
        [TestCase(CompletionKind.LocalFunction, CompletionItemKind.Function)]
        [TestCase(CompletionKind.DatabaseFunction, CompletionItemKind.Function)]
        [TestCase(CompletionKind.BuiltInFunction, CompletionItemKind.Function)]
        [TestCase(CompletionKind.AggregateFunction, CompletionItemKind.Function)]
        [TestCase(CompletionKind.Unknown, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.Keyword, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.Punctuation, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.Identifier, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.Example, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.ScalarPrefix, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.TabularPrefix, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.TabularSuffix, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.QueryPrefix, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.CommandPrefix, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.ScalarInfix, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.RenderChart, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.Parameter, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.Cluster, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.MaterialiedView, CompletionItemKind.Keyword)]
        [TestCase(CompletionKind.ScalarType, CompletionItemKind.Keyword)]
        public void CreateCompletionItemKind_Returns_Kind(CompletionKind completionKind, CompletionItemKind expected)
        {
            var result = KustoIntellisenseHelper.CreateCompletionItemKind(completionKind);
            Assert.AreEqual(expected, result);
        }

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
            
            Assert.AreEqual(6, completionItems.Length);
        }
    }
}