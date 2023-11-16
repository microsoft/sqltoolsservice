//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestAzureBlobConnectionService
    {
        private static Lazy<TestAzureBlobConnectionService> instance = new Lazy<TestAzureBlobConnectionService>(() => new TestAzureBlobConnectionService());
        private AzureBlobConnectionSetting settings;

        private TestAzureBlobConnectionService()
        {
            LoadInstanceSettings();
        }

        public static TestAzureBlobConnectionService Instance
        {
            get
            {
                return instance.Value;
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
                throw new Exception("Fail to load the SQL connection instances.", ex);
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

                if (String.IsNullOrWhiteSpace(settings.AccountName) || String.IsNullOrWhiteSpace(settings.AccountKey) || String.IsNullOrWhiteSpace(settings.BlobContainerUri))
                {
                    throw new InvalidOperationException($"Azure Blob connection settings are not set, but are required for this test.");
                }

                Console.WriteLine("Azure Blob connection settings loaded successfully");
                return settings;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load the Azure Blob connection settings.", ex);
            }
        }
    }
}
