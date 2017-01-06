//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServer
{
    public class SqlCompletionItemTests
    {
        private static readonly string[] ReservedWords = new string[]
        {
            "all",
            "alter",
            "and",
            "apply",
            "as",
            "asc",
            "at",
            "backup",
            "begin",
            "binary",
            "bit",
            "break",
            "bulk",
            "by",
            "call",
            "cascade",
            "case",
            "catch",
            "char",
            "character",
            "check",
            "checkpoint",
            "close",
            "clustered",
            "column",
            "columnstore",
            "commit",
            "connect",
            "constraint",
            "continue",
            "create",
            "cross",
            "current_date",
            "cursor",
            "cursor_close_on_commit",
            "cursor_default",
            "data",
            "data_compression",
            "database",
            "date",
            "datetime",
            "datetime2",
            "days",
            "dbcc",
            "dec",
            "decimal",
            "declare",
            "default",
            "delete",
            "deny",
            "desc",
            "description",
            "disabled",
            "disk",
            "distinct",
            "double",
            "drop",
            "drop_existing",
            "dump",
            "dynamic",
            "else",
            "enable",
            "encrypted",
            "end",
            "end-exec",
            "exec",
            "execute",
            "exists",
            "exit",
            "external",
            "fast_forward",
            "fetch",
            "file",
            "filegroup",
            "filename",
            "filestream",
            "filter",
            "first",
            "float",
            "for",
            "foreign",
            "from",
            "full",
            "function",
            "geography",
            "get",
            "global",
            "go",
            "goto",
            "grant",
            "group",
            "hash",
            "hashed",
            "having",
            "hidden",
            "hierarchyid",
            "holdlock",
            "hours",
            "identity",
            "identitycol",
            "if",
            "image",
            "immediate",
            "include",
            "index",
            "inner",
            "insert",
            "instead",
            "int",
            "integer",
            "intersect",
            "into",
            "isolation",
            "join",
            "json",
            "key",
            "language",
            "last",
            "left",
            "level",
            "lineno",
            "load",
            "local",
            "locate",
            "location",
            "login",
            "masked",
            "maxdop",
            "merge",
            "message",
            "modify",
            "move",
            "namespace",
            "native_compilation",
            "nchar",
            "next",
            "no",
            "nocheck",
            "nocount",
            "nonclustered",
            "none",
            "norecompute",
            "not",
            "now",
            "null",
            "numeric",
            "object",
            "of",
            "off",
            "offsets",
            "on",
            "online",
            "open",
            "openrowset",
            "openxml",
            "option",
            "or",
            "order",
            "out",
            "outer",
            "output",
            "over",
            "owner",
            "partial",
            "partition",
            "password",
            "path",
            "percent",
            "percentage",
            "period",
            "persisted",
            "plan",
            "policy",
            "precision",
            "predicate",
            "primary",
            "print",
            "prior",
            "proc",
            "procedure",
            "public",
            "query_store",
            "quoted_identifier",
            "raiserror",
            "range",
            "raw",
            "read",
            "read_committed_snapshot",
            "read_only",
            "read_write",
            "readonly",
            "readtext",
            "real",
            "rebuild",
            "receive",
            "reconfigure",
            "recovery",
            "recursive",
            "recursive_triggers",
            "references",
            "relative",
            "remove",
            "reorganize",
            "required",
            "restart",
            "restore",
            "restrict",
            "resume",
            "return",
            "returns",
            "revert",
            "revoke",
            "rollback",
            "rollup",
            "row",
            "rowcount",
            "rowguidcol",
            "rows",
            "rule",
            "sample",
            "save",
            "schema",
            "schemabinding",
            "scoped",
            "scroll",
            "secondary",
            "security",
            "select",
            "send",
            "sent",
            "sequence",
            "server",
            "session",
            "set",
            "sets",
            "setuser",
            "simple",
            "smallint",
            "smallmoney",
            "snapshot",
            "sql",
            "standard",
            "start",
            "started",
            "state",
            "statement",
            "static",
            "statistics",
            "statistics_norecompute",
            "status",
            "stopped",
            "sysname",
            "system",
            "system_time",
            "table",
            "take",
            "target",
            "then",
            "throw",
            "time",
            "timestamp",
            "tinyint",
            "to",
            "top",
            "tran",
            "transaction",
            "trigger",
            "truncate",
            "try",
            "tsql",
            "type",
            "uncommitted",
            "union",
            "unique",
            "uniqueidentifier",
            "updatetext",
            "use",
            "user",
            "using",
            "value",
            "values",
            "varchar",
            "version",
            "view",
            "waitfor",
            "when",
            "where",
            "while",
            "with",
            "within",
            "without",
            "writetext",
            "xact_abort",
            "xml",
        };

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
        public void InsertTextShouldIncludeBracketGivenReservedName()
        {
            foreach (string word in ReservedWords)
            {
                string declarationTitle = word;
                string expected = "[" + declarationTitle + "]";
                DeclarationType declarationType = DeclarationType.Table;
                string tokenText = "";
                SqlCompletionItem item = new SqlCompletionItem(declarationTitle, declarationType, tokenText);
                CompletionItem completionItem = item.CreateCompletionItem(0, 1, 2);

                Assert.Equal(completionItem.Label, word);
                Assert.Equal(completionItem.InsertText, expected);
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
