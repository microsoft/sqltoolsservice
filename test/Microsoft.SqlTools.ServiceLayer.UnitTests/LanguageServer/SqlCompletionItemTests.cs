//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
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
        public void ConstructorShouldThrowExceptionGivenEmptyDeclarionType()
        {
            string declarationTitle = "";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            Assert.Throws<ArgumentException>(() => new SqlCompletionItem(declarationTitle, declarationType, tokenText));
        }

        [Fact]
        public void ConstructorShouldThrowExceptionGivenNullDeclarion()
        {
            string tokenText = "";
            Assert.Throws<ArgumentException>(() => new SqlCompletionItem(null, tokenText));
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
        public void LabelShouldIncludeBracketGivenSqlObjectNameWithBracket()
        {
            string declarationTitle = @"Bracket\[";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, declarationTitle);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, declarationTitle);
        }

        [Fact]
        public void LabelShouldIncludeBracketGivenSqlObjectNameWithBracketAndTokenWithBracket()
        {
            string declarationTitle = @"Bracket\[";
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
        public void LabelShouldIncludeQuotedIdentifiersGivenTokenWithQuotedIdentifier()
        {
            string declarationTitle = "name";
            string expected = "\"" + declarationTitle + "\"";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "\"";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Fact]
        public void LabelShouldIncludeQuotedIdentifiersGivenTokenWithQuotedIdentifiers()
        {
            string declarationTitle = "name";
            string expected = "\"" + declarationTitle + "\"";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "\"\"";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Fact]
        public void InsertTextShouldNotIncludeBracketGivenReservedName()
        {
            foreach (string word in AutoCompleteHelper.DefaultCompletionText)
            {
                string declarationTitle = word;
                DeclarationType declarationType = DeclarationType.ApplicationRole;
                string tokenText = "";
                SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
                CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

                Assert.Equal(completionItem.Label, word);
                Assert.Equal(completionItem.InsertText, word);
                Assert.Equal(completionItem.Detail, word);
            }
        }

        [Fact]
        public void LabelShouldNotIncludeBracketIfTokenIncludesQuotedIdentifiersGivenReservedName()
        {
            string declarationTitle = "User";
            string expected = "\"" + declarationTitle + "\"";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "\"";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Fact]
        public void LabelShouldNotIncludeDoubleBracketIfTokenIncludesBracketsGivenReservedName()
        {
            string declarationTitle = "User";
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
        public void TempTablesShouldNotBeEscaped()
        {
            string declarationTitle = "#TestTable";
            string expected = declarationTitle;
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(completionItem.Label, expected);
            Assert.Equal(completionItem.InsertText, expected);
            Assert.Equal(completionItem.Detail, expected);
        }

        [Theory]
        [InlineData(DeclarationType.BuiltInFunction)]
        [InlineData(DeclarationType.ScalarValuedFunction)]
        [InlineData(DeclarationType.TableValuedFunction)]
        public void FunctionsShouldHaveParenthesesAdded(DeclarationType declarationType)
        {
            foreach (string word in AutoCompleteHelper.DefaultCompletionText)
            {
                string declarationTitle = word;
                string tokenText = "";
                SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
                CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

                Assert.Equal(declarationTitle, completionItem.Label);
                Assert.Equal($"{declarationTitle}()", completionItem.InsertText);
                Assert.Equal(declarationTitle, completionItem.Detail);
            }
            
        }

        [Theory]
        [InlineData(DeclarationType.Server)]
        [InlineData(DeclarationType.Database)]
        [InlineData(DeclarationType.Table)]
        [InlineData(DeclarationType.Column)]
        [InlineData(DeclarationType.View)]
        [InlineData(DeclarationType.Schema)]
        public void NamedIdentifiersShouldBeBracketQuoted(DeclarationType declarationType)
        {
            string declarationTitle = declarationType.ToString();
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.Equal(declarationTitle, completionItem.Label);
            Assert.Equal($"[{declarationTitle}]", completionItem.InsertText);
            Assert.Equal(declarationTitle, completionItem.Detail);
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
