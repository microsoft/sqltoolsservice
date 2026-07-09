//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.LanguageService.Formatter;
using Microsoft.SqlTools.LanguageService.Formatter.ScriptDom;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class ScriptDomSqlFormatterTests
    {
        [Test]
        public void FormatShouldReturnEmptyDocumentForWhitespace()
        {
            ScriptDomFormatterResult result = new ScriptDomSqlFormatter().Format("   \r\n\t", new FormatOptions());

            Assert.AreEqual(ScriptDomFormatterOutcome.EmptyDocument, result.Outcome);
            Assert.Null(result.FormattedText);
        }

        [Test]
        public void FormatShouldReturnParseErrorForInvalidSql()
        {
            ScriptDomFormatterResult result = new ScriptDomSqlFormatter().Format("select from", new FormatOptions());

            Assert.AreEqual(ScriptDomFormatterOutcome.ParseError, result.Outcome);
            Assert.Greater(result.ParseErrorCount, 0);
            Assert.Null(result.FormattedText);
        }

        [Test]
        public void FormatShouldReturnNoChangeForFormattedSql()
        {
            ScriptDomSqlFormatter formatter = new ScriptDomSqlFormatter();
            ScriptDomFormatterResult firstResult = formatter.Format("select 1 as value", new FormatOptions());

            Assert.AreEqual(ScriptDomFormatterOutcome.Formatted, firstResult.Outcome);
            Assert.NotNull(firstResult.FormattedText);

            ScriptDomFormatterResult secondResult = formatter.Format(firstResult.FormattedText!, new FormatOptions());

            Assert.AreEqual(ScriptDomFormatterOutcome.NoChange, secondResult.Outcome);
            Assert.Null(secondResult.FormattedText);
        }

        [Test]
        public void FormatShouldPreserveDominantCrLfLineEndings()
        {
            string sql = "create table dbo.T (id int not null,\r\nname int null)";

            ScriptDomFormatterResult result = new ScriptDomSqlFormatter().Format(sql, new FormatOptions());

            Assert.AreEqual(ScriptDomFormatterOutcome.Formatted, result.Outcome);
            Assert.NotNull(result.FormattedText);
            StringAssert.Contains("\r\n", result.FormattedText);
            Assert.False(result.FormattedText!.Replace("\r\n", string.Empty).Contains("\n"));
        }

        [Test]
        public void FormatShouldPreserveDominantLfLineEndings()
        {
            string sql = "create table dbo.T (id int not null,\nname int null)";

            ScriptDomFormatterResult result = new ScriptDomSqlFormatter().Format(sql, new FormatOptions());

            Assert.AreEqual(ScriptDomFormatterOutcome.Formatted, result.Outcome);
            Assert.NotNull(result.FormattedText);
            Assert.False(result.FormattedText!.Contains("\r\n"));
            StringAssert.Contains("\n", result.FormattedText);
        }

        [Test]
        public void FormatShouldUseEnvironmentLineEndingWhenInputHasNoLineEndings()
        {
            string sql = "create table dbo.T (id int not null, name int null)";

            ScriptDomFormatterResult result = new ScriptDomSqlFormatter().Format(sql, new FormatOptions());

            Assert.AreEqual(ScriptDomFormatterOutcome.Formatted, result.Outcome);
            Assert.NotNull(result.FormattedText);
            StringAssert.Contains(Environment.NewLine, result.FormattedText);
            Assert.False(result.FormattedText!.Replace(Environment.NewLine, string.Empty).Contains("\n"));
        }
    }
}
