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
    public class TableColumnCollectionTest
    {
        [Test]
        public void AutoNameForNewItemTest()
        {
            var collection = new TableColumnCollection();
            collection.AddNew();
            Assert.AreEqual(1, collection.Data.Count, "The item count should be 1");
            Assert.AreEqual("column1", collection.Data[0].Name.Value);
            collection.Data.Add(new TableColumnDataModel() { Name = new InputBoxProperties() { Value = "column3" } });
            Assert.AreEqual(2, collection.Data.Count, "The item count should be 2");
            collection.AddNew();
            Assert.AreEqual(3, collection.Data.Count, "The item count should be 3");
            // the name that is not yet used should be picked
            Assert.AreEqual("column2", collection.Data[2].Name.Value);
        }
    }
}
