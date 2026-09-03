//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class IndexCustomeNodeHelperTests
    {
        [Test]
        public void BuildIndexLabelShouldReturnClusteredColumnstoreLabel()
        {
            string label = IndexCustomeNodeHelper.BuildIndexLabel("IX_Test_CS", isUnique: false, isClustered: true, indexType: IndexType.ClusteredColumnStoreIndex);
            Assert.That(label, Is.EqualTo("IX_Test_CS (Non-Unique, Clustered Columnstore)"));
        }

        [Test]
        public void BuildIndexLabelShouldReturnNonClusteredColumnstoreLabel()
        {
            string label = IndexCustomeNodeHelper.BuildIndexLabel("IX_Test_NCCS", isUnique: false, isClustered: false, IndexType.NonClusteredColumnStoreIndex);
            Assert.That(label, Is.EqualTo("IX_Test_NCCS (Non-Unique, Nonclustered Columnstore)"));
        }

        [Test]
        public void BuildIndexLabelShouldStillReturnClusteredLabelForRegularIndex()
        {
            string label = IndexCustomeNodeHelper.BuildIndexLabel("IX_Test_Regular", isUnique: true, isClustered: true, IndexType.ClusteredIndex);
            Assert.That(label, Is.EqualTo("IX_Test_Regular (Unique, Clustered)"));
        }

        [Test]
        public void BuildIndexLabelShouldStillReturnNonClusteredLabelForRegularIndex()
        {
            string label = IndexCustomeNodeHelper.BuildIndexLabel("IX_Test_Regular2", isUnique: false, isClustered: false, IndexType.NonClusteredIndex);
            Assert.That(label, Is.EqualTo("IX_Test_Regular2 (Non-Unique, Non-Clustered)"));
        }
    }
}