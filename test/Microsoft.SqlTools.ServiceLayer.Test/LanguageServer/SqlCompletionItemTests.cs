//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServer
{
    public class SqlCompletionItemTests
    {
        [Fact]
        public void InsertTextShouldIncludeBracketGivenNameWithSpace()
        {
            string declarationTitle = "name with space";
            string expected = "[" + declarationTitle + "]";

            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.True(completionItem.InsertText.StartsWith("[") && completionItem.InsertText.EndsWith("]"));
        }

        [Fact]
        public void InsertTextShouldIncludeBracketGivenNameWithSpecialCharacter()
        {
            string declarationTitle = "name @";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, declarationTitle);
            Assert.Equal(completionItem.Label, declarationTitle);
        }

        [Fact]
        public void LabelShouldIncludeBracketGivenTokenWithBracket()
        {
            string declarationTitle = "name";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Fact]
        public void LabelShouldIncludeBracketGivenTokenWithBrackets()
        {
            string declarationTitle = "name";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[]";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Fact]
        public void LabelShouldNotIncludeBracketGivenNameWithBrackets()
        {
            string declarationTitle = "[name]";
            string expected = declarationTitle;
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[]";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Fact]
        public void LabelShouldIncludeBracketGivenNameWithOneBracket()
        {
            string declarationTitle = "[name";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[]";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Fact]
        public void KindShouldBeModuleGivenSchemaDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Module;
            DeclarationType declarationType = DeclarationType.Schema;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Fact]
        public void KindShouldBeFieldGivenColumnDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Field;
            DeclarationType declarationType = DeclarationType.Column;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Fact]
        public void KindShouldBeFileGivenTableDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.File;
            DeclarationType declarationType = DeclarationType.Table;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Fact]
        public void KindShouldBeFileGivenViewDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.File;
            DeclarationType declarationType = DeclarationType.View;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Fact]
        public void KindShouldBeMethodGivenDatabaseDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Method;
            DeclarationType declarationType = DeclarationType.Database;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Fact]
        public void KindShouldBeValueGivenScalarValuedFunctionDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Value;
            DeclarationType declarationType = DeclarationType.ScalarValuedFunction;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Fact]
        public void KindShouldBeValueGivenTableValuedFunctionDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Value;
            DeclarationType declarationType = DeclarationType.TableValuedFunction;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Fact]
        public void KindShouldBeUnitGivenUnknownDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Unit;
            DeclarationType declarationType = DeclarationType.XmlIndex;
            ValidateDeclarationType(declarationType, expectedType);
        }

        private void ValidateDeclarationType(DeclarationType declarationType, CompletionItemKind expectedType)
        {
            string declarationTitle = "name";
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);


            Assert.Equal(completionItem.Kind, expectedType);
        }
    }
}
