//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class ScriptingTests
    {
        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task ScripTableAzure()
        {
            TestServerType serverType = TestServerType.Azure;
            await VerifyScriptTable(serverType);
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task ScripTableOnPrem()
        {
            TestServerType serverType = TestServerType.OnPrem;
            await VerifyScriptTable(serverType);
        }


        private async Task VerifyScriptTable(TestServerType serverType, [CallerMemberName] string testName = "")
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    await testService.ConnectForQuery(serverType, string.Empty, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                    List<ScriptingObject> scriptingObjects = new List<ScriptingObject>()
                    {
                        new ScriptingObject
                        {
                            Schema = "Person",
                            Name = "Address",
                            Type = "Table"
                        }
                    };
                    ScriptingParams scriptingParams = new ScriptingParams
                    {
                        OwnerUri = queryTempFile.FilePath,
                        Operation = ScriptingOperationType.Create,
                        FilePath = queryTempFile.FilePath,
                        ScriptOptions = new ScriptOptions
                        {
                            ScriptCreateDrop = "ScriptCreate",
                        },
                        ScriptDestination = "ToEditor",
                        ScriptingObjects = scriptingObjects

                    };
                    var result = await testService.CalculateRunTime(() => testService.RequestScript(scriptingParams), timer);

                    Assert.NotNull(result);
                    Assert.NotNull(result.Script);
                   
                    Assert.False(string.IsNullOrEmpty(result.Script), "Script result is invalid");

                    await testService.Disconnect(queryTempFile.FilePath);
                }
            }, testName);
        }
    }
}
