//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class LocalizedPropertyInfoTests
    {
        [Fact]
        public void PropertyDisplayNameShouldReturnNameWhenNotSet()
        {
            LocalizedPropertyInfo propertyInfo = new LocalizedPropertyInfo();
            propertyInfo.PropertyName = "name";

            Assert.Equal(propertyInfo.PropertyDisplayName, propertyInfo.PropertyName);
        }

        [Fact]
        public void PropertyValudDisplayNameShouldReturnValudWhenNotSet()
        {
            LocalizedPropertyInfo propertyInfo = new LocalizedPropertyInfo();
            propertyInfo.PropertyName = "name";
            propertyInfo.PropertyValue = "value";

            Assert.Equal(propertyInfo.PropertyValueDisplayName, propertyInfo.PropertyValue);
        }
    }
}
