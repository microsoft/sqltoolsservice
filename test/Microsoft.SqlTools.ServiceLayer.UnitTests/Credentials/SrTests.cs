//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Credentials.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials
{
    /// <summary>
    /// ScriptFile test case
    /// </summary>
    public class SrTests
    {
        /// <summary>
        /// Simple "test" to access string resources
        /// The purpose of this test is for code coverage.  It's probably better to just 
        /// exclude string resources in the code coverage report than maintain this test.
        /// </summary>
        [Test]
        public void SrStringsTest()
        {
            var culture = Microsoft.SqlTools.Credentials.SR.Culture;
            Microsoft.SqlTools.Credentials.SR.Culture = culture;
            Assert.True(Microsoft.SqlTools.Credentials.SR.Culture == culture);

            var CredentialsServiceInvalidCriticalHandle = Microsoft.SqlTools.Credentials.SR.CredentialsServiceInvalidCriticalHandle;
            var CredentialsServicePasswordLengthExceeded = Microsoft.SqlTools.Credentials.SR.CredentialsServicePasswordLengthExceeded;
            var CredentialsServiceTargetForDelete = Microsoft.SqlTools.Credentials.SR.CredentialsServiceTargetForDelete;
            var CredentialsServiceTargetForLookup = Microsoft.SqlTools.Credentials.SR.CredentialsServiceTargetForLookup;
            var CredentialServiceWin32CredentialDisposed = Microsoft.SqlTools.Credentials.SR.CredentialServiceWin32CredentialDisposed;
        }

        [Test]
        public void SrStringsTestWithEnLocalization()
        {
            string locale = "en";
            var args = new string[] { "--locale", locale };
            CredentialsCommandOptions options = new CredentialsCommandOptions(args);
            Assert.AreEqual(Microsoft.SqlTools.Credentials.SR.Culture.Name, options.Locale);
            Assert.AreEqual(options.Locale, locale);

            var TestLocalizationConstant = Microsoft.SqlTools.Credentials.SR.TestLocalizationConstant;
            Assert.AreEqual("test", TestLocalizationConstant);
        }

        [Test]
        public void SrStringsTestWithEsLocalization()
        {
            string locale = "es";
            var args = new string[] { "--locale", locale };
            CredentialsCommandOptions options = new CredentialsCommandOptions(args);
            Assert.AreEqual(Microsoft.SqlTools.Credentials.SR.Culture.Name, options.Locale);
            Assert.AreEqual(options.Locale, locale);

            var TestLocalizationConstant = Microsoft.SqlTools.Credentials.SR.TestLocalizationConstant;
            Assert.AreEqual("prueba", TestLocalizationConstant);

            // Reset the locale
            SrStringsTestWithEnLocalization(); 
        }

        [Test]
        public void SrStringsTestWithNullLocalization()
        {
            Microsoft.SqlTools.Credentials.SR.Culture = null;
            var args = new string[] { "" };
            CredentialsCommandOptions options = new CredentialsCommandOptions(args);
            Assert.Null(Microsoft.SqlTools.Credentials.SR.Culture);
            Assert.AreEqual("", options.Locale);

            var TestLocalizationConstant = Microsoft.SqlTools.Credentials.SR.TestLocalizationConstant;
            Assert.AreEqual("test", TestLocalizationConstant);
        }
    }
}
