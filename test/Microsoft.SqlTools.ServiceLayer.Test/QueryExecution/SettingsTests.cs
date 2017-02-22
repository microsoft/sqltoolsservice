//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
            Assert.Equal(64*1024, sqlToolsSettings.QueryExecutionSettings.MaxCharsToStore);
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

    }
}
