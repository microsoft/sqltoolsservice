//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Xunit.Sdk;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    /// <summary>
    /// The attribute for each test to create the test db before the test starts
    /// </summary>
    public class CreateTestDbAttribute : BeforeAfterTestAttribute
    {
        public CreateTestDbAttribute(TestServerType serverType)
        {
            ServerType = serverType;
        }

        public CreateTestDbAttribute(int serverType)
        {
            ServerType = (TestServerType)serverType;
        }

        public TestServerType ServerType { get; set; }
        public override void Before(MethodInfo methodUnderTest)
        {
            Task task = Common.CreateTestDatabase(ServerType);
            task.Wait();
        }

        public override void After(MethodInfo methodUnderTest)
        {
        }
    }
}
