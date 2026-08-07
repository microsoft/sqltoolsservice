//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.SqlCore.Scripting;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Scripting
{
    /// <summary>
    /// Integration tests for temporal (system-versioned) table scripting.
    /// Verifies fix for GitHub issue azuredatastudio#20315 - System-Versioned tables must be scripted
    /// with PRIMARY KEY inline in CREATE TABLE, not as a separate ALTER TABLE statement.
    /// </summary>
    public class TemporalTableScriptingTests
    {
        /// <summary>
        /// Verifies that temporal tables are scripted with PRIMARY KEY constraint inline.
        /// This is required because temporal tables need the primary key to exist before
        /// SYSTEM_VERSIONING can be enabled.
        /// </summary>
        [Test]
        public async Task VerifyScriptAsCreateTemporalTable_PrimaryKeyIsInline()
        {
            string query = @"
                CREATE TABLE dbo.TemporalTestTable (
                    [Id] int NOT NULL PRIMARY KEY CLUSTERED,
                    [Name] nvarchar(100) NULL,
                    [ValidFrom] datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
                    [ValidTo] datetime2 GENERATED ALWAYS AS ROW END NOT NULL,
                    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
                )
                WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TemporalTestTableHistory));
            ";

            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "TemporalTestTable",
                Schema = "dbo",
                Type = "Table"
            };

            // Expected: PRIMARY KEY should be in CREATE TABLE, not in ALTER TABLE
            List<string> expectedScripts = new List<string>
            {
                "CREATE TABLE [dbo].[TemporalTestTable]",
                "PRIMARY KEY CLUSTERED",
                "PERIOD FOR SYSTEM_TIME",
                "SYSTEM_VERSIONING = ON"
            };

            // Should NOT contain ALTER TABLE ADD PRIMARY KEY
            List<string> unexpectedScripts = new List<string>
            {
                "ALTER TABLE [dbo].[TemporalTestTable] ADD PRIMARY KEY"
            };

            await VerifyScriptAs(query, scriptingObject, ScriptingOperationType.Create, expectedScripts, unexpectedScripts);
        }

        /// <summary>
        /// Verifies that temporal tables with named PRIMARY KEY constraint are scripted correctly.
        /// </summary>
        [Test]
        public async Task VerifyScriptAsCreateTemporalTable_WithNamedPrimaryKey()
        {
            string query = @"
                CREATE TABLE dbo.TemporalNamedPKTable (
                    [Id] int NOT NULL,
                    [Name] nvarchar(100) NULL,
                    [ValidFrom] datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
                    [ValidTo] datetime2 GENERATED ALWAYS AS ROW END NOT NULL,
                    CONSTRAINT [PK_TemporalNamedPKTable] PRIMARY KEY CLUSTERED ([Id]),
                    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
                )
                WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TemporalNamedPKTableHistory));
            ";

            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "TemporalNamedPKTable",
                Schema = "dbo",
                Type = "Table"
            };

            List<string> expectedScripts = new List<string>
            {
                "CREATE TABLE [dbo].[TemporalNamedPKTable]",
                "PK_TemporalNamedPKTable",
                "PRIMARY KEY CLUSTERED",
                "PERIOD FOR SYSTEM_TIME",
                "SYSTEM_VERSIONING = ON"
            };

            List<string> unexpectedScripts = new List<string>
            {
                "ALTER TABLE [dbo].[TemporalNamedPKTable] ADD"
            };

            await VerifyScriptAs(query, scriptingObject, ScriptingOperationType.Create, expectedScripts, unexpectedScripts);
        }

        /// <summary>
        /// Verifies that regular (non-temporal) tables still script correctly.
        /// This ensures the temporal table fix doesn't break regular table scripting.
        /// </summary>
        [Test]
        public async Task VerifyScriptAsCreateRegularTable_StillWorks()
        {
            string query = @"
                CREATE TABLE dbo.RegularTestTable (
                    [Id] int NOT NULL PRIMARY KEY CLUSTERED,
                    [Name] nvarchar(100) NULL
                );
            ";

            ScriptingObject scriptingObject = new ScriptingObject
            {
                Name = "RegularTestTable",
                Schema = "dbo",
                Type = "Table"
            };

            List<string> expectedScripts = new List<string>
            {
                "CREATE TABLE [dbo].[RegularTestTable]"
            };

            await VerifyScriptAs(query, scriptingObject, ScriptingOperationType.Create, expectedScripts, unexpectedScripts: null);
        }

        private async Task VerifyScriptAs(
            string query,
            ScriptingObject scriptingObject,
            ScriptingOperationType operation,
            List<string> expectedScripts,
            List<string> unexpectedScripts)
        {
            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, "TemporalTableTests");
            try
            {
                string capturedScript = null;
                var requestContext = new Mock<RequestContext<ScriptingResult>>();
                requestContext.Setup(x => x.SendResult(It.IsAny<ScriptingResult>()))
                    .Callback<ScriptingResult>(r => capturedScript = r.Script)
                    .Returns(Task.FromResult(new object()));

                ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(
                        testDb.DatabaseName, queryTempFile.FilePath, ConnectionType.Default);

                    var scriptingParams = new ScriptingParams
                    {
                        OwnerUri = queryTempFile.FilePath,
                        ScriptDestination = "ToEditor",
                        Operation = operation,
                        ScriptOptions = new ScriptOptions
                        {
                            ScriptCreateDrop = "ScriptCreate",
                        },
                        ScriptingObjects = new List<ScriptingObject> { scriptingObject }
                    };

                    ScriptingService service = new ScriptingService();
                    await service.HandleScriptExecuteRequest(scriptingParams, requestContext.Object);
                    Thread.Sleep(2000);
                    await service.ScriptingTask;

                    // Verify expected scripts are present
                    Assert.That(capturedScript, Is.Not.Null.And.Not.Empty, "Script should not be empty");

                    foreach (string expected in expectedScripts)
                    {
                        Assert.That(capturedScript, Does.Contain(expected),
                            $"Script should contain '{expected}'.\nActual script:\n{capturedScript}");
                    }

                    // Verify unexpected scripts are NOT present
                    if (unexpectedScripts != null)
                    {
                        foreach (string unexpected in unexpectedScripts)
                        {
                            Assert.That(capturedScript, Does.Not.Contain(unexpected),
                                $"Script should NOT contain '{unexpected}'.\nActual script:\n{capturedScript}");
                        }
                    }

                    connectionService.Disconnect(new ServiceLayer.Connection.Contracts.DisconnectParams
                    {
                        OwnerUri = queryTempFile.FilePath
                    });
                }
            }
            finally
            {
                await testDb.CleanupAsync();
            }
        }
    }
}
