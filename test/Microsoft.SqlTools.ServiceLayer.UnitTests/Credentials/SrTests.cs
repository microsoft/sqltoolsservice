//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Utility;
using NUnit.Framework;

using CredSR = Microsoft.SqlTools.Credentials.SR;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials
{
    /// <summary>
    /// ScriptFile test case
    /// </summary>
    public class SrTests
    {
        const string CredentialsServiceName = "MicrosoftSqlToolsCredentials.exe";

        [Test]
        public void SrStringsTestWithEnLocalization()
        {
            string locale = "en";
            var args = new string[] { "--locale", locale };
            CommandOptions options = new CommandOptions(args, CredentialsServiceName);
            Assert.AreEqual(options.Locale, locale);

            var CredentialsServiceInvalidCriticalHandle = CredSR.CredentialsServiceInvalidCriticalHandle;
            Assert.AreEqual("Invalid CriticalHandle!", CredentialsServiceInvalidCriticalHandle);
        }

        [Test]
        public void SrStringsTestWithEsLocalization()
        {
            string locale = "es";
            var args = new string[] { "--locale", locale };
            CommandOptions options = new CommandOptions(args, CredentialsServiceName);
            Assert.AreEqual(options.Locale, locale);
            
            var CredentialsServiceInvalidCriticalHandle = CredSR.CredentialsServiceInvalidCriticalHandle;
            Assert.AreEqual("CriticalHandle no válido.", CredentialsServiceInvalidCriticalHandle);

            // Reset the locale
            SrStringsTestWithEnLocalization(); 
        }

        [Test]
        public void SrStringsTestWithNullLocalization()
        {
            CredSR.Culture = null;
            var args = new string[] { "" };
            CommandOptions options = new CommandOptions(args, CredentialsServiceName);
            Assert.AreEqual("", options.Locale);

            var CredentialsServiceInvalidCriticalHandle = CredSR.CredentialsServiceInvalidCriticalHandle;
            Assert.AreEqual("Invalid CriticalHandle!", CredentialsServiceInvalidCriticalHandle);
        }
    }
}
