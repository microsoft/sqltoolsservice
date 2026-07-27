//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.SqlCore.TableDesigner;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TableDesigner
{
    public class TableColumnViewModelTest
    {
        [Test]
        public void VectorPropertiesUseExpectedWireContract()
        {
            var column = new TableColumnViewModel();
            column.Length.Value = "7";
            column.VectorDimension.Value = "7";
            column.VectorDimension.Enabled = true;
            column.VectorBaseType.Value = "Float16";
            column.VectorBaseType.Values = new List<string>() { "Float32", "Float16" };
            column.VectorBaseType.Enabled = true;

            var json = JObject.FromObject(column);

            Assert.That(TableColumnPropertyNames.VectorDimension, Is.EqualTo("vectorDimension"));
            Assert.That(TableColumnPropertyNames.VectorBaseType, Is.EqualTo("vectorBaseType"));
            Assert.That(json[TableColumnPropertyNames.Length]?["value"]?.Value<string>(), Is.EqualTo("7"));
            Assert.That(json[TableColumnPropertyNames.VectorDimension]?["value"]?.Value<string>(), Is.EqualTo("7"));
            Assert.That(json[TableColumnPropertyNames.VectorDimension]?["enabled"]?.Value<bool>(), Is.True);
            Assert.That(json[TableColumnPropertyNames.VectorBaseType]?["value"]?.Value<string>(), Is.EqualTo("Float16"));
            Assert.That(json[TableColumnPropertyNames.VectorBaseType]?["values"]?.Values<string>(), Is.EquivalentTo(new[] { "Float32", "Float16" }));
            Assert.That(json[TableColumnPropertyNames.VectorBaseType]?["enabled"]?.Value<bool>(), Is.True);
        }
    }
}