//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class DatabaseFileInfoTests
    {
        [Test]
        public void DatabaseFileInfoConstructorShouldThrowExceptionGivenNull()
        {
            Assert.Throws<ArgumentNullException>(() => new DatabaseFileInfo(null));
        }

        [Test]
        public void DatabaseFileInfoShouldReturnNullGivenEmptyProperties()
        {
            LocalizedPropertyInfo[] properties = new LocalizedPropertyInfo[] { };
            var fileInfo = new DatabaseFileInfo(properties);
            Assert.True(string.IsNullOrEmpty(fileInfo.Id));
            Assert.True(string.IsNullOrEmpty(fileInfo.GetPropertyValueAsString(BackupSetInfo.BackupComponentPropertyName)));
        }

        [Test]
        public void DatabaseFileInfoShouldReturnValuesGivenValidProperties()
        {
            LocalizedPropertyInfo[] properties = new LocalizedPropertyInfo[] {
                new LocalizedPropertyInfo
                { 
                    PropertyName = "name",
                    PropertyValue = 1
                },
                new LocalizedPropertyInfo
                {
                    PropertyName = DatabaseFileInfo.IdPropertyName,
                    PropertyValue = "id"
                }

            };
            var fileInfo = new DatabaseFileInfo(properties);
            Assert.AreEqual("id", fileInfo.Id);
            Assert.AreEqual("1", fileInfo.GetPropertyValueAsString("name"));
        }
    }
}
