//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Metadata
{
    public class ObjectMetadataTests
    {

        /// <summary>
        /// Verifies that all defined MetadataType enum values have an associated non-Unknown string in the
        /// MetadataTypeName extension method.
        /// </summary>
        [Fact]
        public void AllMetadataTypes_HaveMetadataTypeName()
        {
            // Filter out the Unknown value and then verify that all other values have an associated non-Unknown name
            foreach (MetadataType metadataType in Enum.GetValues(typeof(MetadataType)).Cast<MetadataType>()
                .Where(t => t != MetadataType.Unknown))
            {
                Assert.NotEqual("Unknown", metadataType.MetadataTypeName());
            }
        }

        public class MetadataTypeTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] {new Credential(), MetadataType.Unknown};
                yield return new object[] {new Table(), MetadataType.Table};
                yield return new object[] {new View(), MetadataType.View};
                yield return new object[] {new StoredProcedure(), MetadataType.SProc};
                yield return new object[] {new UserDefinedFunction(), MetadataType.Function};
                yield return new object[] {new Schema(), MetadataType.Schema};
                yield return new object[] {new Database(), MetadataType.Database};
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Verifies that the MetadataType extension method for SMO Objects returns expected values.
        /// </summary>
        /// <param name="smoObj">The SMO object to test</param>
        /// <param name="expectedMetadataType">The expected MetadataType</param>
        [Theory]
        [ClassData(typeof(MetadataTypeTestData))]
        public void MetadataTypeFromSmoObjectWorksCorrectly(NamedSmoObject smoObj, MetadataType expectedMetadataType)
        {
            Assert.Equal(expectedMetadataType, smoObj.MetadataType());
        }
    }
}