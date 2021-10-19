//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TableDesigner
{
    public class TableDesignerChangeInfoTest
    {
        [Test]
        public void DeserializeTableChangeProperty()
        {
            string testJsonStringType = "{\"type\": 1, \"property\": \"columns\"}";
            TableDesignerChangeInfo changeInfo = JsonConvert.DeserializeObject<TableDesignerChangeInfo>(testJsonStringType);
            Assert.IsNotNull(changeInfo, "string property: the changeInfo shouldn't be null.");
            Assert.IsNotNull(changeInfo.Property, "string property: the property shouldn't be null.");
            Assert.IsTrue(changeInfo.Property.GetType() == typeof(string));
            string testJsonObjectType = "{\"type\": 1, \"property\": {\"parentProperty\": \"columns\",\"index\": 0,\"property\": \"length\"}}";
            changeInfo = JsonConvert.DeserializeObject<TableDesignerChangeInfo>(testJsonObjectType);
            Assert.IsNotNull(changeInfo, "object property: the changeInfo shouldn't be null.");
            Assert.IsNotNull(changeInfo.Property, "object property: the property shouldn't be null.");
            Assert.IsTrue(changeInfo.Property.GetType() == typeof(TableDesignerPropertyIdentifier));
        }
    }
}
