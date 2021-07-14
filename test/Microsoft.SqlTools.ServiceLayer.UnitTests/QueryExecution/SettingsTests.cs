//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class SettingsTests
    {
        [Test]
        public void ValidateQueryExecuteDefaults()
        {
            // If: I create a new settings object
            var sqlToolsSettings = new SqlToolsSettings();

            // Then: The default values should be as expected
            Assert.AreEqual("GO", sqlToolsSettings.QueryExecutionSettings.BatchSeparator);
            Assert.AreEqual(ushort.MaxValue, sqlToolsSettings.QueryExecutionSettings.MaxCharsToStore);
            Assert.AreEqual(2*1024*1024, sqlToolsSettings.QueryExecutionSettings.MaxXmlCharsToStore);
            Assert.False(sqlToolsSettings.QueryExecutionSettings.ExecutionPlanOptions.IncludeActualExecutionPlanXml);
            Assert.False(sqlToolsSettings.QueryExecutionSettings.ExecutionPlanOptions.IncludeEstimatedExecutionPlanXml);
            Assert.True(sqlToolsSettings.QueryExecutionSettings.DisplayBitAsNumber);
        }

        [Test]
        public void ValidateSettingsParsedFromJson()
        {
            ValidateSettings("mssql");
            ValidateSettings("sql");
        }

        private static void ValidateSettings(string settingsPropertyName)
        {
            // NOTE: Only testing displayBitAsNumber for now because it is the only one piped through
            string settingsJson = @"{"
                                        + @"""params"": {"
                                        + @""""+settingsPropertyName+@""": {"
                                        + @"""query"": {"
                                        + @"displayBitAsNumber: false"
                                        + @"}"
                                        + @"}"
                                        + @"}"
                                        + @"}";

            // If: I parse the settings JSON object
            JObject message = JObject.Parse(settingsJson);
            JToken messageParams;
            Assert.True(message.TryGetValue("params", out messageParams));
            SqlToolsSettings sqlToolsSettings = messageParams.ToObject<SqlToolsSettings>();

            // Then: The values defined in the JSON should propagate to the setting object
            Assert.False(sqlToolsSettings.QueryExecutionSettings.DisplayBitAsNumber);
        }

        [Test]
        public void ValidateSettingsObjectUpdates()
        {
            // If: I update a settings object with a new settings object
            var qes = new QueryExecutionService(null, null);
            SqlToolsSettings settings = new SqlToolsSettings()
            {
                SqlTools = new SqlToolsSettingsValues
                {
                    QueryExecutionSettings = new QueryExecutionSettings
                    {
                        DisplayBitAsNumber = false,
                        MaxXmlCharsToStore = 1,
                        MaxCharsToStore = 1,
                        RowCount = 0,
                        TextSize = 1000,
                        ExecutionTimeout = 5000,
                        NoCount = true,
                        NoExec = true,
                        ParseOnly = true,
                        ArithAbort = true,
                        StatisticsTime = true,
                        StatisticsIO = true,
                        XactAbortOn = true,
                        TransactionIsolationLevel = "REPEATABLE READ",
                        DeadlockPriority = "LOW",
                        LockTimeout = 5000,
                        QueryGovernorCostLimit = 2000,
                        AnsiDefaults = false,
                        QuotedIdentifier = true,
                        AnsiNullDefaultOn = true,
                        ImplicitTransactions = true,
                        CursorCloseOnCommit = true,
                        AnsiPadding = true,
                        AnsiWarnings = true,
                        AnsiNulls = true,
                        ExecutionPlanOptions = new ExecutionPlanOptions
                        {
                            IncludeActualExecutionPlanXml = true,
                            IncludeEstimatedExecutionPlanXml = true
                        },
                        BatchSeparator = "YO"
                    }
                }    
            };
            qes.UpdateSettings(settings, null, new EventContext());

            // Then: The settings object should match what it was updated to
            Assert.False(qes.Settings.QueryExecutionSettings.DisplayBitAsNumber);
            Assert.True(qes.Settings.QueryExecutionSettings.ExecutionPlanOptions.IncludeActualExecutionPlanXml);
            Assert.True(qes.Settings.QueryExecutionSettings.ExecutionPlanOptions.IncludeEstimatedExecutionPlanXml);
            Assert.AreEqual(1, qes.Settings.QueryExecutionSettings.MaxCharsToStore);
            Assert.AreEqual(1, qes.Settings.QueryExecutionSettings.MaxXmlCharsToStore);
            Assert.AreEqual("YO", qes.Settings.QueryExecutionSettings.BatchSeparator);
            Assert.AreEqual(1, qes.Settings.QueryExecutionSettings.MaxCharsToStore);
            Assert.AreEqual(0, qes.Settings.QueryExecutionSettings.RowCount);
            Assert.AreEqual(1000, qes.Settings.QueryExecutionSettings.TextSize);
            Assert.AreEqual(5000, qes.Settings.QueryExecutionSettings.ExecutionTimeout);
            Assert.True(qes.Settings.QueryExecutionSettings.NoCount);
            Assert.True(qes.Settings.QueryExecutionSettings.NoExec);
            Assert.True(qes.Settings.QueryExecutionSettings.ParseOnly);
            Assert.True(qes.Settings.QueryExecutionSettings.ArithAbort);
            Assert.True(qes.Settings.QueryExecutionSettings.StatisticsTime);
            Assert.True(qes.Settings.QueryExecutionSettings.StatisticsIO);
            Assert.True(qes.Settings.QueryExecutionSettings.XactAbortOn);
            Assert.AreEqual("REPEATABLE READ", qes.Settings.QueryExecutionSettings.TransactionIsolationLevel);
            Assert.AreEqual("LOW", qes.Settings.QueryExecutionSettings.DeadlockPriority);
            Assert.AreEqual(5000, qes.Settings.QueryExecutionSettings.LockTimeout);
            Assert.AreEqual(2000, qes.Settings.QueryExecutionSettings.QueryGovernorCostLimit);
            Assert.False(qes.Settings.QueryExecutionSettings.AnsiDefaults);
            Assert.True(qes.Settings.QueryExecutionSettings.QuotedIdentifier);
            Assert.True(qes.Settings.QueryExecutionSettings.AnsiNullDefaultOn);
            Assert.True(qes.Settings.QueryExecutionSettings.ImplicitTransactions);
            Assert.True(qes.Settings.QueryExecutionSettings.CursorCloseOnCommit);
            Assert.True(qes.Settings.QueryExecutionSettings.AnsiPadding);
            Assert.True(qes.Settings.QueryExecutionSettings.AnsiWarnings);
            Assert.True(qes.Settings.QueryExecutionSettings.AnsiNulls);
        }
    }
}
