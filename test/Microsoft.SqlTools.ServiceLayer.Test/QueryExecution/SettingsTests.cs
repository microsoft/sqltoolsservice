//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class SettingsTests
    {
        [Fact]
        public void ValidateQueryExecuteDefaults()
        {
            // If: I create a new settings object
            var sqlToolsSettings = new SqlToolsSettings();

            // Then: The default values should be as expected
            Assert.Equal("GO", sqlToolsSettings.QueryExecutionSettings.BatchSeparator);
            Assert.Equal(ushort.MaxValue, sqlToolsSettings.QueryExecutionSettings.MaxCharsToStore);
            Assert.Equal(2*1024*1024, sqlToolsSettings.QueryExecutionSettings.MaxXmlCharsToStore);
            Assert.False(sqlToolsSettings.QueryExecutionSettings.ExecutionPlanOptions.IncludeActualExecutionPlanXml);
            Assert.False(sqlToolsSettings.QueryExecutionSettings.ExecutionPlanOptions.IncludeEstimatedExecutionPlanXml);
            Assert.True(sqlToolsSettings.QueryExecutionSettings.DisplayBitAsNumber);
        }

        [Fact]
        public void ValidateSettingsParsedFromJson()
        {
            // NOTE: Only testing displayBitAsNumber for now because it is the only one piped through
            const string settingsJson = @"{"
                                        + @"""params"": {"
                                        + @"""mssql"": {"
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

        [Fact]
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
                        ExecutionPlanOptions = new ExecutionPlanOptions
                        {
                            IncludeActualExecutionPlanXml = true,
                            IncludeEstimatedExecutionPlanXml = true
                        },
                        BatchSeparator = "YO"
                    }
                }
                    
            };
            qes.UpdateSettings(null, settings, new EventContext());

            // Then: The settings object should match what it was updated to
            Assert.False(qes.Settings.QueryExecutionSettings.DisplayBitAsNumber);
            Assert.True(qes.Settings.QueryExecutionSettings.ExecutionPlanOptions.IncludeActualExecutionPlanXml);
            Assert.True(qes.Settings.QueryExecutionSettings.ExecutionPlanOptions.IncludeEstimatedExecutionPlanXml);
            Assert.Equal(1, qes.Settings.QueryExecutionSettings.MaxCharsToStore);
            Assert.Equal(1, qes.Settings.QueryExecutionSettings.MaxXmlCharsToStore);
            Assert.Equal("YO", qes.Settings.QueryExecutionSettings.BatchSeparator);
        }

    }
}
