//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    public class SqlCompletionItemTests
    {

        [Test]
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

        [Test]
        public void ConstructorShouldThrowExceptionGivenEmptyDeclarionType()
        {
            string declarationTitle = "";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            Assert.Throws<ArgumentException>(() => new SqlCompletionItem(declarationTitle, declarationType, tokenText));
        }

        [Test]
        public void ConstructorShouldThrowExceptionGivenNullDeclarion()
        {
            string tokenText = "";
            Assert.Throws<ArgumentException>(() => new SqlCompletionItem(null, tokenText));
        }

        [Test]
        public void InsertTextShouldIncludeBracketGivenNameWithSpecialCharacter()
        {
            string declarationTitle = "name @";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, declarationTitle);
            Assert.AreEqual(completionItem.Label, declarationTitle);
        }

        [Test]
        public void LabelShouldIncludeBracketGivenTokenWithBracket()
        {
            string declarationTitle = "name";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void LabelShouldIncludeBracketGivenTokenWithBrackets()
        {
            string declarationTitle = "name";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[]";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void LabelShouldIncludeBracketGivenSqlObjectNameWithBracket()
        {
            string declarationTitle = @"Bracket\[";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, declarationTitle);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, declarationTitle);
        }

        [Test]
        public void LabelShouldIncludeBracketGivenSqlObjectNameWithBracketAndTokenWithBracket()
        {
            string declarationTitle = @"Bracket\[";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[]";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void LabelShouldNotIncludeBracketGivenNameWithBrackets()
        {
            string declarationTitle = "[name]";
            string expected = declarationTitle;
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[]";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void LabelShouldIncludeBracketGivenNameWithOneBracket()
        {
            string declarationTitle = "[name";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[]";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void LabelShouldIncludeQuotedIdentifiersGivenTokenWithQuotedIdentifier()
        {
            string declarationTitle = "name";
            string expected = "\"" + declarationTitle + "\"";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "\"";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void LabelShouldIncludeQuotedIdentifiersGivenTokenWithQuotedIdentifiers()
        {
            string declarationTitle = "name";
            string expected = "\"" + declarationTitle + "\"";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "\"\"";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void InsertTextShouldNotIncludeBracketGivenReservedName()
        {
            foreach (string word in AutoCompleteHelper.DefaultCompletionText)
            {
                string declarationTitle = word;
                DeclarationType declarationType = DeclarationType.ApplicationRole;
                string tokenText = "";
                SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
                CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

                Assert.AreEqual(completionItem.Label, word);
                Assert.AreEqual(completionItem.InsertText, word);
                Assert.AreEqual(completionItem.Detail, word);
            }
        }

        [Test]
        public void LabelShouldNotIncludeBracketIfTokenIncludesQuotedIdentifiersGivenReservedName()
        {
            string declarationTitle = "User";
            string expected = "\"" + declarationTitle + "\"";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "\"";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void LabelShouldNotIncludeDoubleBracketIfTokenIncludesBracketsGivenReservedName()
        {
            string declarationTitle = "User";
            string expected = "[" + declarationTitle + "]";
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "[";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void TempTablesShouldNotBeEscaped()
        {
            string declarationTitle = "#TestTable";
            string expected = declarationTitle;
            DeclarationType declarationType = DeclarationType.Table;
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(completionItem.Label, expected);
            Assert.AreEqual(completionItem.InsertText, expected);
            Assert.AreEqual(completionItem.Detail, expected);
        }

        [Test]
        public void FunctionsShouldHaveParenthesesAdded([Values(DeclarationType.BuiltInFunction, DeclarationType.ScalarValuedFunction, DeclarationType.TableValuedFunction)] DeclarationType declarationType)
        {
            foreach (string word in AutoCompleteHelper.DefaultCompletionText)
            {
                string declarationTitle = word;
                string tokenText = "";
                SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
                CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

                Assert.AreEqual(declarationTitle, completionItem.Label);
                Assert.AreEqual($"{declarationTitle}()", completionItem.InsertText);
                Assert.AreEqual(declarationTitle, completionItem.Detail);
            }

        }

        [Test]
        public void GlobalVariableSystemFunctionsShouldNotHaveParenthesesAdded()
        {
            string declarationTitle = "@@CONNECTIONS";
            string tokenText = "";
            SqlCompletionItem item = new SqlCompletionItem(declarationTitle, DeclarationType.BuiltInFunction, tokenText);
            CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

            Assert.AreEqual(declarationTitle, completionItem.Label);
            Assert.AreEqual($"{declarationTitle}", completionItem.InsertText);
            Assert.AreEqual(declarationTitle, completionItem.Detail);

        }

        [Test]
        public void NamedIdentifiersShouldBeBracketQuoted(
            [Values(DeclarationType.Server, DeclarationType.Database, DeclarationType.Table, DeclarationType.Column, DeclarationType.View, DeclarationType.Schema)] DeclarationType declarationType)
        {
            string declarationTitle = declarationType.ToString();
            // All words - both reserved and not - should be bracket quoted for these types
            foreach (string word in AutoCompleteHelper.DefaultCompletionText.ToList().Append("NonReservedWord"))
            {
                string tokenText = "";
                SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
                CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

                Assert.AreEqual(declarationTitle, completionItem.Label);
                Assert.AreEqual($"[{declarationTitle}]", completionItem.InsertText);
                Assert.AreEqual(declarationTitle, completionItem.Detail);
            }
        }

        [Test]
        public void KindShouldBeModuleGivenSchemaDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Module;
            DeclarationType declarationType = DeclarationType.Schema;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Test]
        public void KindShouldBeFieldGivenColumnDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Field;
            DeclarationType declarationType = DeclarationType.Column;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Test]
        public void KindShouldBeFileGivenTableDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.File;
            DeclarationType declarationType = DeclarationType.Table;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Test]
        public void KindShouldBeFileGivenViewDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.File;
            DeclarationType declarationType = DeclarationType.View;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Test]
        public void KindShouldBeMethodGivenDatabaseDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Method;
            DeclarationType declarationType = DeclarationType.Database;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Test]
        public void KindShouldBeValueGivenScalarValuedFunctionDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Value;
            DeclarationType declarationType = DeclarationType.ScalarValuedFunction;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Test]
        public void KindShouldBeValueGivenTableValuedFunctionDeclarationType()
        {
            CompletionItemKind expectedType = CompletionItemKind.Value;
            DeclarationType declarationType = DeclarationType.TableValuedFunction;
            ValidateDeclarationType(declarationType, expectedType);
        }

        [Test]
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


            Assert.AreEqual(completionItem.Kind, expectedType);
        }
    }
}
