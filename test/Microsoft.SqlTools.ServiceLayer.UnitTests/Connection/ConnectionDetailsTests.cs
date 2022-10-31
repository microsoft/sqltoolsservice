//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
    /// <summary>
    /// Tests for ConnectionDetails Class
    /// </summary>
    public class ConnectionDetailsTests
    {
        [Test]
        public void ConnectionDetailsWithoutAnyOptionShouldReturnNullOrDefaultForOptions()
        {
            ConnectionDetails details = new ConnectionDetails();

            var expectedForStrings = default(string);
            var expectedForInt = default(int?);
            var expectedForBoolean = default(bool?);

            Assert.AreEqual(details.ApplicationIntent, expectedForStrings);
            Assert.AreEqual(details.ApplicationName, expectedForStrings);
            Assert.AreEqual(details.AttachDbFilename, expectedForStrings);
            Assert.AreEqual(details.AuthenticationType, expectedForStrings);
            Assert.AreEqual(details.CurrentLanguage, expectedForStrings);
            Assert.AreEqual(details.DatabaseName, expectedForStrings);
            Assert.AreEqual(details.FailoverPartner, expectedForStrings);
            Assert.AreEqual(details.Password, expectedForStrings);
            Assert.AreEqual(details.ServerName, expectedForStrings);
            Assert.AreEqual(details.TypeSystemVersion, expectedForStrings);
            Assert.AreEqual(details.UserName, expectedForStrings);
            Assert.AreEqual(details.WorkstationId, expectedForStrings);
            Assert.AreEqual(details.ConnectRetryInterval, expectedForInt);
            Assert.AreEqual(details.ConnectRetryCount, expectedForInt);
            Assert.AreEqual(details.ConnectTimeout, expectedForInt);
            Assert.AreEqual(details.LoadBalanceTimeout, expectedForInt);
            Assert.AreEqual(details.MaxPoolSize, expectedForInt);
            Assert.AreEqual(details.MinPoolSize, expectedForInt);
            Assert.AreEqual(details.PacketSize, expectedForInt);
            Assert.AreEqual(details.ColumnEncryptionSetting, expectedForStrings);
            Assert.AreEqual(details.EnclaveAttestationUrl, expectedForStrings);
            Assert.AreEqual(details.EnclaveAttestationProtocol, expectedForStrings);
            Assert.AreEqual(details.Encrypt, expectedForStrings);
            Assert.AreEqual(details.MultipleActiveResultSets, expectedForBoolean);
            Assert.AreEqual(details.MultiSubnetFailover, expectedForBoolean);
            Assert.AreEqual(details.PersistSecurityInfo, expectedForBoolean);
            Assert.AreEqual(details.Pooling, expectedForBoolean);
            Assert.AreEqual(details.Replication, expectedForBoolean);
            Assert.AreEqual(details.TrustServerCertificate, expectedForBoolean);
            Assert.AreEqual(details.HostNameInCertificate, expectedForStrings);
            Assert.AreEqual(details.Port, expectedForInt);
        }

        [Test]
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
            details.Encrypt = expectedForStrings + index++;
            details.MultipleActiveResultSets = (index++ % 2 == 0);
            details.MultiSubnetFailover = (index++ % 2 == 0);
            details.PersistSecurityInfo = (index++ % 2 == 0);
            details.Pooling = (index++ % 2 == 0);
            details.Replication = (index++ % 2 == 0);
            details.TrustServerCertificate = (index++ % 2 == 0);
            details.HostNameInCertificate = expectedForStrings + index++;
            details.Port = expectedForInt + index++;

            index = 0;
            Assert.AreEqual(details.ApplicationIntent, expectedForStrings + index++);
            Assert.AreEqual(details.ApplicationName, expectedForStrings + index++);
            Assert.AreEqual(details.AttachDbFilename, expectedForStrings + index++);
            Assert.AreEqual(details.AuthenticationType, expectedForStrings + index++);
            Assert.AreEqual(details.CurrentLanguage, expectedForStrings + index++);
            Assert.AreEqual(details.DatabaseName, expectedForStrings + index++);
            Assert.AreEqual(details.FailoverPartner, expectedForStrings + index++);
            Assert.AreEqual(details.Password, expectedForStrings + index++);
            Assert.AreEqual(details.ServerName, expectedForStrings + index++);
            Assert.AreEqual(details.TypeSystemVersion, expectedForStrings + index++);
            Assert.AreEqual(details.UserName, expectedForStrings + index++);
            Assert.AreEqual(details.WorkstationId, expectedForStrings + index++);
            Assert.AreEqual(details.ConnectRetryInterval, expectedForInt + index++);
            Assert.AreEqual(details.ConnectRetryCount, expectedForInt + index++);
            Assert.AreEqual(details.ConnectTimeout, expectedForInt + index++);
            Assert.AreEqual(details.LoadBalanceTimeout, expectedForInt + index++);
            Assert.AreEqual(details.MaxPoolSize, expectedForInt + index++);
            Assert.AreEqual(details.MinPoolSize, expectedForInt + index++);
            Assert.AreEqual(details.PacketSize, expectedForInt + index++);
            Assert.AreEqual(details.ColumnEncryptionSetting, expectedForStrings + index++);
            Assert.AreEqual(details.EnclaveAttestationProtocol, expectedForStrings + index++);
            Assert.AreEqual(details.EnclaveAttestationUrl, expectedForStrings + index++);
            Assert.AreEqual(details.Encrypt, expectedForStrings + index++);
            Assert.AreEqual(details.MultipleActiveResultSets, (index++ % 2 == 0));
            Assert.AreEqual(details.MultiSubnetFailover, (index++ % 2 == 0));
            Assert.AreEqual(details.PersistSecurityInfo, (index++ % 2 == 0));
            Assert.AreEqual(details.Pooling, (index++ % 2 == 0));
            Assert.AreEqual(details.Replication, (index++ % 2 == 0));
            Assert.AreEqual(details.TrustServerCertificate, (index++ % 2 == 0));
            Assert.AreEqual(details.HostNameInCertificate, expectedForStrings + index++);
            Assert.AreEqual(details.Port, (expectedForInt + index++));
        }

        [Test]
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
            details.Encrypt = expectedForStrings + index++;
            details.MultipleActiveResultSets = (index++ % 2 == 0);
            details.MultiSubnetFailover = (index++ % 2 == 0);
            details.PersistSecurityInfo = (index++ % 2 == 0);
            details.Pooling = (index++ % 2 == 0);
            details.Replication = (index++ % 2 == 0);
            details.TrustServerCertificate = (index++ % 2 == 0);
            details.HostNameInCertificate = expectedForStrings + index++;
            details.Port = expectedForInt + index++;

            if (optionMetadata.Options.Count() != details.Options.Count)
            {
                var optionsNotInMetadata = details.Options.Where(o => !optionMetadata.Options.Any(m => m.Name == o.Key));
                var optionNames = optionsNotInMetadata.Any() ? optionsNotInMetadata.Select(s => s.Key).Aggregate((i, j) => i + "," + j) : null;
                Assert.True(string.IsNullOrEmpty(optionNames), "Options not in metadata: " + optionNames);
            }
            foreach (var option in details.Options)
            {
                var metadata = optionMetadata.Options.FirstOrDefault(x => x.Name == option.Key);
                Assert.NotNull(metadata);
                if (metadata.ValueType == ConnectionOption.ValueTypeString)
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


        [Test]
        public void SettingConnectiomTimeoutToLongShouldStillReturnInt()
        {
            ConnectionDetails details = new ConnectionDetails();

            long timeout = 30;
            int? expectedValue = 30;
            details.Options["connectTimeout"] = timeout;

            Assert.AreEqual(details.ConnectTimeout, expectedValue);
        }

        [Test]
        public void ConnectTimeoutShouldReturnNullIfNotSet()
        {
            ConnectionDetails details = new ConnectionDetails();
            int? expectedValue = null;
            Assert.AreEqual(details.ConnectTimeout, expectedValue);
        }

        [Test]
        public void ConnectTimeoutShouldReturnNullIfSetToNull()
        {
            ConnectionDetails details = new ConnectionDetails();
            details.Options["connectTimeout"] = null;
            int? expectedValue = null;
            Assert.AreEqual(details.ConnectTimeout, expectedValue);
        }

        [Test]
        public void SettingEncrypShouldReturnExpectedEncryptOption()
        {
            ConnectionDetails details = new ConnectionDetails();
            details.Options["Encrypt"] = true.ToString();
            Assert.That(details.Encrypt, Is.EqualTo(true.ToString()), "Encrypt should be Mandatory.");

            details.Options["Encrypt"] = "Strict";
            Assert.That(details.Encrypt, Is.EqualTo("Strict"), "Encrypt should be Strict.");
        }

        [Test]
        public void EncryptShouldReturnNullIfNotSet()
        {
            ConnectionDetails details = new ConnectionDetails();
            Assert.That(details.Encrypt, Is.Null, "Encrypt should be null when set to null");
        }

        [Test]
        public void SettingConnectiomTimeoutToLongWhichCannotBeConvertedToIntShouldNotCrash()
        {
            ConnectionDetails details = new ConnectionDetails();

            long timeout = long.MaxValue;
            int? expectedValue = null;
            string? expectedEncryptValue = "Mandatory";
            details.Options["connectTimeout"] = timeout;
            details.Options["encrypt"] = expectedEncryptValue;

            Assert.That(details.ConnectTimeout, Is.EqualTo(expectedValue), "Connect Timeout not as expected");
            Assert.That(details.Encrypt, Is.EqualTo("Mandatory"), "Encrypt should be mandatory.");
        }
    }
}
