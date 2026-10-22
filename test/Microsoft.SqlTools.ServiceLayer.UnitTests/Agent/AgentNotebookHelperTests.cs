//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.Agent;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Agent
{
    [TestFixture]
    public class AgentNotebookHelperTests
    {
        [Test]
        public void ComputeNotebookHash_BindsNotebookAndExecutionDatabase()
        {
            string original = AgentNotebookHelper.ComputeNotebookHash("{\"cells\":[]}", "database1");

            Assert.That(
                AgentNotebookHelper.ComputeNotebookHash("{\"cells\":[{}]}", "database1"),
                Is.Not.EqualTo(original));
            Assert.That(
                AgentNotebookHelper.ComputeNotebookHash("{\"cells\":[]}", "database2"),
                Is.Not.EqualTo(original));
        }

        [Test]
        public void CreateNotebookPowerShellStep_EnforcesIntegrityAndUsesSafeOutputTypes()
        {
            const string expectedHash = "ABCDEF0123456789";

            string command = AgentNotebookHelper.CreateNotebookPowerShellStep(
                "notebook",
                "O’Brien'; Write-Output 'unexpected",
                expectedHash)[0].Command;
            string encodedDatabase = Convert.ToBase64String(
                Encoding.UTF8.GetBytes("O’Brien'; Write-Output 'unexpected"));

            Assert.That(
                command,
                Does.Contain($"[System.Convert]::FromBase64String('{encodedDatabase}')"));
            Assert.That(command, Does.Not.Contain("O’Brien"));
            Assert.That(command, Does.Not.Contain("unexpected"));
            Assert.That(command, Does.Contain($"$ExpectedNotebookHash = '{expectedHash}'"));
            Assert.That(command, Does.Contain("Notebook template integrity validation failed"));
            Assert.That(command, Does.Contain("$StringOutputData[\"text/plain\"]"));
            Assert.That(command, Does.Not.Contain("$StringOutputData[\"text/html\"]"));
            Assert.That(command, Does.Contain("[System.Net.WebUtility]::HtmlEncode"));
        }

        [Test]
        public void ValidateNotebookHash_RejectsChangedStoredContent()
        {
            string expectedHash = AgentNotebookHelper.ComputeNotebookHash("{\"cells\":[]}", "database1");

            Assert.DoesNotThrow(() => AgentNotebookHelper.ValidateNotebookHash(
                "{\"cells\":[]}",
                "database1",
                expectedHash));
            Assert.Throws<InvalidOperationException>(() => AgentNotebookHelper.ValidateNotebookHash(
                "{\"cells\":[{}]}",
                "database1",
                expectedHash));
            Assert.Throws<InvalidOperationException>(() => AgentNotebookHelper.ValidateNotebookHash(
                "{\"cells\":[]}",
                "database2",
                expectedHash));
        }
    }
}
