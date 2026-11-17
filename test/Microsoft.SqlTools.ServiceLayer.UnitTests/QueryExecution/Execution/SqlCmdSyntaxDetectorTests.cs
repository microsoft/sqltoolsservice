//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.BatchParser;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.Execution
{
    /// <summary>
    /// Tests for detecting SQLCMD syntax in scripts
    /// </summary>
    public class SqlCmdSyntaxDetectorTests
    {
        [TestCase(":setvar MyVar 1\nSELECT 1")]
        [TestCase("SELECT 1\n   :SETVAR MyVar 1")]
        [TestCase(":r C:\\scripts\\other.sql")]
        [TestCase(":on error exit")]
        [TestCase(":connect myServer")]
        [TestCase(":!! dir")]
        [TestCase(":exit")]
        [TestCase(":out stdout")]
        [TestCase(":listvar")]
        [TestCase("SELECT * FROM $(TableName)")]
        [TestCase("USE [$(DatabaseName)]")]
        [TestCase("SELECT \"$(ColumnName)\" FROM t")]
        [TestCase("SELECT 'it''s' AS a, $(Col) AS b")]
        [TestCase("SELECT 1 -- $(NotThis)\nSELECT $(ButThis)")]
        public void DetectsSqlCmdSyntax(string script)
        {
            Assert.That(SqlCmdSyntaxDetector.ContainsSqlCmdSyntax(script), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("SELECT 1\nGO\nSELECT 2\nGO 5")]
        [TestCase("SELECT * FROM ::fn_virtualfilestats(1, 1)")]
        [TestCase("SELECT ':setvar x 1'")]
        [TestCase("SELECT 'line1\n:setvar x 1'")]
        [TestCase("-- :setvar x 1\nSELECT 1")]
        [TestCase("/*\n:setvar x 1\n*/\nSELECT 1")]
        [TestCase("SELECT '$(NotAVariable)'")]
        [TestCase("SELECT 1 -- $(NotAVariable)")]
        [TestCase("SELECT [it's]] a 'label'] FROM t WHERE c = '$(y)'")]
        [TestCase("SELECT $(1abc), $( x), $(x")]
        [TestCase("SELECT 1 AS [label:setvar]")]
        [TestCase(":setvariable x")]
        [TestCase("SELECT 'unterminated")]
        public void DoesNotDetectSqlCmdSyntax(string script)
        {
            Assert.That(SqlCmdSyntaxDetector.ContainsSqlCmdSyntax(script), Is.False);
        }

        [TestCase("/* note\n:setvar x 1\n*/\nSELECT 1")]
        [TestCase("SELECT 'line1\n  :setvar x 1'")]
        public void LineStartColonInCommentOrStringPassesPreCheckButIsNotDetected(string script)
        {
            Assert.That(SqlCmdSyntaxDetector.MayContainSqlCmdSyntax(script), Is.True, "Pre-check should defer to the lexer");
            Assert.That(SqlCmdSyntaxDetector.ContainsSqlCmdSyntax(script), Is.False);
        }

        [TestCase("SELECT * FROM ::fn_trace_getinfo(DEFAULT) -- Note: 12:30:00\nSELECT 'HH:mm:ss', geography::Point(1, 2, 4326)")]
        [TestCase("SELECT 1 AS [label:setvar]\n  SELECT 2 /* :setvar */")]
        public void MidLineColonsAreRejectedByPreCheck(string script)
        {
            Assert.That(SqlCmdSyntaxDetector.MayContainSqlCmdSyntax(script), Is.False);
            Assert.That(SqlCmdSyntaxDetector.ContainsSqlCmdSyntax(script), Is.False);
        }

        [TestCase("SELECT 1\r\n\t :setvar x 1", true)]
        [TestCase("SELECT $(x)", true)]
        [TestCase("SELECT 1", false)]
        public void PreCheckDetectsCandidates(string script, bool expected)
        {
            Assert.That(SqlCmdSyntaxDetector.MayContainSqlCmdSyntax(script), Is.EqualTo(expected));
        }
    }
}
