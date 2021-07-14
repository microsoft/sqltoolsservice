//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class LocalizedPropertyInfoTests
    {
        [Test]
        public void PropertyDisplayNameShouldReturnNameWhenNotSet()
        {
            LocalizedPropertyInfo propertyInfo = new LocalizedPropertyInfo();
            propertyInfo.PropertyName = "name";

            Assert.AreEqual(propertyInfo.PropertyDisplayName, propertyInfo.PropertyName);
        }

        [Test]
        public void PropertyValudDisplayNameShouldReturnValudWhenNotSet()
        {
            LocalizedPropertyInfo propertyInfo = new LocalizedPropertyInfo();
            propertyInfo.PropertyName = "name";
            propertyInfo.PropertyValue = "value";

            Assert.AreEqual(propertyInfo.PropertyValueDisplayName, propertyInfo.PropertyValue);
        }
    }
}
