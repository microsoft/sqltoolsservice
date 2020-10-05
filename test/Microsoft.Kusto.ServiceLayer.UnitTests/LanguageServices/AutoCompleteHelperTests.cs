using System.Linq;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.LanguageServices
{
    public class AutoCompleteHelperTests
    {
        [Test]
        public void CreateCompletionItem_Returns_CompletionItem()
        {
            string label = "";
            string detail = "";
            string insertText = "";
            var itemKind = CompletionItemKind.Method;
            int row = 1;
            int startColumn = 2;
            int endColumn = 3;
            
            var completionItem = AutoCompleteHelper.CreateCompletionItem(label, detail, insertText, itemKind, row, startColumn, endColumn);
            
            Assert.IsNotNull(completionItem);
            Assert.AreEqual(label, completionItem.Label);
            Assert.AreEqual(itemKind, completionItem.Kind);
            Assert.AreEqual(detail, completionItem.Detail);
            Assert.AreEqual(insertText, completionItem.InsertText);
            
            Assert.IsNotNull(completionItem.TextEdit);
            Assert.AreEqual(insertText, completionItem.TextEdit.NewText);
            
            Assert.IsNotNull(completionItem.TextEdit.Range);
            
            Assert.IsNotNull(completionItem.TextEdit.Range.Start);
            Assert.AreEqual(row, completionItem.TextEdit.Range.Start.Line);
            Assert.AreEqual(startColumn, completionItem.TextEdit.Range.Start.Character);
            
            Assert.IsNotNull(completionItem.TextEdit.Range.End);
            Assert.AreEqual(row, completionItem.TextEdit.Range.End.Line);
            Assert.AreEqual(endColumn, completionItem.TextEdit.Range.End.Character);
        }

        [Test]
        public void ConvertQuickInfoToHover_Returns_Null_For_Null_QuickInfoText()
        {
            var hover = AutoCompleteHelper.ConvertQuickInfoToHover(null, "", 0, 0, 0);
            Assert.IsNull(hover);
        }

        [Test]
        public void ConvertQuickInfoToHover_Returns_Hover()
        {
            string quickInfoText = "";
            string language = "";
            int row = 0;
            int startColumn = 0;
            int endColumn = 0;
            
            var hover = AutoCompleteHelper.ConvertQuickInfoToHover(quickInfoText, language, row, startColumn, endColumn);
            
            Assert.IsNotNull(hover);
            
            Assert.AreEqual(1, hover.Contents.Length);
            var content = hover.Contents.First();
            Assert.AreEqual(language, content.Language);
            Assert.AreEqual(quickInfoText, content.Value);
            
            Assert.IsNotNull(hover.Range);
            Assert.IsNotNull(hover.Range.Value.Start);
            Assert.AreEqual(row, hover.Range.Value.Start.Line);
            Assert.AreEqual(startColumn, hover.Range.Value.Start.Character);
            
            Assert.IsNotNull(hover.Range.Value.End);
            Assert.AreEqual(row, hover.Range.Value.End.Line);
            Assert.AreEqual(endColumn, hover.Range.Value.End.Character);
        }
    }
}