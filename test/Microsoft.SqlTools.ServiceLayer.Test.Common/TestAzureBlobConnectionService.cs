//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestAzureBlobConnectionService
    {
        private static string DefaultAzureBlobSettingFileName = Path.Combine(FileUtils.UserRootFolder, "azureBlobConnectionSettings.json");
        private static TestAzureBlobConnectionService instance = new TestAzureBlobConnectionService();
        private AzureBlobConnectionSetting settings;

        private TestAzureBlobConnectionService()
        {
            LoadInstanceSettings();
        }

        public static TestAzureBlobConnectionService Intance
        {
            get
            {
                return instance;
            }
        }

        public AzureBlobConnectionSetting Settings
        {
            get
            {
                return settings;
            }
        }

        internal void LoadInstanceSettings()
        {
            try
            {
                this.settings = TestAzureBlobConnectionService.InitAzureBlobConnectionSetting();
            }
            catch (Exception ex)
            {
                Assert.True(false, "Fail to load the SQL connection instances. error: " + ex.Message);
            }
        }

        internal static AzureBlobConnectionSetting InitAzureBlobConnectionSetting()
        {
            try
            {
                string azureBlobSettingFileContents = GetAzureBlobSettingFileContent();
                AzureBlobConnectionSetting setting = Newtonsoft.Json.JsonConvert.DeserializeObject<AzureBlobConnectionSetting>(azureBlobSettingFileContents);
                Console.WriteLine("Azure Blob Connection Settings loaded successfully");
                return setting;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load the azure blob connection settings. error: " + ex.Message);
                return new AzureBlobConnectionSetting();
            }
        }


        /// <summary>
        /// Get the location of azureblobsetting.json.
        /// </summary>
        /// <returns>
        /// Value of environment variable 'AzureBlobSettingsFileName'
        /// </returns>
        private static string GetAzureBlobSettingFileContent()
        {
            var settingsFileName = Environment.GetEnvironmentVariable(Constants.AzureBlobConectionSettingsEnvironmentVariable);

            if (string.IsNullOrEmpty(settingsFileName))
            {
                if (File.Exists(DefaultAzureBlobSettingFileName))
                {
                    settingsFileName = DefaultAzureBlobSettingFileName;
                }
                else
                {
                    Console.WriteLine(DefaultAzureBlobSettingFileName + " Azure blob connection settings are not configured.");
                }
            }

            if (!string.IsNullOrEmpty(settingsFileName))
            {
                Console.WriteLine("SQL Connection settings are loaded from: " + settingsFileName);
            }

            string azureBlobSettingsFileContents = string.IsNullOrEmpty(settingsFileName) ? string.Empty : File.ReadAllText(settingsFileName);

            return azureBlobSettingsFileContents;
        }
    }
}
