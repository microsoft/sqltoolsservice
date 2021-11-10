//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.TableDesigner;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TableDesigner
{
    public class DesignerPathUtilsTest
    {
        [Test]
        public void ParseDesignerPathTest()
        {
            DesignerEditType editType = DesignerEditType.Add;
            this.RunTest(new object[] { "property1" }, editType, true);
            this.RunTest(new object[] { "property1", 1, "property2" }, editType, true);
            this.RunTest(new object[] { "property1", "xx", "property2" }, editType, false);
            this.RunTest(new object[] { "property1", 1 }, editType, false);
            this.RunTest(new object[] { "property1", 1, "property2", 1 }, editType, false);
            this.RunTest(new object[] { "property1", 1, "property2", 1, "property3" }, editType, false);
            this.RunTest(new object[] { }, editType, false);
            this.RunTest(null, editType, false);

            editType = DesignerEditType.Remove;
            this.RunTest(new object[] { "property1", 0 }, editType, true);
            this.RunTest(new object[] { "property1", 1, "property2", 1 }, editType, true);
            this.RunTest(new object[] { "property1" }, editType, false);
            this.RunTest(new object[] { "property1", 1, "property2" }, editType, false);
            this.RunTest(new object[] { "property1", 1, "property2", 1, "property3", 1 }, editType, false);
            this.RunTest(new object[] { }, editType, false);
            this.RunTest(null, editType, false);

            editType = DesignerEditType.Update;
            this.RunTest(new object[] { "property1" }, editType, true);
            this.RunTest(new object[] { "property1", 1, "property2" }, editType, true);
            this.RunTest(new object[] { "property1", 1, "property2", 1, "property3" }, editType, true);
            this.RunTest(new object[] { "property1", "abc" }, editType, false);
            this.RunTest(new object[] { "property1", 1, "property2", 1 }, editType, false);
            this.RunTest(new object[] { "property1", 1, "property2", 1, "property3", 2, "property4" }, editType, false);
            this.RunTest(new object[] { }, editType, false);
            this.RunTest(null, editType, false);
        }

        private void RunTest(object[] path, DesignerEditType editType, bool isValidPath)
        {
            if (isValidPath)
            {
                Assert.DoesNotThrow(() =>
                {
                    DesignerPathUtils.Validate(path, editType);
                }, string.Format("Path '{0}' should be a valid path for edit type: '{1}'.", path, editType.ToString()));
            }
            else
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    DesignerPathUtils.Validate(path, editType);
                }, string.Format("Path '{0}' should not be a valid path for edit type: '{1}'.", path, editType.ToString()));
            }
        }
    }
}
