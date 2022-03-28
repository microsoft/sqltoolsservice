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
                AzureBlobConnectionSetting settings = new AzureBlobConnectionSetting();
                settings.AccountKey = Environment.GetEnvironmentVariable(Constants.AzureStorageAccountKey);
                settings.AccountName = Environment.GetEnvironmentVariable(Constants.AzureStorageAccountName);
                settings.BlobContainerUri = Environment.GetEnvironmentVariable(Constants.AzureBlobContainerUri); 
                Console.WriteLine("Azure Blob Connection Settings loaded successfully");
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load the azure blob connection settings. error: " + ex.Message);
                return new AzureBlobConnectionSetting();
            }
        }
    }
}
