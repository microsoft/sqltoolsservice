//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Kusto.ServiceLayer.Utility;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ServiceHost
{
    /// <summary>
    /// ScriptFile test case
    /// </summary>
    public class SrTests
    {
        [Test]
        public void SrStringsTestWithEnLocalization()
        {
            string locale = "en";
            var args = new string[] { "--locale", locale };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.AreEqual(SR.Culture.Name, options.Locale);
            Assert.AreEqual(options.Locale, locale);

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.AreEqual("test", TestLocalizationConstant);

            // Test for unique string in Kusto ServiceLayer.
            var QueryServiceQueryInvalidOwnerUri = SR.QueryServiceQueryInvalidOwnerUri;
            Assert.AreEqual("This editor is not connected to a database", QueryServiceQueryInvalidOwnerUri);
        }

        [Test]
        public void SrStringsTestWithEsLocalization()
        {
            string locale = "es";
            var args = new string[] { "--locale", locale };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.AreEqual(SR.Culture.Name, options.Locale);
            Assert.AreEqual(options.Locale, locale);

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.AreEqual("prueba", TestLocalizationConstant);

            // Test for unique string in Kusto ServiceLayer.
            var QueryServiceQueryInvalidOwnerUri = SR.QueryServiceQueryInvalidOwnerUri;
            Assert.AreEqual("Este editor no está conectado a una base de datos", QueryServiceQueryInvalidOwnerUri);

            // Reset the locale
            SrStringsTestWithEnLocalization(); 
        }

        [Test]
        public void SrStringsTestWithNullLocalization()
        {
            SR.Culture = null;
            var args = new string[] { "" };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.Null(SR.Culture);
            Assert.AreEqual("", options.Locale);

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.AreEqual("test", TestLocalizationConstant);

            // Test for unique string in Kusto ServiceLayer.
            var QueryServiceQueryInvalidOwnerUri = SR.QueryServiceQueryInvalidOwnerUri;
            Assert.AreEqual("This editor is not connected to a database", QueryServiceQueryInvalidOwnerUri);
        }
    }
}
