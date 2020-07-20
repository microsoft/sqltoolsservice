//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    /// <summary>
    /// Tests for ConnectionDetails Class
    /// </summary>
    public class ConnectionDetailsTests
    {
        [Fact]
        public void ConnectionDetailsWithoutAnyOptionShouldReturnNullOrDefaultForOptions()
        {
            ConnectionDetails details = new ConnectionDetails();

            var expectedForStrings = default(string);
            var expectedForInt = default(int?);
            var expectedForBoolean = default(bool?);

            Assert.Equal(details.ApplicationIntent, expectedForStrings);
            Assert.Equal(details.ApplicationName, expectedForStrings);
            Assert.Equal(details.AttachDbFilename, expectedForStrings);
            Assert.Equal(details.AuthenticationType, expectedForStrings);
            Assert.Equal(details.CurrentLanguage, expectedForStrings);
            Assert.Equal(details.DatabaseName, expectedForStrings);
            Assert.Equal(details.FailoverPartner, expectedForStrings);
            Assert.Equal(details.Password, expectedForStrings);
            Assert.Equal(details.ServerName, expectedForStrings);
            Assert.Equal(details.TypeSystemVersion, expectedForStrings);
            Assert.Equal(details.UserName, expectedForStrings);
            Assert.Equal(details.WorkstationId, expectedForStrings);
            Assert.Equal(details.ConnectRetryInterval, expectedForInt);
            Assert.Equal(details.ConnectRetryCount, expectedForInt);
            Assert.Equal(details.ConnectTimeout, expectedForInt);
            Assert.Equal(details.LoadBalanceTimeout, expectedForInt);
            Assert.Equal(details.MaxPoolSize, expectedForInt);
            Assert.Equal(details.MinPoolSize, expectedForInt);
            Assert.Equal(details.PacketSize, expectedForInt);
            Assert.Equal(details.ColumnEncryptionSetting, expectedForStrings);
            Assert.Equal(details.EnclaveAttestationUrl, expectedForStrings);
            Assert.Equal(details.EnclaveAttestationProtocol, expectedForStrings);
            Assert.Equal(details.Encrypt, expectedForBoolean);
            Assert.Equal(details.MultipleActiveResultSets, expectedForBoolean);
            Assert.Equal(details.MultiSubnetFailover, expectedForBoolean);
            Assert.Equal(details.PersistSecurityInfo, expectedForBoolean);
            Assert.Equal(details.Pooling, expectedForBoolean);
            Assert.Equal(details.Replication, expectedForBoolean);
            Assert.Equal(details.TrustServerCertificate, expectedForBoolean);
            Assert.Equal(details.Port, expectedForInt);
        }

        [Fact]
        public void ConnectionDetailsPropertySettersShouldSetOptionValuesCorrectly()
        {
            ConnectionDetails details = new ConnectionDetails();

            var index = 0;
            var expectedForStrings = "Value for strings";
            var expectedForInt = 345;
            details.ApplicationIntent = expectedForStrings + index++;
            details.ApplicationName = expectedForStrings + index++;
            details.AttachDbFilename = expectedForStrings + index++;
            details.AuthenticationType = expectedForStrings + index++;
            details.CurrentLanguage = expectedForStrings + index++;
            details.DatabaseName = expectedForStrings + index++;
            details.FailoverPartner = expectedForStrings + index++;
            details.Password = expectedForStrings + index++;
            details.ServerName = expectedForStrings + index++;
            details.TypeSystemVersion = expectedForStrings + index++;
            details.UserName = expectedForStrings + index++;
            details.WorkstationId = expectedForStrings + index++;
            details.ConnectRetryInterval = expectedForInt + index++;
            details.ConnectRetryCount = expectedForInt + index++;
            details.ConnectTimeout = expectedForInt + index++;
            details.LoadBalanceTimeout = expectedForInt + index++;
            details.MaxPoolSize = expectedForInt + index++;
            details.MinPoolSize = expectedForInt + index++;
            details.PacketSize = expectedForInt + index++;
            details.ColumnEncryptionSetting = expectedForStrings + index++;
            details.EnclaveAttestationProtocol = expectedForStrings + index++;
            details.EnclaveAttestationUrl = expectedForStrings + index++;
            details.Encrypt = (index++ % 2 == 0);
            details.MultipleActiveResultSets = (index++ % 2 == 0);
            details.MultiSubnetFailover = (index++ % 2 == 0);
            details.PersistSecurityInfo = (index++ % 2 == 0);
            details.Pooling = (index++ % 2 == 0);
            details.Replication = (index++ % 2 == 0);
            details.TrustServerCertificate = (index++ % 2 == 0);
            details.Port = expectedForInt + index++;

            index = 0;
            Assert.Equal(details.ApplicationIntent, expectedForStrings + index++);
            Assert.Equal(details.ApplicationName, expectedForStrings + index++);
            Assert.Equal(details.AttachDbFilename, expectedForStrings + index++);
            Assert.Equal(details.AuthenticationType, expectedForStrings + index++);
            Assert.Equal(details.CurrentLanguage, expectedForStrings + index++);
            Assert.Equal(details.DatabaseName, expectedForStrings + index++);
            Assert.Equal(details.FailoverPartner, expectedForStrings + index++);
            Assert.Equal(details.Password, expectedForStrings + index++);
            Assert.Equal(details.ServerName, expectedForStrings + index++);
            Assert.Equal(details.TypeSystemVersion, expectedForStrings + index++);
            Assert.Equal(details.UserName, expectedForStrings + index++);
            Assert.Equal(details.WorkstationId, expectedForStrings + index++);
            Assert.Equal(details.ConnectRetryInterval, expectedForInt + index++);
            Assert.Equal(details.ConnectRetryCount, expectedForInt + index++);
            Assert.Equal(details.ConnectTimeout, expectedForInt + index++);
            Assert.Equal(details.LoadBalanceTimeout, expectedForInt + index++);
            Assert.Equal(details.MaxPoolSize, expectedForInt + index++);
            Assert.Equal(details.MinPoolSize, expectedForInt + index++);
            Assert.Equal(details.PacketSize, expectedForInt + index++);
            Assert.Equal(details.ColumnEncryptionSetting, expectedForStrings + index++);
            Assert.Equal(details.EnclaveAttestationProtocol, expectedForStrings + index++);
            Assert.Equal(details.EnclaveAttestationUrl, expectedForStrings + index++);
            Assert.Equal(details.Encrypt, (index++ % 2 == 0));
            Assert.Equal(details.MultipleActiveResultSets, (index++ % 2 == 0));
            Assert.Equal(details.MultiSubnetFailover, (index++ % 2 == 0));
            Assert.Equal(details.PersistSecurityInfo, (index++ % 2 == 0));
            Assert.Equal(details.Pooling, (index++ % 2 == 0));
            Assert.Equal(details.Replication, (index++ % 2 == 0));
            Assert.Equal(details.TrustServerCertificate, (index++ % 2 == 0));
            Assert.Equal(details.Port, (expectedForInt + index++));
        }

        [Fact]
        public void ConnectionDetailsOptionsShouldBeDefinedInConnectionProviderOptions()
        {
            ConnectionDetails details = new ConnectionDetails();
            ConnectionProviderOptions optionMetadata = ConnectionProviderOptionsHelper.BuildConnectionProviderOptions();

            var index = 0;
            var expectedForStrings = "Value for strings";
            var expectedForInt = 345;
            details.ApplicationIntent = expectedForStrings + index++;
            details.ApplicationName = expectedForStrings + index++;
            details.AttachDbFilename = expectedForStrings + index++;
            details.AuthenticationType = expectedForStrings + index++;
            details.CurrentLanguage = expectedForStrings + index++;
            details.DatabaseName = expectedForStrings + index++;
            details.FailoverPartner = expectedForStrings + index++;
            details.Password = expectedForStrings + index++;
            details.ServerName = expectedForStrings + index++;
            details.TypeSystemVersion = expectedForStrings + index++;
            details.UserName = expectedForStrings + index++;
            details.WorkstationId = expectedForStrings + index++;
            details.ConnectRetryInterval = expectedForInt + index++;
            details.ConnectRetryCount = expectedForInt + index++;
            details.ConnectTimeout = expectedForInt + index++;
            details.LoadBalanceTimeout = expectedForInt + index++;
            details.MaxPoolSize = expectedForInt + index++;
            details.MinPoolSize = expectedForInt + index++;
            details.PacketSize = expectedForInt + index++;
            details.ColumnEncryptionSetting = expectedForStrings + index++;
            details.EnclaveAttestationProtocol = expectedForStrings + index++;
            details.EnclaveAttestationUrl = expectedForStrings + index++;
            details.Encrypt = (index++ % 2 == 0);
            details.MultipleActiveResultSets = (index++ % 2 == 0);
            details.MultiSubnetFailover = (index++ % 2 == 0);
            details.PersistSecurityInfo = (index++ % 2 == 0);
            details.Pooling = (index++ % 2 == 0);
            details.Replication = (index++ % 2 == 0);
            details.TrustServerCertificate = (index++ % 2 == 0);
            details.Port = expectedForInt + index++;

            if(optionMetadata.Options.Count() != details.Options.Count)
            {
                var optionsNotInMetadata = details.Options.Where(o => !optionMetadata.Options.Any(m => m.Name == o.Key));
                var optionNames = optionsNotInMetadata.Any() ? optionsNotInMetadata.Select(s => s.Key).Aggregate((i, j) => i + "," + j) : null;
                Assert.True(string.IsNullOrEmpty(optionNames), "Options not in metadata: " + optionNames);
            }
            foreach (var option in details.Options)
            {
                var metadata = optionMetadata.Options.FirstOrDefault(x => x.Name == option.Key);
                Assert.NotNull(metadata);
                if(metadata.ValueType == ConnectionOption.ValueTypeString)
                {
                    Assert.True(option.Value is string);
                }
                else if (metadata.ValueType == ConnectionOption.ValueTypeBoolean)
                {
                    Assert.True(option.Value is bool?);
                }
                else if (metadata.ValueType == ConnectionOption.ValueTypeNumber)
                {
                    Assert.True(option.Value is int?);
                }
            }
        }


        [Fact]
        public void SettingConnectiomTimeoutToLongShouldStillReturnInt()
        {
            ConnectionDetails details = new ConnectionDetails();
            
            long timeout = 30;
            int? expectedValue = 30;
            details.Options["connectTimeout"] = timeout;

            Assert.Equal(details.ConnectTimeout, expectedValue);
        }

        [Fact]
        public void ConnectTimeoutShouldReturnNullIfNotSet()
        {
            ConnectionDetails details = new ConnectionDetails();
            int? expectedValue = null;
            Assert.Equal(details.ConnectTimeout, expectedValue);
        }

        [Fact]
        public void ConnectTimeoutShouldReturnNullIfSetToNull()
        {
            ConnectionDetails details = new ConnectionDetails();
            details.Options["connectTimeout"] = null;
            int? expectedValue = null;
            Assert.Equal(details.ConnectTimeout, expectedValue);
        }

        [Fact]
        public void SettingEncryptToStringShouldStillReturnBoolean()
        {
            ConnectionDetails details = new ConnectionDetails();

            string encrypt = "True";
            bool? expectedValue = true;
            details.Options["encrypt"] = encrypt;

            Assert.Equal(details.Encrypt, expectedValue);
        }

        [Fact]
        public void SettingEncryptToLowecaseStringShouldStillReturnBoolean()
        {
            ConnectionDetails details = new ConnectionDetails();

            string encrypt = "true";
            bool? expectedValue = true;
            details.Options["encrypt"] = encrypt;

            Assert.Equal(details.Encrypt, expectedValue);
        }

        [Fact]
        public void EncryptShouldReturnNullIfNotSet()
        {
            ConnectionDetails details = new ConnectionDetails();
            bool? expectedValue = null;
            Assert.Equal(details.Encrypt, expectedValue);
        }

        [Fact]
        public void EncryptShouldReturnNullIfSetToNull()
        {
            ConnectionDetails details = new ConnectionDetails();
            details.Options["encrypt"] = null;
            int? expectedValue = null;
            Assert.Equal(details.ConnectTimeout, expectedValue);
        }

        [Fact]
        public void SettingConnectiomTimeoutToLongWhichCannotBeConvertedToIntShouldNotCrash()
        {
            ConnectionDetails details = new ConnectionDetails();

            long timeout = long.MaxValue;
            int? expectedValue = null;
            details.Options["connectTimeout"] = timeout;
            details.Options["encrypt"] = true;

            Assert.Equal(details.ConnectTimeout, expectedValue);
            Assert.Equal(true, details.Encrypt);
        }
    }
}
